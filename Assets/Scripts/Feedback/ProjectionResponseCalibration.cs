using System;
using System.Collections.Generic;
using Brick2Life.Simulation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Brick2Life.Feedback
{
    // A known gray reference, never the player's board. Calibrates the existing Gamma renderer without changing its appearance.
    internal sealed class ProjectionResponseCalibration : IDisposable
    {
        private const int ReferenceLayer = 28;
        private readonly Camera camera;
        private readonly GameObject surface;
        private readonly Renderer renderer;
        private readonly Material material;
        private readonly Dictionary<double, Texture2D> maps = new Dictionary<double, Texture2D>();
        private string configuration;

        public ProjectionResponseCalibration(Scene scene)
        {
            var cameraObject = new GameObject("ProjectionResponseReferenceCamera", typeof(Camera)) { hideFlags = HideFlags.HideAndDontSave };
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            camera = cameraObject.GetComponent<Camera>(); camera.enabled = false;
            surface = GameObject.CreatePrimitive(PrimitiveType.Quad); surface.name = "ProjectionResponseGrayReference";
            surface.hideFlags = HideFlags.HideAndDontSave; surface.layer = ReferenceLayer;
            SceneManager.MoveGameObjectToScene(surface, scene);
            surface.GetComponent<Collider>().enabled = false;
            surface.transform.rotation = Quaternion.Euler(90, 0, 0); surface.transform.localScale = new Vector3(14, 14, 1);
            material = new Material(Shader.Find("Standard")) { color = new Color(0.5f, 0.5f, 0.5f), hideFlags = HideFlags.HideAndDontSave };
            renderer = surface.GetComponent<Renderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false; renderer.forceRenderingOff = true;
        }

        public Texture2D Get(Camera observer, SimulationObservationImage image, double height)
        {
            string key = image.Width + ":" + image.Height + ":" + string.Join(",", image.WorldToCamera) + ":" + string.Join(",", image.Projection)
                + JsonUtility.ToJson(SimulationLightingConfiguration.SnapshotScene());
            if (configuration != key)
            {
                foreach (Texture2D map in maps.Values) Destroy(map);
                maps.Clear(); configuration = key;
            }
            if (maps.TryGetValue(height, out Texture2D response)) return response;
            camera.CopyFrom(observer); camera.enabled = false;
            camera.transform.SetPositionAndRotation(observer.transform.position, observer.transform.rotation);
            camera.cullingMask = 1 << ReferenceLayer; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            camera.allowHDR = false; camera.allowMSAA = false;
            surface.transform.position = new Vector3(5, (float)height, 5);
            var target = RenderTexture.GetTemporary(image.Width, image.Height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            try
            {
                response = new Texture2D(image.Width, image.Height, TextureFormat.RGB24, false, true)
                    { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
                renderer.forceRenderingOff = false; camera.targetTexture = target; camera.Render();
                RenderTexture.active = target; response.ReadPixels(new Rect(0, 0, image.Width, image.Height), 0, 0); response.Apply(false, false);
                maps.Add(height, response); return response;
            }
            catch { Destroy(response); throw; }
            finally { renderer.forceRenderingOff = true; camera.targetTexture = null; RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); }
        }

        public void Dispose()
        {
            foreach (Texture2D map in maps.Values) Destroy(map);
            maps.Clear(); Destroy(camera.gameObject); Destroy(surface); Destroy(material);
        }
        private static void Destroy(UnityEngine.Object item)
        { if (item != null) { if (UnityEngine.Application.isPlaying) UnityEngine.Object.Destroy(item); else UnityEngine.Object.DestroyImmediate(item); } }
    }
}
