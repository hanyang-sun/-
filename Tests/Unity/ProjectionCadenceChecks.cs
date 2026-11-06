using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Brick2Life.Core;
using Brick2Life.Feedback;
using Brick2Life.Simulation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Measures actual automatic RGB sampling and gate transitions, not a model of the capture code.
public static class ProjectionCadenceChecks
{
    [Serializable] private sealed class Measurement
    {
        public float stableInterval, confirmationInterval, idleSeconds;
        public int idleBlankings;
        public float changeLatencySeconds, freshConfirmationSeconds;
    }
    [Serializable] private sealed class Result
    {
        public bool passed;
        public int assertions;
        public List<string> failures = new List<string>();
        public List<Measurement> measurements = new List<Measurement>();
    }
    private static Result result;
    private static readonly Stack<IEnumerator> steps = new Stack<IEnumerator>();
    private static int lastFrame;
    private static double started;
    private static SimulationVisionMonitor monitor;
    private static SimulationBoardSession board;
    private static SimulationCameraCapture capture;
    private static ProjectionSimulator projection;
    public static void Run()
    {
        result = new Result(); lastFrame = -1;
        EditorSceneManager.OpenScene("Assets/Scenes/Brick2LifeSimulation.unity");
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        EditorApplication.playModeStateChanged += OnPlay;
        EditorApplication.isPlaying = true;
    }
    private static void OnPlay(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        EditorApplication.playModeStateChanged -= OnPlay;
        started = EditorApplication.timeSinceStartup;
        steps.Clear(); steps.Push(Verify()); EditorApplication.update += Tick;
    }
    private static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup - started > 70) throw new TimeoutException("Cadence measurements timed out.");
            if (lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;
            while (steps.Count > 0)
            {
                IEnumerator step = steps.Peek();
                if (!step.MoveNext()) { (step as IDisposable)?.Dispose(); steps.Pop(); continue; }
                if (step.Current is IEnumerator nested) { steps.Push(nested); continue; }
                return;
            }
            Finish();
        }
        catch (Exception e) { result.failures.Add(e.ToString()); Finish(); }
    }
    private static void Finish()
    {
        EditorApplication.update -= Tick;
        if (monitor != null) monitor.Automatic = false;
        while (steps.Count > 0) (steps.Pop() as IDisposable)?.Dispose();
        result.passed = result.failures.Count == 0;
        File.WriteAllText(Path.Combine(UnityEngine.Application.dataPath, "..", "verification.json"), JsonUtility.ToJson(result, true));
        EditorApplication.Exit(result.passed ? 0 : 1);
    }
    private static void Check(bool value, string message) { result.assertions++; if (!value) result.failures.Add(message); }
    private static IEnumerator Wait(Func<bool> predicate, string message)
    {
        double deadline = EditorApplication.timeSinceStartup + 3;
        while (!predicate() && EditorApplication.timeSinceStartup < deadline) yield return null;
        Check(predicate(), message);
    }
    private static void SetInterval(string name, float value) => typeof(SimulationVisionMonitor)
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(monitor, value);
    private static IEnumerator Verify()
    {
        yield return Wait(() => Object.FindFirstObjectByType<SimulationVisionMonitor>()?.Session != null, "Scene did not initialize.");
        monitor = Object.FindFirstObjectByType<SimulationVisionMonitor>();
        board = Object.FindFirstObjectByType<SimulationBoardSession>();
        capture = Object.FindFirstObjectByType<SimulationCameraCapture>();
        projection = Object.FindFirstObjectByType<SimulationLevelAdapter>().Projection;
        capture.CaptureWithoutProjection = true;
        var battle = Object.FindFirstObjectByType<SimulationBattleController>();
        Check(battle.GenerateNewRound(2, 3), "Cannot generate repeatable fixture.");
        var free = Enumerable.Range(0, 100).Select(i => new GridPoint(i % 10, i / 10))
            .Where(p => !battle.Round.IsReserved(p.X, p.Y)).Take(4).ToArray();
        float[] stable = { 0.25f, 0.6f, 0.8f, 1f };
        for (int c = 0; c < stable.Length; c++)
        {
            monitor.Automatic = false;
            SetInterval("interval", stable[c]); SetInterval("confirmationInterval", c == 0 ? 0.25f : 0.12f);
            monitor.Configure(3, true);
            yield return Wait(() => monitor.HasBaseline && !capture.ObservationCapturePending, "Baseline did not confirm for interval " + stable[c]);
            var m = new Measurement { stableInterval = stable[c], confirmationInterval = c == 0 ? 0.25f : 0.12f };
            double idleStarted = EditorApplication.timeSinceStartup, deadline = idleStarted + 4;
            bool wasHidden = !projection.IsVisible;
            while (EditorApplication.timeSinceStartup < deadline)
            {
                bool hidden = !projection.IsVisible;
                if (hidden && !wasHidden) m.idleBlankings++;
                wasHidden = hidden;
                yield return null;
            }
            m.idleSeconds = (float)(EditorApplication.timeSinceStartup - idleStarted);
            yield return Wait(() => !capture.ObservationCapturePending, "Idle capture did not complete.");
            // This edit is only the test trigger. Runtime scheduling sees automatic RGB predictions alone.
            double editStarted = EditorApplication.timeSinceStartup;
            Check(board.TryAdd(free[c].X, free[c].Y, "green", out _), "Cannot place cadence fixture wall.");
            yield return Wait(() => monitor.Session.Confirmed.At(free[c].X, free[c].Y).Occupancy == Occupancy.Occupied,
                "Automatic RGB did not confirm the edit for interval " + stable[c]);
            m.changeLatencySeconds = (float)(EditorApplication.timeSinceStartup - editStarted);
            long request = monitor.RequestFreshConfirmation();
            double freshStarted = EditorApplication.timeSinceStartup;
            Check(!monitor.HasConfirmed(request), "Fresh request accepted old RGB evidence.");
            yield return Wait(() => monitor.HasConfirmed(request), "Fresh confirmation did not collect three independent samples.");
            m.freshConfirmationSeconds = (float)(EditorApplication.timeSinceStartup - freshStarted);
            result.measurements.Add(m);
        }
        Measurement baseline = result.measurements[0], chosen = result.measurements[2];
        Check(chosen.idleBlankings <= baseline.idleBlankings * 0.5f, "Stable blanking count was not reduced by at least half.");
        Check(chosen.changeLatencySeconds < 1.5f, "Reduced blanking caused excessive layout latency.");
        Check(chosen.freshConfirmationSeconds < 0.8f, "Fresh start evidence waited at the idle interval.");
    }
}
