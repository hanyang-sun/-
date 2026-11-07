using Brick2Life.Application;
using Brick2Life.Battle;
using Brick2Life.Core;

internal static class CenteredRouteChecks
{
    private static LevelState Level(params (int x, int y, string color)[] bricks)
    {
        var definition = new BoardDefinition(colorIds: new[] { "blue", "green", "red" });
        var cells = new BoardCell[100]; Array.Fill(cells, BoardCell.Empty);
        foreach (var b in bricks) cells[b.y * 10 + b.x] = new BoardCell(Occupancy.Occupied, b.color);
        return LevelState.FromSnapshot(new BoardSnapshot(definition, 1, cells, SnapshotSource.Vision),
            LevelRuleSet.Create(new("blue", GameRole.Core), new("green", GameRole.Wall), new("red", GameRole.Tower)));
    }
    private static bool Centered(IReadOnlyList<GridPoint> path)
    {
        for (int i = 0; i < path.Count; i++)
        {
            var p = path[i];
            if (p.X % 2 == 0 && p.Y % 2 == 0) return false;
            if (i == 0) continue;
            var a = path[i - 1];
            if (Math.Abs(p.X - a.X) + Math.Abs(p.Y - a.Y) != 1) return false;
            if (p.X != a.X && p.Y % 2 != 1 || p.Y != a.Y && p.X % 2 != 1) return false;
        }
        return true;
    }
    private static int Penalty(LevelState level, int x, int y)
    {
        int value = 0;
        foreach (var d in new[] { (x: -1, y: 0), (x: 1, y: 0), (x: 0, y: -1), (x: 0, y: 1) })
        {
            int nx = x + d.x, ny = y + d.y;
            if (nx >= 0 && ny >= 0 && nx < 10 && ny < 10 && Blocked(level, nx, ny)) value += 12;
        }
        return value;
    }
    private static bool Blocked(LevelState level, int x, int y) =>
        level.At(x, y).Role == GameRole.Wall || level.At(x, y).Role == GameRole.Tower;

    // Independent Dijkstra on the original 10x10 board, without calling RouteSearch or its heap.
    private static int Oracle(LevelState level, GridPoint spawn)
    {
        var costs = Enumerable.Repeat(int.MaxValue, 100).ToArray(); var visited = new bool[100];
        costs[spawn.Y * 10 + spawn.X] = 0;
        for (int n = 0; n < 100; n++)
        {
            int best = -1;
            for (int i = 0; i < 100; i++)
                if (!visited[i] && costs[i] < int.MaxValue && (best < 0 || costs[i] < costs[best])) best = i;
            if (best < 0) break;
            if (level.At(best % 10, best / 10).Role == GameRole.Core) return costs[best];
            visited[best] = true;
            foreach (var d in new[] { (x: -1, y: 0), (x: 1, y: 0), (x: 0, y: -1), (x: 0, y: 1) })
            {
                int x = best % 10 + d.x, y = best / 10 + d.y;
                if (x < 0 || y < 0 || x >= 10 || y >= 10 || Blocked(level, x, y)) continue;
                int index = y * 10 + x;
                costs[index] = Math.Min(costs[index], costs[best] + 20 + Penalty(level, x, y));
            }
        }
        return int.MaxValue;
    }
    private static int Cost(LevelState level, IReadOnlyList<GridPoint> path) =>
        (path.Count - 1) * 10 + path.Skip(1).Where(p => p.X % 2 == 1 && p.Y % 2 == 1)
            .Sum(p => Penalty(level, p.X / 2, p.Y / 2));

    public static void Run()
    {
        int checks = 0;
        void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
        var spawn = new GridPoint(0, 0);
        var level = Level((9, 0, "blue"), (4, 0, "green"));
        var built = BattleLevel.Build(level, new[] { spawn });
        Check(built.IsValid && Centered(built.Routes[0].Route), "Route follows brick edges instead of empty physical-cell centers.");
        var path = built.Routes[0].Route;
        Check(path.All(p => !built.CellAt(p.X, p.Y).Blocked), "Centered route crosses an occupied cell.");
        bool clearance = true;
        for (int i = 1; i < path.Count; i++)
            for (int sample = 0; sample <= 10; sample++)
            {
                double t = sample / 10.0, x = (path[i - 1].X * (1 - t) + path[i].X * t) / 2,
                    y = (path[i - 1].Y * (1 - t) + path[i].Y * t) / 2;
                double dx = Math.Max(4 - x, Math.Max(0, x - 5)), dy = Math.Max(-y, Math.Max(0, y - 1));
                clearance &= Math.Sqrt(dx * dx + dy * dy) >= 0.5 - 1e-8;
            }
        Check(clearance, "Enemy centerline comes closer than half a physical cell to a brick.");

        var longWall = Enumerable.Range(2, 6).Select(x => (x, 4, "green")).Append((9, 5, "blue")).ToArray();
        var wide = BattleLevel.Build(Level(longWall), new[] { new GridPoint(0, 5) });
        Check(wide.IsValid && wide.Routes[0].Route.Any(p => p.Y >= 13), "Wide route still hugs a long wall despite a modest safer detour.");
        Check(Cost(Level(longWall), wide.Routes[0].Route) == Oracle(Level(longWall), new GridPoint(0, 5)), "Clearance route is not minimum weighted cost.");

        var corridor = Enumerable.Range(0, 10).SelectMany(x => new[] { (x, 4, "green"), (x, 6, "red") })
            .Append((9, 5, "blue")).ToArray();
        var narrow = BattleLevel.Build(Level(corridor), new[] { new GridPoint(0, 5) });
        Check(narrow.IsValid && narrow.Routes[0].Route.All(p => p.Y == 11) && Centered(narrow.Routes[0].Route),
            "One-cell corridor was falsely blocked or failed to use its centerline.");
        Check(!BattleLevel.Build(Level(corridor.Append((4, 5, "green")).ToArray()), new[] { new GridPoint(0, 5) }).IsValid,
            "Sealed corridor gained a path along brick edges.");

        var random = new Random(613);
        for (int trial = 0; trial < 40; trial++)
        {
            var bricks = new List<(int, int, string)> { (9, 9, "blue") };
            for (int y = 0; y < 10; y++) for (int x = 0; x < 10; x++)
                if ((x != 0 || y != 0) && (x != 9 || y != 9) && random.NextDouble() < 0.27)
                    bricks.Add((x, y, random.Next(2) == 0 ? "green" : "red"));
            var fixture = Level(bricks.ToArray()); var map = BattleLevel.Build(fixture, new[] { spawn });
            int expected = Oracle(fixture, spawn);
            Check(map.IsValid == (expected != int.MaxValue), "Physical-grid reachability mismatch at seed case " + trial);
            if (!map.IsValid) continue;
            Check(Centered(map.Routes[0].Route) && Cost(fixture, map.Routes[0].Route) == expected,
                "Centered route or Dijkstra weighted optimum mismatch at seed case " + trial);
        }
        Console.WriteLine($"CENTERED_ROUTE_CHECKS_PASSED: {checks} assertions.");
    }
}
