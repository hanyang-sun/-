using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Brick2Life.Simulation;
using Brick2Life.Core;
using Brick2Life.Vision;

namespace Brick2Life.Feedback
{
    /// <summary>Projects one output image onto opaque scene surfaces, with surface reflectance and projector-view occlusion.</summary>
    [DisallowMultipleComponent]
    public sealed class SimulationProjectedLight : MonoBehaviour, IProjectionObservationSource
    {
        [SerializeField] private Vector3 projectorPosition = new Vector3(5, 14, 5);
        [SerializeField] private Vector3 projectorAngles = new Vector3(90, 0, 0);
        [SerializeField, Range(10, 100)] private float fieldOfView = 46.39718f;
        [SerializeField, Range(128, 2048)] private int outputResolution = 512;
        [SerializeField, Range(0, 4)] private float lightStrength = 1.3f;
        [Tooltip("黑场残余光的归一化强度；默认 0 表示理想黑场，实机测量后再校准。")]
        [SerializeField, Range(0, 0.05f)] private float blackLevel;
        [Tooltip("输出图像的近似模糊半径（像素）；不代表已标定的镜头失焦。")]
        [SerializeField, Range(0, 3)] private float blurPixels = 0.5f;
        [SerializeField, Min(0)] private float shadowBias = 0.025f;
        private const float Far = 50;
        private const int SourceLayer = 31;
        private ProjectionSimulator source;
        private Camera contentCamera;
        private RenderTexture output;
        private Material lightMaterial, depthMaterial;
        private CommandBuffer depthCommands;
        private bool renderingContent;
        private Texture2D observationOutput;
        private SimulationObservationImage lightingImage;
        private ProjectionResponseCalibration responseCalibration;
        private Camera observationCamera;
        private int observedFrame = -1;
        private readonly Dictionary<Camera, bool> views = new Dictionary<Camera, bool>();
        private readonly Dictionary<Camera, ViewFrame> frames = new Dictionary<Camera, ViewFrame>();
        private readonly List<MeshRenderer> receivers = new List<MeshRenderer>();
        private readonly List<Renderer> contentRenderers = new List<Renderer>();

        private sealed class ViewFrame : IDisposable
        {
            public readonly Camera Camera;
            public readonly CommandBuffer Commands = new CommandBuffer { name = "Brick2Life projected light" };
            public RenderTexture Depth;
            public ViewFrame(Camera camera) { Camera = camera; camera.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, Commands); }
            public void Dispose()
            {
                if (Camera != null) Camera.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, Commands);
                Commands.Release(); Release(Depth);
            }
        }

        public Vector3 ProjectorPosition { get => projectorPosition; set => projectorPosition = value; }
        public Vector3 ProjectorAngles { get => projectorAngles; set => projectorAngles = value; }
        public Texture CalibrationPattern { get; set; }
        public RenderTexture OutputTexture => output;
        public float LightStrength { get => lightStrength; set => lightStrength = Mathf.Clamp(value, 0, 4); }
        public float BlackLevel { get => blackLevel; set => blackLevel = Mathf.Clamp(value, 0, 0.05f); }

        public void SetViewEnabled(Camera camera, bool value) { if (camera != null) views[camera] = value; }
        public void ReleaseView(Camera camera)
        {
            if (camera == null) return;
            views.Remove(camera);
            if (frames.TryGetValue(camera, out ViewFrame frame)) { frame.Dispose(); frames.Remove(camera); }
        }

        private void OnEnable() => Camera.onPreCull += BeforeCamera;
        private void OnDisable()
        {
            Camera.onPreCull -= BeforeCamera;
            foreach (ViewFrame frame in frames.Values) frame.Dispose();
            frames.Clear();
        }
        private void OnDestroy()
        {
            if (contentCamera != null) DestroyOwned(contentCamera.gameObject);
            Release(output); depthCommands?.Release();
            DestroyOwned(lightMaterial); DestroyOwned(depthMaterial);
            DestroyOwned(observationOutput);
            responseCalibration?.Dispose();
        }
        private static void DestroyOwned(UnityEngine.Object value)
        {
            if (value == null) return;
            if (UnityEngine.Application.isPlaying) Destroy(value); else DestroyImmediate(value);
        }
        private static void Release(RenderTexture texture)
        { if (texture != null) { texture.Release(); DestroyOwned(texture); } }

        private void EnsureResources()
        {
            if (source == null) source = GetComponent<ProjectionSimulator>();
            if (lightMaterial == null)
            {
                Shader shader = Resources.Load<Shader>("ProjectedLight/SurfaceLight");
                Shader depth = Resources.Load<Shader>("ProjectedLight/ProjectorDepth");
                if (shader == null || depth == null || !shader.isSupported || !depth.isSupported)
                    throw new InvalidOperationException("缺少或不支持虚拟投影仪 Shader。");
                lightMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                depthMaterial = new Material(depth) { hideFlags = HideFlags.HideAndDontSave };
                depthCommands = new CommandBuffer { name = "Brick2Life projector depth" };
            }
            if (output == null || output.width != outputResolution)
            {
                Release(output);
                output = new RenderTexture(outputResolution, outputResolution, 24, RenderTextureFormat.ARGB32)
                    { name = "VirtualProjectorOutput", hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
                output.Create();
            }
            if (contentCamera == null)
            {
                var go = new GameObject("VirtualProjectorContent", typeof(Camera)) { hideFlags = HideFlags.HideAndDontSave };
                SceneManager.MoveGameObjectToScene(go, gameObject.scene);
                contentCamera = go.GetComponent<Camera>();
                contentCamera.enabled = false;
                contentCamera.orthographic = false;
                contentCamera.aspect = 1; contentCamera.nearClipPlane = 0.1f; contentCamera.farClipPlane = Far;
                contentCamera.clearFlags = CameraClearFlags.SolidColor; contentCamera.backgroundColor = Color.clear;
                contentCamera.cullingMask = 1 << SourceLayer;
                contentCamera.allowHDR = false; contentCamera.allowMSAA = false;
            }
            // Bake and reproject with the same optics, including runtime pose/FOV changes.
            contentCamera.fieldOfView = fieldOfView;
            contentCamera.transform.SetPositionAndRotation(projectorPosition, Quaternion.Euler(projectorAngles));
        }

        private void BeforeCamera(Camera camera)
        {
            if (renderingContent || camera == null || !isActiveAndEnabled) return;
            bool registered = views.TryGetValue(camera, out bool enabledForView);
            if (!registered && (camera.name != "TopCamera" || camera.gameObject.scene != gameObject.scene)) return;
            if (!frames.TryGetValue(camera, out ViewFrame frame)) { frame = new ViewFrame(camera); frames.Add(camera, frame); }
            frame.Commands.Clear();
            if (camera.name == "TopCamera" && camera.gameObject.scene == gameObject.scene)
            { observedFrame = Time.frameCount; observationCamera = camera; }
            try
            {
                EnsureResources();
                if ((registered && !enabledForView) || source == null || !source.IsVisible || ProjectionCaptureGate.IsSuppressed) return;
                if (CalibrationPattern == null && source.LastPlan == null) return;
                if (CalibrationPattern == null)
                {
                    renderingContent = true;
                    contentRenderers.Clear();
                    if (source.MarksRoot != null) contentRenderers.AddRange(source.MarksRoot.GetComponentsInChildren<Renderer>());
                    var overlay = GetComponentInChildren<SimulationBattleOverlay>();
                    if (overlay != null && overlay.MarksRoot != null) contentRenderers.AddRange(overlay.MarksRoot.GetComponentsInChildren<Renderer>());
                    try
                    {
                        foreach (Renderer renderer in contentRenderers) renderer.forceRenderingOff = false;
                        contentCamera.targetTexture = output; contentCamera.Render();
                    }
                    finally
                    {
                        foreach (Renderer renderer in contentRenderers) if (renderer != null) renderer.forceRenderingOff = true;
                        renderingContent = false;
                    }
                }
                Scene scene = camera.scene.IsValid() ? camera.scene : camera.gameObject.scene;
                CollectReceivers(scene, camera.cullingMask);
                Matrix4x4 view = contentCamera.worldToCameraMatrix;
                Matrix4x4 vp = GL.GetGPUProjectionMatrix(contentCamera.projectionMatrix, true) * view;
                RenderOcclusion(frame, vp, view);
                CommandBuffer draw = frame.Commands;
                draw.SetGlobalTexture("_VPImage", CalibrationPattern != null ? CalibrationPattern : output);
                draw.SetGlobalTexture("_VPDepth", frame.Depth);
                draw.SetGlobalMatrix("_VPMatrix", vp); draw.SetGlobalMatrix("_VPView", view);
                draw.SetGlobalVector("_VPPosition", projectorPosition);
                draw.SetGlobalFloat("_VPFar", Far); draw.SetGlobalFloat("_VPBias", shadowBias);
                draw.SetGlobalFloat("_VPStrength", lightStrength);
                draw.SetGlobalFloat("_VPBlackLevel", blackLevel);
                Texture imageSource = CalibrationPattern != null ? CalibrationPattern : output;
                draw.SetGlobalVector("_VPBlurStep", new Vector4(blurPixels / imageSource.width, blurPixels / imageSource.height, 0, 0));
                foreach (MeshRenderer receiver in receivers)
                {
                    Material[] materials = receiver.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        Material material = materials[i];
                        if (material == null || material.renderQueue >= 2500) continue;
                        draw.SetGlobalColor("_VPReceiverColor", material.HasProperty("_Color") ? material.color : Color.white);
                        Texture image = material.HasProperty("_MainTex") ? material.mainTexture : null;
                        draw.SetGlobalTexture("_VPReceiverTex", image != null ? image : Texture2D.whiteTexture);
                        Vector2 scale = material.HasProperty("_MainTex") ? material.mainTextureScale : Vector2.one;
                        Vector2 offset = material.HasProperty("_MainTex") ? material.mainTextureOffset : Vector2.zero;
                        draw.SetGlobalVector("_VPReceiverST", new Vector4(scale.x, scale.y, offset.x, offset.y));
                        draw.DrawRenderer(receiver, lightMaterial, i, 0);
                    }
                }
            }
            catch (Exception error) { frame.Commands.Clear(); Debug.LogException(error, this); }
        }

        public void GetObservationLighting(BoardDefinition definition, VisionCalibration calibration, SimulationObservationImage image,
            out PhotometricProfile profile, out CellProjection projection)
        {
            if (!isActiveAndEnabled || !image.ProjectionVisible || observedFrame != Time.frameCount || image.RenderFrame != observedFrame)
                throw new InvalidOperationException("缺少本次 RGB 曝光对应的投影输出，不能使用旧指令补偿。");
            Texture input = CalibrationPattern != null ? CalibrationPattern : source.LastPlan == null ? Texture2D.blackTexture : output;
            if (input == null) throw new InvalidOperationException("尚未生成投影输出。");
            if (observationOutput == null || observationOutput.width != input.width || observationOutput.height != input.height)
            {
                DestroyOwned(observationOutput);
                observationOutput = new Texture2D(input.width, input.height, TextureFormat.RGBA32, false, true)
                    { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
            }
            if (!ReferenceEquals(lightingImage, image))
            {
                RenderTexture previous = RenderTexture.active;
                RenderTexture temporary = null;
                try
                {
                    if (input is RenderTexture target) RenderTexture.active = target;
                    else { temporary = RenderTexture.GetTemporary(input.width, input.height, 0); Graphics.Blit(input, temporary); RenderTexture.active = temporary; }
                    observationOutput.ReadPixels(new Rect(0, 0, input.width, input.height), 0, 0); observationOutput.Apply(false, false);
                    lightingImage = image;
                }
                finally { RenderTexture.active = previous; if (temporary != null) RenderTexture.ReleaseTemporary(temporary); }
            }
            int perAxis = calibration.SamplesPerAxis, count = perAxis * perAxis;
            if (QualitySettings.activeColorSpace != ColorSpace.Gamma) throw new InvalidOperationException("当前连续识别适配需要 Gamma 渲染；Linear 管线须另行标定。");
            if (responseCalibration == null) responseCalibration = new ProjectionResponseCalibration(gameObject.scene);
            Texture2D ambientResponse = responseCalibration.Get(observationCamera, image, calibration.SurfaceY);
            var red = new double[definition.CellCount][]; var green = new double[definition.CellCount][]; var blue = new double[definition.CellCount][];
            var ar = new double[definition.CellCount][]; var ag = new double[definition.CellCount][]; var ab = new double[definition.CellCount][];
            for (int cell = 0; cell < definition.CellCount; cell++)
            {
                red[cell] = new double[count]; green[cell] = new double[count]; blue[cell] = new double[count];
                ar[cell] = new double[count]; ag[cell] = new double[count]; ab[cell] = new double[count];
                for (int sample = 0; sample < count; sample++)
                {
                    CameraProjection.TopPoint(definition, cell % definition.Width, cell / definition.Width,
                        sample % perAxis, sample / perAxis, perAxis, calibration.Inset, calibration.SurfaceY, out double x, out double y, out double z);
                    Vector3 point = new Vector3((float)x, (float)y, (float)z);
                    Vector3 cameraUV = observationCamera.WorldToViewportPoint(point);
                    // Match the exact camera pixel read by RgbImage.TryRead, not the ideal subpixel grid point.
                    cameraUV.x = (Mathf.Floor(cameraUV.x * image.Width) + 0.5f) / image.Width;
                    cameraUV.y = 1 - (Mathf.Floor((1 - cameraUV.y) * image.Height) + 0.5f) / image.Height;
                    Ray ray = observationCamera.ViewportPointToRay(cameraUV);
                    point = ray.GetPoint(((float)calibration.SurfaceY - ray.origin.y) / ray.direction.y);
                    Vector3 uv = contentCamera.WorldToViewportPoint(point);
                    Color ambient = ambientResponse.GetPixelBilinear(cameraUV.x, cameraUV.y) * 2;
                    red[cell][sample] = ambient.r * image.ExposureScale / 8;
                    green[cell][sample] = ambient.g * image.ExposureScale / 8; blue[cell][sample] = ambient.b * image.ExposureScale / 8;
                    ar[cell][sample] = red[cell][sample]; ag[cell][sample] = green[cell][sample]; ab[cell][sample] = blue[cell][sample];
                    if (!source.IsVisible || uv.z < contentCamera.nearClipPlane || uv.z > contentCamera.farClipPlane || uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1) continue;
                    Vector2 step = new Vector2(blurPixels / input.width, blurPixels / input.height);
                    Color color = observationOutput.GetPixelBilinear(uv.x, uv.y) * 0.5f;
                    color += (observationOutput.GetPixelBilinear(uv.x + step.x, uv.y) + observationOutput.GetPixelBilinear(uv.x - step.x, uv.y)
                        + observationOutput.GetPixelBilinear(uv.x, uv.y + step.y) + observationOutput.GetPixelBilinear(uv.x, uv.y - step.y)) * 0.125f;
                    color = Color.Lerp(color, Color.white, blackLevel);
                    Vector3 direction = projectorPosition - point;
                    float response = Mathf.Clamp01(direction.normalized.y) * Mathf.Min(1.5f, 196f / Mathf.Max(1, direction.sqrMagnitude)) * lightStrength;
                    red[cell][sample] = Mathf.Clamp01((ambient.r + color.r * response) * image.ExposureScale / 8);
                    green[cell][sample] = Mathf.Clamp01((ambient.g + color.g * response) * image.ExposureScale / 8);
                    blue[cell][sample] = Mathf.Clamp01((ambient.b + color.b * response) * image.ExposureScale / 8);
                }
            }
            var reflectance = new LinearRgb[calibration.Colors.Count];
            for (int i = 0; i < reflectance.Length; i++)
            {
                RgbColor c = calibration.Colors[i].Value;
                reflectance[i] = new LinearRgb(c.R / 255.0, c.G / 255.0, c.B / 255.0);
            }
            profile = PhotometricProfile.Create(calibration.CalibrationId, true, new LinearRgb(0, 0, 0),
                new LinearRgb(0, 0, 0), new LinearRgb(8, 8, 8),
                new LinearRgb(1, 1, 1), reflectance, 1);
            projection = CellProjection.FromSamples(red, green, blue, ar, ag, ab);
        }

        private void CollectReceivers(Scene scene, int cameraMask)
        {
            receivers.Clear();
            if (!scene.IsValid() || !scene.isLoaded) return;
            int excluded = (1 << SourceLayer) | (1 << 30) | (1 << SimulationRoundMarkers.LayerIndex) | (1 << 5);
            var region = new Bounds(new Vector3(5, 2, 5), new Vector3(14, 8, 14));
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>())
                {
                    int mask = 1 << renderer.gameObject.layer;
                    if (!renderer.gameObject.activeInHierarchy || !renderer.enabled || renderer.forceRenderingOff || (mask & excluded) != 0 ||
                        (mask & cameraMask) == 0 || !region.Intersects(renderer.bounds)) continue;
                    if (renderer.GetComponent<MeshFilter>() == null) continue;
                    receivers.Add(renderer);
                }
        }

        private void RenderOcclusion(ViewFrame frame, Matrix4x4 vp, Matrix4x4 view)
        {
            if (frame.Depth == null || frame.Depth.width != outputResolution)
            {
                Release(frame.Depth);
                var format = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RFloat)
                    ? RenderTextureFormat.RFloat : RenderTextureFormat.ARGBFloat;
                frame.Depth = new RenderTexture(outputResolution, outputResolution, 24, format)
                    { name = "VirtualProjectorOcclusion", hideFlags = HideFlags.HideAndDontSave,
                        filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                frame.Depth.Create();
            }
            depthCommands.Clear();
            depthCommands.SetRenderTarget(frame.Depth);
            depthCommands.ClearRenderTarget(true, true, Color.white);
            depthCommands.SetGlobalMatrix("_VPMatrix", vp); depthCommands.SetGlobalMatrix("_VPView", view);
            depthCommands.SetGlobalFloat("_VPFar", Far);
            foreach (MeshRenderer receiver in receivers)
            {
                Material[] materials = receiver.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                    if (materials[i] != null && materials[i].renderQueue < 2500) depthCommands.DrawRenderer(receiver, depthMaterial, i, 0);
            }
            RenderTexture previous = RenderTexture.active;
            try { Graphics.ExecuteCommandBuffer(depthCommands); }
            finally { RenderTexture.active = previous; }
        }
    }
}
