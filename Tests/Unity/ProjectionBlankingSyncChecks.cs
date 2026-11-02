using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Brick2Life.Editor;
using Brick2Life.Feedback;
using Brick2Life.Simulation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Exercises the automatic sampling path across displayed frames, not only synchronous captures.
public static class ProjectionBlankingSyncChecks
{
    [Serializable] private sealed class Result
    {
        public bool passed;
        public int assertions, hiddenFrames, visibleFrames;
        public List<string> failures = new List<string>();
    }
    private static Result result;
    private static readonly Stack<IEnumerator> steps = new Stack<IEnumerator>();
    private static double started;
    private static int lastFrame;
    private static SimulationVisionMonitor monitor;
    private static SimulationCameraCapture capture;
    private static ProjectionSimulator projection;
    private static SimulationProjectionPreview preview;
    private static Camera workspace, overhead;
    private static RenderTexture originalTarget;

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
            if (EditorApplication.timeSinceStartup - started > 25) throw new TimeoutException("Blanking checks timed out.");
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
        catch (Exception error) { result.failures.Add(error.ToString()); Finish(); }
    }
    private static void Finish()
    {
        EditorApplication.update -= Tick;
        if (overhead != null && originalTarget != null) overhead.targetTexture = originalTarget;
        if (monitor != null) monitor.Automatic = false;
        while (steps.Count > 0) (steps.Pop() as IDisposable)?.Dispose();
        SimulationDualWindowSession.Close();
        result.passed = result.failures.Count == 0;
        File.WriteAllText(Path.Combine(UnityEngine.Application.dataPath, "..", "verification.json"), JsonUtility.ToJson(result, true));
        EditorApplication.Exit(result.passed ? 0 : 1);
    }
    private static void Check(bool value, string message)
    { result.assertions++; if (!value) result.failures.Add(message); }
    private static IEnumerator Wait(Func<bool> predicate, string message)
    {
        double deadline = EditorApplication.timeSinceStartup + 3;
        while (!predicate() && EditorApplication.timeSinceStartup < deadline) yield return null;
        Check(predicate(), message);
    }
    private static bool PreviewProjectionVisible => ((Transform)typeof(SimulationProjectionPreview)
        .GetField("projectionRoot", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(preview)).gameObject.activeSelf;
    private static void RenderPreview(SimulationLevelAdapter adapter, SimulationBattleController battle)
    {
        preview.UpdateScene(adapter.Feedback.Level, projection, battle.View, false);
        preview.Render(480, 360);
    }
    private static IEnumerator Verify()
    {
        yield return Wait(() => Object.FindFirstObjectByType<SimulationVisionMonitor>()?.Session != null, "Scene did not initialize.");
        monitor = Object.FindFirstObjectByType<SimulationVisionMonitor>();
        capture = Object.FindFirstObjectByType<SimulationCameraCapture>();
        var adapter = Object.FindFirstObjectByType<SimulationLevelAdapter>();
        var battle = Object.FindFirstObjectByType<SimulationBattleController>();
        projection = adapter.Projection;
        workspace = GameObject.Find("WorkspaceCamera").GetComponent<Camera>();
        overhead = (Camera)typeof(SimulationCameraCapture).GetField("sourceCamera", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(capture);
        originalTarget = overhead.targetTexture;
        monitor.Automatic = false;
        yield return null;
        for (int i = 0; i < 3; i++) monitor.AnalyzeOnce();
        SimulationDualWindowSession.Open();
        preview = (SimulationProjectionPreview)typeof(SimulationProjectionWindow)
            .GetField("preview", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(SimulationDualWindowSession.DisplayWindow);
        Check((workspace.cullingMask & ((1 << 30) | (1 << projection.ProjectionLayer))) == 0,
            "Layout window must stay free of projection and duplicate gameplay models.");
        RenderPreview(adapter, battle);
        Check(PreviewProjectionVisible, "Preview did not initially show projection.");
        capture.CaptureWithoutProjection = true;
        monitor.Automatic = true;
        double deadline = EditorApplication.timeSinceStartup + 1.5;
        while (EditorApplication.timeSinceStartup < deadline)
        {
            RenderPreview(adapter, battle);
            Check((workspace.cullingMask & (1 << projection.ProjectionLayer)) == 0,
                "Sampling changed the layout window's projection-free configuration.");
            if (!projection.IsVisible)
            {
                result.hiddenFrames++;
                Check(!PreviewProjectionVisible, "Preview projection remains visible during source blanking.");
                Check(preview.BricksVisible, "Blanking hid the preview bricks.");
                var overlay = battle.GetComponent<SimulationBattleOverlay>();
                foreach (Renderer renderer in overlay.MarksRoot.GetComponentsInChildren<Renderer>())
                    Check(renderer.forceRenderingOff, "A battle projection renderer escaped blanking.");
            }
            else
            {
                result.visibleFrames++;
                Check(PreviewProjectionVisible, "Preview failed to restore with source projection.");
            }
            yield return null;
        }
        Check(result.hiddenFrames > 0, "Automatic sampling never exposes a blanked display frame to either window.");
        Check(result.visibleFrames > 0, "Automatic sampling never restores a displayed projection frame.");
        if (result.hiddenFrames == 0) yield break;

        yield return Wait(() => !projection.IsVisible, "Could not enter blanking to test cancellation.");
        monitor.Automatic = false;
        yield return null; yield return null;
        Check(projection.IsVisible, "Stopping automatic recognition left projection hidden.");
        RenderPreview(adapter, battle);
        Check(PreviewProjectionVisible, "Stopping recognition did not restore the view.");

        monitor.Automatic = true;
        yield return Wait(() => !projection.IsVisible, "Could not enter blanking before disabling.");
        monitor.enabled = false;
        Check(projection.IsVisible, "Disabling recognition left projection hidden.");
        monitor.Automatic = false; monitor.enabled = true;

        projection.SetVisible(false);
        monitor.Automatic = true;
        for (int i = 0; i < 10; i++) yield return null;
        monitor.Automatic = false;
        yield return null; yield return null;
        Check(!projection.IsVisible, "Capture reopened projection that was already turned off.");
        projection.SetVisible(true);

        capture.CaptureWithoutProjection = false;
        monitor.Automatic = true;
        for (int i = 0; i < 8; i++)
        {
            yield return null;
            Check(projection.IsVisible, "Projection-inclusive sampling unexpectedly blanks the display.");
        }

        capture.CaptureWithoutProjection = true;
        yield return Wait(() => !projection.IsVisible, "Could not enter blanking before camera failure.");
        overhead.targetTexture = null;
        yield return Wait(() => monitor.LastError != null && projection.IsVisible, "Failed capture did not restore projection.");
        overhead.targetTexture = originalTarget;
        monitor.Automatic = false;
        yield return null;
        RenderPreview(adapter, battle);
        Check(PreviewProjectionVisible, "Failed capture left view hidden.");
        monitor.AnalyzeOnce();
        Check(monitor.LastError == null, "Recognition did not recover after restoring camera.");
    }
}
