using Brick2Life.Application;
using Brick2Life.Battle;
using Brick2Life.Core;

internal static class LocalOverlapRerouteChecks
{
    private static readonly GridPoint[] Entrances = { new GridPoint(0, 0), new GridPoint(0, 9) };
    private static BattleLevel Map(params (int x, int y, string color)[] bricks)
    {
        var definition = new BoardDefinition(colorIds: new[] { "blue", "green" });
        var cells = new BoardCell[definition.CellCount]; Array.Fill(cells, BoardCell.Empty);
        foreach (var b in bricks) cells[definition.Index(b.x, b.y)] = new BoardCell(Occupancy.Occupied, b.color);
        var rules = LevelRuleSet.Create(new KeyValuePair<string, GameRole>("blue", GameRole.Core),
            new KeyValuePair<string, GameRole>("green", GameRole.Wall));
        return BattleLevel.Build(LevelState.FromSnapshot(new BoardSnapshot(definition, 1, cells, SnapshotSource.Vision), rules), Entrances);
    }
    private static BattleSimulation NewRun() => new BattleSimulation(Map((9, 4, "blue")),
        new WavePlan(1, 50, 9999, 1.4, 10, 0.2), new TowerSettings(500, 1, 1, 1), keepLastValidRoutes: true);
    public static void Run()
    {
        int checks = 0;
        void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
        bool SameEnemies(BattleState a, BattleState b) => a.Enemies.Count == b.Enemies.Count &&
            a.Enemies.Zip(b.Enemies).All(pair => pair.First.Id == pair.Second.Id && pair.First.Health == pair.Second.Health &&
                pair.First.SpawnIndex == pair.Second.SpawnIndex && pair.First.X == pair.Second.X && pair.First.Y == pair.Second.Y &&
                pair.First.RouteProgress == pair.Second.RouteProgress);

        var run = NewRun(); run.StartWave(); run.Step(3);
        BattleState before = run.State;
        var sixth = before.Enemies.Single(enemy => enemy.Id == 6);
        int wallX = (int)Math.Floor(sixth.X / 2), wallY = (int)Math.Floor(sixth.Y / 2);
        var changed = Map((9, 4, "blue"), (wallX, wallY, "green"));
        Check(changed.IsValid && changed.Routes.Count == 2, "Fixture must preserve both entrance routes.");
        Check(run.TryUpdateLevel(changed, out string warning), "A valid map with local overlap was rejected.");
        Check(!run.State.UsingPreviousRoutes, "A single overlapped enemy incorrectly retained all entrance routes.");
        Check(run.State.PendingEnemyReroutes > 0 && !run.State.WaitingForLayout,
            "Local overlap did not retain only the affected enemies' movement.");
        Check(warning.Contains("脱离") && !warning.Contains("布局堵路"), "Local overlap used the global blockage warning.");
        Check(ReferenceEquals(run.ActiveRoutes, changed.Routes), "New spawns did not immediately receive new routes.");
        Check(SameEnemies(before, run.State) && before.Elapsed == run.State.Elapsed && before.CoreHealth == run.State.CoreHealth &&
            before.SpawnedThisWave == run.State.SpawnedThisWave && before.Wave == run.State.Wave,
            "Rerouting teleported an enemy or reset battle progress.");

        run.SetPaused(true); var paused = run.State; run.Step(0.5);
        Check(SameEnemies(paused, run.State) && paused.PendingEnemyReroutes == run.State.PendingEnemyReroutes && paused.Elapsed == run.State.Elapsed,
            "Pause advanced pending escapes.");
        run.SetPaused(false);
        int oldSpawnCount = before.SpawnedThisWave;
        bool newEnemyEnteredWall = false, teleported = false;
        for (int frame = 0; frame < 180; frame++)
        {
            var old = run.State.Enemies.ToDictionary(enemy => enemy.Id);
            run.Step(BattleSimulation.FixedStep);
            foreach (var enemy in run.State.Enemies)
            {
                int x = (int)Math.Floor(enemy.X), y = (int)Math.Floor(enemy.Y);
                if (enemy.Id > oldSpawnCount && changed.CellAt(x, y).Blocked) newEnemyEnteredWall = true;
                if (old.TryGetValue(enemy.Id, out var previous) &&
                    Math.Abs(enemy.X - previous.X) + Math.Abs(enemy.Y - previous.Y) > 1.4 * BattleSimulation.FixedStep + 1e-8)
                    teleported = true;
            }
        }
        Check(!newEnemyEnteredWall, "New enemies repeatedly entered the new wall on the old route.");
        Check(!teleported, "An escaping enemy jumped instead of preserving its continuous position.");
        Check(run.State.Phase == BattlePhase.Spawning && run.State.SpawnedThisWave > oldSpawnCount,
            "The continuous-spawn reproduction stopped producing enemies.");
        Check(run.State.PendingEnemyReroutes == 0 && run.State.RouteWarning.Length == 0 && !run.State.UsingPreviousRoutes,
            "Pending enemies did not reconnect without another layout update.");

        var open = Map((9, 4, "blue"));
        Check(run.TryUpdateLevel(open, out _) && run.State.PendingEnemyReroutes == 0, "Removing the wall did not keep the map current.");
        var sealedMap = Map((9, 4, "blue"), (0, 0, "green"));
        var validRoutes = run.ActiveRoutes;
        Check(!sealedMap.IsValid && run.TryUpdateLevel(sealedMap, out warning) && run.State.UsingPreviousRoutes,
            "Actual global blockage stopped using the configured fallback.");
        Check(ReferenceEquals(run.ActiveRoutes, validRoutes) && run.ActiveRoutes.Count == 2 && run.State.PendingEnemyReroutes == 0,
            "Global fallback compressed entrance routes or retained stale local state.");
        Check(warning.Contains("布局堵路"), "Actual global blockage lost its warning.");
        Check(run.TryUpdateLevel(open, out _) && !run.State.UsingPreviousRoutes && run.State.RouteWarning.Length == 0,
            "Removing global blockage did not recover the new map.");

        var repaired = NewRun(); repaired.StartWave(); repaired.Step(3);
        var e = repaired.State.Enemies.Single(enemy => enemy.Id == 6);
        var covered = Map((9, 4, "blue"), ((int)Math.Floor(e.X / 2), (int)Math.Floor(e.Y / 2), "green"));
        Check(repaired.TryUpdateLevel(covered, out _) && repaired.State.PendingEnemyReroutes > 0, "Second overlap fixture failed.");
        before = repaired.State;
        Check(repaired.TryUpdateLevel(open, out _) && repaired.State.PendingEnemyReroutes == 0 && SameEnemies(before, repaired.State),
            "Moving the wall away required movement or reset enemy state.");
        Console.WriteLine($"LOCAL_OVERLAP_REROUTE_CHECKS_PASSED: {checks} assertions.");
    }
}
