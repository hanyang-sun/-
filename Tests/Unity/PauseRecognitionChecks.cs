using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Brick2Life.Core;
using Brick2Life.Editor;
using Brick2Life.Feedback;
using Brick2Life.Simulation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Real RGB, real Update frames and the actual pause/continue button. No repeated-image confirmations.
public static class PauseRecognitionChecks
{
    [Serializable] private sealed class Result
    {
        public bool passed;
        public int assertions, pausedFrames, displayedFrames;
        public List<string> failures = new List<string>();
    }
    private static Result result;
    private static readonly Stack<IEnumerator> steps = new Stack<IEnumerator>();
    private static double started;
    private static int lastFrame;
    private static SimulationVisionMonitor monitor;
    private static SimulationCameraCapture capture;
    private static SimulationBoardSession board;
    private static SimulationBattleController battle;
    private static SimulationLevelAdapter adapter;
    private static SimulationProjectionPreview preview;
    private static Camera overhead;
    private static RenderTexture originalTarget;

    public static void Run()
    {
        result = new Result(); lastFrame = -1;
        EditorSceneManager.OpenScene("Assets/Scenes/Brick2LifeSimulation.unity");
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        EditorApplication.playModeStateChanged += OnPlay; EditorApplication.isPlaying = true;
    }
    private static void OnPlay(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        EditorApplication.playModeStateChanged -= OnPlay;
        started = EditorApplication.timeSinceStartup; steps.Clear(); steps.Push(Verify()); EditorApplication.update += Tick;
    }
    private static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup - started > 60) throw new TimeoutException("Pause recognition checks timed out.");
            if (lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;
            while (steps.Count > 0)
            {
                var step = steps.Peek();
                if (!step.MoveNext()) { (step as IDisposable)?.Dispose(); steps.Pop(); continue; }
                if (step.Current is IEnumerator nested) { steps.Push(nested); continue; }
                return;
            }
            Finish();
        }
        catch (Exception error) { result.failures.Add(error.ToString()); Finish(); }
    }
    private static void Finish()
    {
        EditorApplication.update -= Tick;
        if (overhead != null && originalTarget != null) overhead.targetTexture = originalTarget;
        if (capture != null) capture.enabled = true;
        if (monitor != null) { monitor.Automatic = false; monitor.SetRecognitionPaused(false); }
        while (steps.Count > 0) (steps.Pop() as IDisposable)?.Dispose();
        SimulationDualWindowSession.Close(); SimulationEditAccess.ConfigureFocus(null);
        result.passed = result.failures.Count == 0;
        File.WriteAllText(Path.Combine(UnityEngine.Application.dataPath, "..", "verification.json"), JsonUtility.ToJson(result, true));
        EditorApplication.Exit(result.passed ? 0 : 1);
    }
    private static void Check(bool value, string message)
    { result.assertions++; if (!value) result.failures.Add(message + " | " + battle?.LastMessage + " | " + monitor?.ReportText); }
    private static IEnumerator Wait(Func<bool> predicate, string message)
    {
        double end = EditorApplication.timeSinceStartup + 10;
        while (!predicate() && EditorApplication.timeSinceStartup < end) yield return null;
        Check(predicate(), message);
        if (!predicate()) throw new InvalidOperationException(message);
    }
    private static Button PauseButton => (Button)typeof(SimulationBattlePanel).GetField("pauseButton", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(battle.Panel);
    private static void CheckDisplay(bool visible)
    {
        preview.UpdateScene(adapter.Feedback.Level, adapter.Projection, battle.View, false); preview.Render(480, 360);
        Check(preview.ProjectionVisible == visible && adapter.Projection.IsVisible == visible && ProjectionCaptureGate.IsSuppressed != visible,
            "Source and display do not share pause-wide projection visibility.");
        Check(preview.BricksVisible, "Projection pause hid physical bricks.");
        if (visible) result.displayedFrames++; else result.pausedFrames++;
    }
    private static bool Matches(GridPoint p, string color) => monitor.Session.Confirmed != null &&
        monitor.Session.Confirmed.At(p.X, p.Y).ColorId == color &&
        monitor.Session.Confirmed.At(p.X, p.Y).Occupancy == (color == null ? Occupancy.Empty : Occupancy.Occupied);
    private static IEnumerator WatchDisplayed(int frames)
    {
        for (int i = 0; i < frames; i++) { CheckDisplay(true); yield return null; }
    }
    private static IEnumerator Verify()
    {
        yield return Wait(() => Object.FindFirstObjectByType<SimulationVisionMonitor>()?.Session != null, "Scene did not initialize.");
        monitor = Object.FindFirstObjectByType<SimulationVisionMonitor>();
        capture = Object.FindFirstObjectByType<SimulationCameraCapture>(); board = Object.FindFirstObjectByType<SimulationBoardSession>();
        battle = Object.FindFirstObjectByType<SimulationBattleController>(); adapter = Object.FindFirstObjectByType<SimulationLevelAdapter>();
        overhead = GameObject.Find("TopCamera").GetComponent<Camera>(); originalTarget = overhead.targetTexture;
        SimulationDualWindowSession.Open(); SimulationEditAccess.ConfigureFocus(() => true);
        preview = (SimulationProjectionPreview)typeof(SimulationProjectionWindow).GetField("preview", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(SimulationDualWindowSession.DisplayWindow);
        Check(battle.GenerateNewRound(2, 3), "Cannot prepare repeatable round.");
        battle.WavePlan = new Brick2Life.Battle.WavePlan(2, 10, 9999, 0.1, 1, 0.2);
        yield return Wait(() => monitor.HasBaseline, "Continuous mode did not establish initial baseline.");
        var free = Enumerable.Range(0, 100).Select(i => new GridPoint(i % 10, i / 10)).Where(p => !battle.Round.IsReserved(p.X, p.Y)).Take(3).ToArray();
        var modeButton = GameObject.Find("ProjectionSamplingMode").GetComponent<Button>();
        modeButton.onClick.Invoke();
        Check(!monitor.CompensatingProjection && !monitor.RecognitionPaused && PauseButton.interactable, "Pause mode cannot be controlled during preparation.");
        long before = monitor.Session.Confirmed.Version;
        Check(board.TryAdd(free[0].X, free[0].Y, "green", out _), "Cannot add preparation wall.");
        yield return WatchDisplayed(12);
        Check(monitor.Session.Confirmed.Version == before, "Pause mode sampled or confirmed while projection was displayed.");
        PauseButton.onClick.Invoke();
        Check(monitor.RecognitionPaused && monitor.Session.Confirmed.Version == before, "Pause reused one photograph as multiple confirmations.");
        CheckDisplay(false);
        yield return Wait(() => Matches(free[0], "green"), "Paused preparation did not collect fresh RGB.");
        Check(monitor.LastRawScore.ExactMatches == 100, "Paused clean RGB is inaccurate.");
        CheckDisplay(false);
        PauseButton.onClick.Invoke();
        Check(battle.PendingResume && monitor.RecognitionPaused, "Continue did not wait for fresh confirmation.");
        yield return Wait(() => !battle.PendingResume && !monitor.RecognitionPaused, "Preparation continue did not restore projection.");
        CheckDisplay(true);

        battle.StartBattle();
        Check(battle.PendingStart && monitor.RecognitionPaused, "Pause mode start failed to request a clean confirmation.");
        yield return Wait(() => battle.Running, "Pause-mode start did not finish.");
        CheckDisplay(true);
        var sameBattle = battle.Battle;
        Check(board.TryAdd(free[1].X, free[1].Y, "red", out var tower), "Cannot add tower while projection displayed.");
        before = monitor.Session.Confirmed.Version;
        yield return WatchDisplayed(12);
        Check(monitor.Session.Confirmed.Version == before, "Running pause mode periodically refreshed recognition.");
        PauseButton.onClick.Invoke();
        int spawned = battle.State.SpawnedThisWave;
        yield return Wait(() => Matches(free[1], "red"), "Paused battle did not recognize the tower.");
        Check(battle.State.Paused && battle.State.SpawnedThisWave == spawned, "Paused RGB collection advanced battle state.");
        CheckDisplay(false);
        Check(board.TryMove(tower.BrickId, free[2].X, free[2].Y), "Cannot move tower during paused capture.");
        yield return Wait(() => Matches(free[1], null) && Matches(free[2], "red"), "Edits made after pressing pause were lost.");
        PauseButton.onClick.Invoke();
        Check(battle.PendingResume && battle.State.Paused, "Continue resumed battle before RGB was confirmed.");
        yield return Wait(() => !battle.PendingResume && !battle.State.Paused, "Continue failed after fresh layout confirmation.");
        Check(ReferenceEquals(sameBattle, battle.Battle), "Pause/continue restarted the battle.");
        CheckDisplay(true);

        PauseButton.onClick.Invoke();
        modeButton.onClick.Invoke();
        Check(monitor.CompensatingProjection && battle.State.Paused, "Switching to continuous mode lost the user's battle pause.");
        CheckDisplay(true);
        Check(board.TryDelete(tower.BrickId), "Cannot delete continuous-mode fixture.");
        yield return Wait(() => Matches(free[2], null), "Continuous RGB did not resume after switching modes.");
        modeButton.onClick.Invoke();
        Check(monitor.RecognitionPaused, "Switching into pause mode during a battle pause did not close projection.");
        CheckDisplay(false);

        overhead.targetTexture = null;
        yield return Wait(() => monitor.LastError != null && !ProjectionCaptureGate.IsSuppressed, "Failed RGB capture left projection closed.");
        overhead.targetTexture = originalTarget;
        battle.SetPaused(false);
        yield return Wait(() => !battle.PendingResume && !battle.State.Paused, "Restored camera could not resume fresh capture.");
        CheckDisplay(true);
        battle.SetPaused(true); monitor.enabled = false;
        Check(!ProjectionCaptureGate.IsSuppressed, "Disabling recognition retained the projection gate.");
        monitor.enabled = true; battle.SetPaused(true); capture.enabled = false;
        yield return Wait(() => !ProjectionCaptureGate.IsSuppressed, "Disabling capture retained the projection gate.");
        capture.enabled = true; battle.SetPaused(true); monitor.Automatic = false;
        Check(!ProjectionCaptureGate.IsSuppressed, "Stopping recognition retained the projection gate.");
        monitor.Automatic = true; battle.SetPaused(true); battle.enabled = false;
        Check(!ProjectionCaptureGate.IsSuppressed, "Disabling battle controls retained the projection gate.");
        battle.enabled = true; battle.SetPaused(true);
        battle.SetPaused(false);
        typeof(SimulationBattleController).GetField("pendingSeconds", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(battle, 9f);
        yield return null; yield return null;
        Check(!battle.PendingResume && battle.State.Paused && !ProjectionCaptureGate.IsSuppressed, "Confirmation timeout lost the pause or left display dark.");
        battle.SetPaused(true); Check(battle.Restart(), "Cannot reset paused battle.");
        Check(!ProjectionCaptureGate.IsSuppressed && !battle.PendingResume, "Retry retained pause capture state.");
        CheckDisplay(true);
    }
}
