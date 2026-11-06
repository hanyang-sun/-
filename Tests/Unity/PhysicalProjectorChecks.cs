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
using Object = UnityEngine.Object;

public static class PhysicalProjectorChecks
{
    [Serializable] private sealed class Result
    {
        public bool passed;
        public int assertions;
        public float redResponse, greenResponse, heightResponse, shadowResponse, unshadowedResponse;
        public int shiftedPixels, reflectedPixels;
        public Color redDelta, greenDelta;
        public float beforeShadow;
        public List<string> failures = new List<string>();
    }
    private static Result result;
    private static readonly Stack<IEnumerator> steps = new Stack<IEnumerator>();
    private static double started;
    private static int lastFrame = -1;
    private static SimulationVisionMonitor monitor;
    private static SimulationCameraCapture capture;
    private static SimulationProjectedLight light;
    private static Camera top, workspace;
    private static readonly List<Object> owned = new List<Object>();
    public static void Run()
    {
        result = new Result();
        EditorSceneManager.OpenScene("Assets/Scenes/Brick2LifeSimulation.unity");
        SimulationFeedbackSetup.Install();
        var feedbackRoot = GameObject.Find("SimulationFeedback");
        Check(feedbackRoot.GetComponents<MonoBehaviour>().All(component => component != null), "Cleanup left a missing feedback script.");
        Check(feedbackRoot.transform.Find("Level") == null, "Installer recreated the retired 3D role hierarchy.");
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
            if (EditorApplication.timeSinceStartup - started > 45) throw new TimeoutException("Physical projector checks timed out.");
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
        if (monitor != null) monitor.Automatic = false;
        if (light != null) light.CalibrationPattern = null;
        SimulationDualWindowSession.Close(); SimulationEditAccess.ConfigureFocus(null);
        foreach (Object item in owned) if (item != null) Object.DestroyImmediate(item);
        owned.Clear();
        result.passed = result.failures.Count == 0;
        File.WriteAllText(Path.Combine(UnityEngine.Application.dataPath, "..", "verification.json"), JsonUtility.ToJson(result, true));
        EditorApplication.Exit(result.passed ? 0 : 1);
    }
    private static void Check(bool value, string message) { result.assertions++; if (!value) result.failures.Add(message); }
    private static Texture2D Pattern(Func<int, Color> color)
    {
        var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        var pixels = new Color[64 * 64];
        for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++) pixels[y * 64 + x] = color(x);
        texture.SetPixels(pixels); texture.Apply(); owned.Add(texture); return texture;
    }
    private static Color Sample(SimulationObservationImage image, Vector3 position)
    {
        Vector3 v = top.WorldToViewportPoint(position);
        int x = Mathf.RoundToInt(v.x * image.Width - 0.5f), y = Mathf.RoundToInt((1 - v.y) * image.Height - 0.5f);
        Color sum = Color.clear;
        for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
        {
            int i = (Mathf.Clamp(y + dy, 0, image.Height - 1) * image.Width + Mathf.Clamp(x + dx, 0, image.Width - 1)) * 3;
            sum += new Color(image.Rgb[i] / 255f, image.Rgb[i + 1] / 255f, image.Rgb[i + 2] / 255f);
        }
        return sum / 9;
    }
    private static int Difference(byte[] a, byte[] b) => a.Zip(b, (x, y) => Math.Abs(x - y) > 3 ? 1 : 0).Sum();
    private static byte[] ReadCamera(Camera camera)
    {
        var target = new RenderTexture(512, 512, 24); target.Create();
        var image = new Texture2D(512, 512, TextureFormat.RGB24, false);
        RenderTexture old = camera.targetTexture, active = RenderTexture.active;
        try
        {
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 512, 512), 0, 0); image.Apply(); return image.GetRawTextureData<byte>().ToArray();
        }
        finally { camera.targetTexture = old; RenderTexture.active = active; target.Release(); Object.DestroyImmediate(image); Object.DestroyImmediate(target); }
    }
    private static void Save(RenderTexture texture, string filename)
    {
        var image = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
        RenderTexture active = RenderTexture.active;
        try
        {
            RenderTexture.active = texture; image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0); image.Apply();
            File.WriteAllBytes(Path.Combine(UnityEngine.Application.dataPath, "..", filename), image.EncodeToPNG());
        }
        finally { RenderTexture.active = active; Object.DestroyImmediate(image); }
    }
    private static IEnumerator Verify()
    {
        while (Object.FindFirstObjectByType<SimulationVisionMonitor>()?.Session == null) yield return null;
        monitor = Object.FindFirstObjectByType<SimulationVisionMonitor>(); monitor.Automatic = false;
        capture = Object.FindFirstObjectByType<SimulationCameraCapture>();
        var adapter = Object.FindFirstObjectByType<SimulationLevelAdapter>();
        var battle = Object.FindFirstObjectByType<SimulationBattleController>();
        var board = Object.FindFirstObjectByType<SimulationBoardSession>();
        Check(battle.GenerateNewRound(2, 3), "Cannot generate physical projector fixture.");
        Check(board.TryAdd(2, 2, "red", out _) && board.TryAdd(6, 2, "green", out _), "Cannot create colored receiver bricks.");
        light = adapter.Projection.GetComponent<SimulationProjectedLight>();
        Check(light != null && light.enabled, "Physical projector was not enabled automatically.");
        top = GameObject.Find("TopCamera").GetComponent<Camera>();
        workspace = GameObject.Find("WorkspaceCamera").GetComponent<Camera>();
        for (int i = 0; i < 3; i++) monitor.AnalyzeOnce();
        SimulationDualWindowSession.Open(); SimulationEditAccess.ConfigureFocus(() => true);
        Texture2D white = Pattern(x => Color.white), black = Pattern(x => Color.black), blue = Pattern(x => Color.blue);
        Texture2D split = Pattern(x => x < 16 ? Color.blue : Color.black);

        var clean = capture.CaptureObservationImage(false);
        light.CalibrationPattern = black;
        var dark = capture.CaptureObservationImage(true);
        Check(Difference(clean.Rgb, dark.Rgb) == 0, "A black projector image paints the scene black or exposes source quads.");
        light.BlackLevel = 0.03f;
        Check(Difference(clean.Rgb, capture.CaptureObservationImage(true).Rgb) > 1000,
            "Configurable black-level leakage did not add residual light.");
        light.BlackLevel = 0;
        // Use a non-clipping calibration exposure to measure reflectance rather than sensor saturation.
        float strength = light.LightStrength;
        light.LightStrength = 0.35f;
        light.CalibrationPattern = white;
        var bright = capture.CaptureObservationImage(true);
        result.reflectedPixels = Difference(clean.Rgb, bright.Rgb);
        Check(result.reflectedPixels > 1000, "Projector light does not reach the captured scene.");
        Color redDelta = Sample(bright, new Vector3(2.5f, 1, 2.5f)) - Sample(clean, new Vector3(2.5f, 1, 2.5f));
        Color greenDelta = Sample(bright, new Vector3(6.5f, 1, 2.5f)) - Sample(clean, new Vector3(6.5f, 1, 2.5f));
        result.redResponse = redDelta.r; result.greenResponse = greenDelta.g;
        result.redDelta = redDelta; result.greenDelta = greenDelta;
        Save(top.targetTexture, "physical-projector-white.png");
        Check(redDelta.r > 0.03f && redDelta.r > redDelta.b * 1.3f, "Red surface did not modulate reflected white projector light.");
        Check(greenDelta.g > 0.03f && greenDelta.g > greenDelta.r * 1.3f, "Green surface did not modulate reflected white projector light.");
        byte[] workspaceBright = ReadCamera(workspace);
        light.CalibrationPattern = black;
        Check(Difference(workspaceBright, ReadCamera(workspace)) == 0, "Clean building view changed with projector output.");
        light.LightStrength = strength;

        light.CalibrationPattern = split;
        var original = capture.CaptureObservationImage(true);
        light.ProjectorPosition = new Vector3(7, 14, 5);
        result.shiftedPixels = Difference(original.Rgb, capture.CaptureObservationImage(true).Rgb);
        Check(result.shiftedPixels > 1000, "Moving projector did not move the projected image.");
        light.ProjectorPosition = new Vector3(5, 14, 5);

        var receiver = GameObject.CreatePrimitive(PrimitiveType.Cube); owned.Add(receiver);
        receiver.name = "HeightReceiver"; receiver.transform.position = new Vector3(2.5f, 0.5f, 5);
        var gray = new Material(Shader.Find("Standard")) { color = new Color(0.4f, 0.4f, 0.4f) }; owned.Add(gray);
        receiver.GetComponent<Renderer>().sharedMaterial = gray;
        Vector3 lowPoint = new Vector3(2.3f, 1, 5);
        float low = Sample(capture.CaptureObservationImage(true), lowPoint).b - Sample(capture.CaptureObservationImage(false), lowPoint).b;
        receiver.transform.localScale = new Vector3(1, 2, 1); receiver.transform.position = new Vector3(2.5f, 1, 5);
        Vector3 highPoint = new Vector3(2.3f, 2, 5);
        float high = Sample(capture.CaptureObservationImage(true), highPoint).b - Sample(capture.CaptureObservationImage(false), highPoint).b;
        result.heightResponse = high - low;
        Check(result.heightResponse > 0.08f, "Changing height did not shift the projected pattern relative to the brick.");
        receiver.SetActive(false);

        light.ProjectorPosition = new Vector3(2, 8, 5);
        light.ProjectorAngles = Quaternion.LookRotation(new Vector3(5, 0, 5) - light.ProjectorPosition).eulerAngles;
        light.CalibrationPattern = blue;
        Vector3 shadowPoint = new Vector3(6, 0, 5), control = new Vector3(6, 0, 7);
        var beforeBlock = capture.CaptureObservationImage(true);
        var beforeClean = capture.CaptureObservationImage(false);
        float unblocked = Sample(beforeBlock, shadowPoint).b - Sample(beforeClean, shadowPoint).b;
        result.beforeShadow = unblocked;
        receiver.SetActive(true); receiver.transform.position = new Vector3(5, 1.5f, 5); receiver.transform.localScale = new Vector3(1, 3, 2);
        var blocked = capture.CaptureObservationImage(true); var blockedClean = capture.CaptureObservationImage(false);
        result.shadowResponse = Sample(blocked, shadowPoint).b - Sample(blockedClean, shadowPoint).b;
        result.unshadowedResponse = Sample(blocked, control).b - Sample(blockedClean, control).b;
        Check(unblocked > 0.05f && result.shadowResponse < unblocked * 0.25f, "Projector light passes through an intervening tall brick.");
        Check(result.unshadowedResponse > 0.05f, "Occlusion incorrectly removed the entire projector output.");
        capture.CaptureObservationImage(true);
        Save(top.targetTexture, "physical-projector-shadow.png");
        receiver.SetActive(false);
        light.ProjectorPosition = new Vector3(5, 14, 5); light.ProjectorAngles = new Vector3(90, 0, 0);
        light.CalibrationPattern = null;
        var natural = capture.CaptureObservationImage(true);
        Check(light.OutputTexture != null && Difference(clean.Rgb, natural.Rgb) > 20, "Existing role patterns were not baked into the projector output.");
        int originalMask = top.cullingMask;
        top.cullingMask &= ~(1 << adapter.Projection.ProjectionLayer);
        Check(Difference(clean.Rgb, capture.CaptureObservationImage(true).Rgb) > 20,
            "Camera layer exclusion unrealistically removed reflected projector light.");
        top.cullingMask = originalMask;
        Save(light.OutputTexture, "physical-projector-output.png");
        Save(top.targetTexture, "physical-projector-rgb.png");
        Check(adapter.Projection.MarksRoot.GetComponentsInChildren<Renderer>().All(r => r.forceRenderingOff), "Source quads remained directly visible after output capture.");
        var preview = (SimulationProjectionPreview)typeof(SimulationProjectionWindow)
            .GetField("preview", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(SimulationDualWindowSession.DisplayWindow);
        preview.UpdateScene(adapter.Feedback.Level, adapter.Projection, battle.View, false);
        Save(preview.Render(960, 720), "physical-projector-preview.png");
        Check(preview.BrickCount == 2 && preview.ProjectionCount > 0, "Preview did not receive the shared projector output.");
        Check(!preview.Camera.scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Transform>(true))
            .Any(t => t.name == "Projection" || t.name == "Battle"), "Preview retained obsolete projection or battle-copy roots.");
        Vector3 pose = light.ProjectorPosition, angles = light.ProjectorAngles;
        preview.Orbit(new Vector2(40, 15)); preview.Render(512, 512);
        Check(light.ProjectorPosition == pose && light.ProjectorAngles == angles, "Orbiting the observer moved the projector.");
        using (ProjectionCaptureGate.Suppress())
        {
            preview.UpdateScene(adapter.Feedback.Level, adapter.Projection, battle.View, false); preview.Render(512, 512);
            Check(!preview.ProjectionVisible, "Preview did not suppress projector contribution with capture gate.");
        }
        Check(adapter.Projection.IsVisible, "Capture gate did not restore projection.");
        light.enabled = false;
        Check(Difference(clean.Rgb, capture.CaptureObservationImage(true).Rgb) == 0, "Disabling physical projector exposes legacy source quads.");
        Check(!top.GetCommandBuffers(UnityEngine.Rendering.CameraEvent.BeforeForwardAlpha).Any(b => b.name == "Brick2Life projected light"),
            "Disabling projector leaked a camera command buffer.");
        light.enabled = true;
        battle.SetAutoStep(false);
        Check(battle.StartBattle(), "Cannot start battle with the physical projector.");
        battle.StepManual(2);
        yield return null;
        Check(battle.Running && battle.GetComponent<SimulationBattleOverlay>().ActiveEnemyCount > 0,
            "Live battle animation did not enter the projector output.");
        monitor.AnalyzeOnce();
        Check(monitor.LastRawScore.ExactMatches == 100, "Clean sampling after reflected projection corrupted RGB recognition.");
        capture.CaptureObservationImage(true);
        preview.UpdateScene(adapter.Feedback.Level, adapter.Projection, battle.View, false);
        Save(preview.Render(960, 720), "physical-projector-battle.png");
    }
}
