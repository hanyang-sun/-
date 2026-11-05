using System.Collections.Generic;
using Brick2Life.Application;
using Brick2Life.Battle;
using Brick2Life.Core;
using Brick2Life.Simulation;
using UnityEngine;
using UnityEngine.Rendering;

namespace Brick2Life.Feedback
{
    /// <summary>
    /// Paints the battle onto one board-aligned sheet: cyan path, orange enemies, dashed range,
    /// spawn and goal rings, orange aim and shot beams. The same <see cref="AudienceProjectionFrame"/>
    /// is what a later hardware projector should display. Recognition no longer hides this sheet.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SimulationBattleOverlay : MonoBehaviour, IProjectionGate, IProjectedLightSample
    {
        [SerializeField] private SimulationBattleController controller;
        [SerializeField] private ProjectionSimulator projector;
        [SerializeField] private Transform root;
        [SerializeField] private string projectionLayerName = "Brick2LifeProjection";
        [SerializeField, Range(0, 31)] private int fallbackLayer = 31;
        [SerializeField, Min(1)] private int arrowsPerRoute = 7;
        [SerializeField, Min(0.05f)] private float arrowSize = 0.34f;
        [SerializeField, Min(0.1f)] private float arrowSpeed = 1.6f;
        [SerializeField, Min(0.05f)] private float ringHeight = 0.02f;
        [SerializeField, Min(0.1f)] private float flashSeconds = 0.18f;
        [SerializeField, Min(0.1f)] private float rippleSeconds = 0.5f;

        private sealed class Transient
        {
            public bool Beam;
            public AudienceRingKind Kind;
            public float X, Y, X1, Y1;
            public float Age, Life, StartRadius, EndRadius;
        }

        private readonly List<Transient> transients = new List<Transient>();
        private readonly List<GridPoint> spawnCells = new List<GridPoint>();
        private readonly List<float> wallCooldown = new List<float>();
        private readonly AudienceProjectionFrame frame = new AudienceProjectionFrame();
        private GameObject sheet;
        private Renderer sheetRenderer;
        private Texture2D sheetTexture;
        private Material sheetMaterial;
        private byte[] sheetPixels;
        private int projectionLayer;
        private int lastCoreHealth = -1;
        private long lastShotSequence = -1;
        private BattleLevel lastLevel;
        private bool captureVisible = true;
        private float animationTime;

        public AudienceProjectionFrame Frame => frame;
        public Transform MarksRoot => root;
        public BattleLevel DisplayedLevel => lastLevel;
        public int ActiveEnemyCount => frame.Dots.Count;
        public int SpawnMarkerCount => spawnCells.Count;
        public int RouteCount => controller != null && controller.DisplayRoutes != null ? controller.DisplayRoutes.Count : 0;
        public int ActiveArrowCount => frame.Paths.Count * arrowsPerRoute;
        public int ActiveRangeRingCount
        {
            get
            {
                int count = 0;
                foreach (AudienceRing ring in frame.Rings) if (ring.Kind == AudienceRingKind.Range) count++;
                return count;
            }
        }
        public int ActiveTowerArrowCount
        {
            get
            {
                int count = 0;
                foreach (AudienceBeam beam in frame.Beams) if (beam.Aim) count++;
                return count;
            }
        }
        public int ActiveEffectCount => transients.Count;
        public int PathSegmentCount
        {
            get
            {
                int count = 0;
                foreach (AudiencePath path in frame.Paths) count += path.Points.Length - 1;
                return count;
            }
        }

        bool IProjectionGate.IsProjectionVisible => captureVisible;
        void IProjectionGate.SetProjectionVisible(bool value)
        {
            captureVisible = value;
            ApplyCaptureVisibility();
        }

        private void OnEnable()
        {
            ProjectionCaptureGate.Register(this);
            ProjectedLight.Register(this);
        }
        private void OnDisable()
        {
            ProjectedLight.Unregister(this);
            ProjectionCaptureGate.Unregister(this);
            if (sheet != null) sheet.SetActive(false);
        }

        float IProjectedLightSample.PlaneY => sheet != null ? sheet.transform.position.y : 0.03f;

        bool IProjectedLightSample.TrySample(float boardX, float boardY, out float red, out float green, out float blue, out float alpha)
        {
            red = green = blue = alpha = 0f;
            if (sheet == null || !sheet.activeInHierarchy || sheetRenderer == null || sheetRenderer.forceRenderingOff) return false;
            if (sheetPixels == null || sheetTexture == null) return false;
            float board = BoardSize();
            if (board <= 0f || boardX < 0f || boardY < 0f || boardX > board || boardY > board) return false;
            int resolution = sheetTexture.width;
            float pixelX = boardX / board * resolution - 0.5f;
            float pixelY = boardY / board * resolution - 0.5f;
            SampleBilinear(sheetPixels, resolution, pixelX, pixelY, out red, out green, out blue, out alpha);
            return alpha > 0.001f;
        }

        private static void SampleBilinear(byte[] pixels, int resolution, float pixelX, float pixelY,
            out float red, out float green, out float blue, out float alpha)
        {
            int x0 = Mathf.Clamp(Mathf.FloorToInt(pixelX), 0, resolution - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(pixelY), 0, resolution - 1);
            int x1 = Mathf.Min(x0 + 1, resolution - 1);
            int y1 = Mathf.Min(y0 + 1, resolution - 1);
            float tx = Mathf.Clamp01(pixelX - x0);
            float ty = Mathf.Clamp01(pixelY - y0);
            ReadPixel(pixels, resolution, x0, y0, out float r00, out float g00, out float b00, out float a00);
            ReadPixel(pixels, resolution, x1, y0, out float r10, out float g10, out float b10, out float a10);
            ReadPixel(pixels, resolution, x0, y1, out float r01, out float g01, out float b01, out float a01);
            ReadPixel(pixels, resolution, x1, y1, out float r11, out float g11, out float b11, out float a11);
            red = Lerp(Lerp(r00, r10, tx), Lerp(r01, r11, tx), ty);
            green = Lerp(Lerp(g00, g10, tx), Lerp(g01, g11, tx), ty);
            blue = Lerp(Lerp(b00, b10, tx), Lerp(b01, b11, tx), ty);
            alpha = Lerp(Lerp(a00, a10, tx), Lerp(a01, a11, tx), ty);
        }

        private static void ReadPixel(byte[] pixels, int resolution, int x, int y, out float red, out float green, out float blue, out float alpha)
        {
            int index = (y * resolution + x) * 4;
            red = pixels[index] / 255f;
            green = pixels[index + 1] / 255f;
            blue = pixels[index + 2] / 255f;
            alpha = pixels[index + 3] / 255f;
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;

        private void Awake()
        {
            if (controller == null) controller = FindFirstObjectByType<SimulationBattleController>();
            if (projector == null) projector = FindFirstObjectByType<ProjectionSimulator>();
            if (root == null || root == transform)
            {
                root = new GameObject("BattleProjection").transform;
                root.SetParent(transform, false);
            }
            projectionLayer = LayerMask.NameToLayer(projectionLayerName);
            if (projectionLayer < 0) projectionLayer = fallbackLayer;
            EnsureSheet();
        }

        private void OnDestroy()
        {
            if (sheetTexture != null) Destroy(sheetTexture);
            if (sheetMaterial != null) Destroy(sheetMaterial);
            if (sheet != null) Destroy(sheet);
        }

        private void Update()
        {
            SynchronizeLevel();
            bool running = controller != null && controller.State != null && !controller.State.Paused && !controller.State.Finished;
            if (running) animationTime += Time.deltaTime;
            AgeTransients();
            RebuildFrame();
        }

        public void RefreshLayout()
        {
            SynchronizeLevel();
            RebuildFrame();
        }

        public void SetSpawnMarkers(IReadOnlyList<GridPoint> cells)
        {
            spawnCells.Clear();
            if (cells != null) foreach (GridPoint cell in cells) spawnCells.Add(cell);
            RebuildFrame();
        }

        public void ClearSpawnMarkers()
        {
            spawnCells.Clear();
            RebuildFrame();
        }

        private void SynchronizeLevel()
        {
            if (controller == null || lastLevel == controller.Level) return;
            transients.Clear();
            wallCooldown.Clear();
            lastLevel = controller.Level;
            lastCoreHealth = -1;
            lastShotSequence = -1;
            animationTime = 0f;
        }

        private void AgeTransients()
        {
            if (controller == null || controller.Battle == null)
            {
                transients.Clear();
                return;
            }
            bool paused = controller.State != null && controller.State.Paused;
            float delta = paused ? 0f : Time.deltaTime;
            for (int i = wallCooldown.Count - 1; i >= 0; i--) wallCooldown[i] = Mathf.Max(0f, wallCooldown[i] - delta);
            for (int i = transients.Count - 1; i >= 0; i--)
            {
                transients[i].Age += delta;
                if (transients[i].Age >= transients[i].Life) transients.RemoveAt(i);
            }
        }

        private void RebuildFrame()
        {
            frame.Clear();
            bool projectionOn = projector == null || projector.IsVisible;
            bool showBattle = projectionOn && controller != null && controller.State != null &&
                !controller.State.WaitingForLayout && controller.Battle != null;
            if (showBattle)
            {
                HandleShots();
                HandleCore(controller.State);
                if (!controller.State.Paused && !controller.State.Finished) HandleWallRipples(controller.State);
                AddPaths();
                AddTowers(controller.State);
                AddEnemies(controller.State);
                AddGoal();
                AddTransients();
            }
            if (projectionOn) AddSpawns();
            frame.Flow = animationTime * arrowSpeed;
            bool hasContent = frame.Paths.Count + frame.Rings.Count + frame.Dots.Count + frame.Beams.Count > 0;
            if (!hasContent || sheet == null)
            {
                if (sheet != null) sheet.SetActive(false);
                return;
            }
            sheet.SetActive(true);
            PlaceSheet();
            PaintSheet();
            ApplyCaptureVisibility();
        }

        private void AddPaths()
        {
            IReadOnlyList<BattleRoute> routes = controller.DisplayRoutes;
            if (routes == null) return;
            foreach (BattleRoute route in routes)
            {
                if (route.Route == null || route.Route.Count < 2) continue;
                var points = new List<AudiencePoint>(route.Route.Count);
                foreach (GridPoint cell in route.Route)
                    points.Add(new AudiencePoint(cell.X * 0.5f, cell.Y * 0.5f));
                frame.Paths.Add(new AudiencePath(points, Mathf.Max(0.2f, arrowSize)));
            }
        }

        private void AddTowers(BattleState state)
        {
            BattleLevel level = controller.Level;
            if (level == null) return;
            float radius = (float)(controller.Battle != null ? controller.Battle.Towers.Range : controller.TowerSettings.Range) * 0.5f;
            if (radius <= 0.05f) return;
            var seen = new HashSet<string>();
            foreach (BattleCell cell in level.Cells)
            {
                if (cell.Role != GameRole.Tower || !seen.Add(cell.SourceBrickId)) continue;
                Vector2 center = BoardCenter(cell);
                bool invaded = false;
                foreach (EnemyState enemy in state.Enemies)
                {
                    float dx = (float)enemy.X * 0.5f - center.x;
                    float dy = (float)enemy.Y * 0.5f - center.y;
                    if (dx * dx + dy * dy <= radius * radius) { invaded = true; break; }
                }
                frame.Rings.Add(new AudienceRing(center.x, center.y, radius, AudienceRingKind.Range, invaded, 1f));
                Vector2 aim = new Vector2(center.x + 0.45f, center.y);
                float best = float.MaxValue;
                foreach (EnemyState enemy in state.Enemies)
                {
                    float ex = (float)enemy.X * 0.5f, ey = (float)enemy.Y * 0.5f;
                    float dx = ex - center.x, dy = ey - center.y;
                    float distance = dx * dx + dy * dy;
                    if (distance >= best) continue;
                    best = distance;
                    aim = new Vector2(ex, ey);
                }
                frame.Beams.Add(new AudienceBeam(center.x, center.y, aim.x, aim.y, 0.95f, true));
            }
        }

        private void AddEnemies(BattleState state)
        {
            foreach (EnemyState enemy in state.Enemies)
                frame.Dots.Add(new AudienceDot((float)enemy.X * 0.5f, (float)enemy.Y * 0.5f, 0.22f, 1f));
        }

        private void AddGoal()
        {
            BattleLevel level = controller.Level;
            if (level == null) return;
            var seen = new HashSet<string>();
            foreach (BattleCell cell in level.Cells)
            {
                if (cell.Role != GameRole.Core || !seen.Add(cell.SourceBrickId)) continue;
                Vector2 center = BoardCenter(cell);
                frame.Rings.Add(new AudienceRing(center.x, center.y, 0.55f, AudienceRingKind.Goal, false, 1f));
            }
        }

        private void AddSpawns()
        {
            foreach (GridPoint cell in spawnCells)
                frame.Rings.Add(new AudienceRing(cell.X + 0.5f, cell.Y + 0.5f, 0.42f, AudienceRingKind.Spawn, false, 1f));
        }

        private void AddTransients()
        {
            foreach (Transient fx in transients)
            {
                float t = fx.Life <= 0f ? 1f : Mathf.Clamp01(fx.Age / fx.Life);
                float alpha = 1f - t;
                if (fx.Beam)
                {
                    frame.Beams.Add(new AudienceBeam(fx.X, fx.Y, fx.X1, fx.Y1, alpha, false));
                    continue;
                }
                float radius = Mathf.Lerp(fx.StartRadius, fx.EndRadius, t);
                if (radius <= 0.02f) continue;
                frame.Rings.Add(new AudienceRing(fx.X, fx.Y, radius, fx.Kind, fx.Kind == AudienceRingKind.Impact, alpha));
            }
        }

        private void HandleShots()
        {
            if (lastShotSequence == controller.ShotSequence) return;
            lastShotSequence = controller.ShotSequence;
            IReadOnlyList<ShotEvent> shots = controller.LastShots;
            if (shots == null) return;
            foreach (ShotEvent shot in shots)
            {
                transients.Add(new Transient
                {
                    Beam = true,
                    X = (float)shot.TowerX * 0.5f, Y = (float)shot.TowerY * 0.5f,
                    X1 = (float)shot.TargetX * 0.5f, Y1 = (float)shot.TargetY * 0.5f,
                    Life = flashSeconds
                });
                transients.Add(new Transient
                {
                    Kind = AudienceRingKind.Ripple,
                    X = (float)shot.TargetX * 0.5f, Y = (float)shot.TargetY * 0.5f,
                    StartRadius = 0.12f, EndRadius = 0.42f, Life = rippleSeconds
                });
            }
        }

        private void HandleCore(BattleState state)
        {
            if (lastCoreHealth < 0) { lastCoreHealth = state.CoreHealth; return; }
            if (state.CoreHealth >= lastCoreHealth) { lastCoreHealth = state.CoreHealth; return; }
            lastCoreHealth = state.CoreHealth;
            BattleLevel level = controller.Level;
            if (level == null) return;
            foreach (BattleCell cell in level.Cells)
            {
                if (cell.Role != GameRole.Core) continue;
                Vector2 center = BoardCenter(cell);
                transients.Add(new Transient
                {
                    Kind = AudienceRingKind.Impact,
                    X = center.x, Y = center.y,
                    StartRadius = 0.35f, EndRadius = 0.85f, Life = rippleSeconds * 1.4f
                });
                break;
            }
        }

        private void HandleWallRipples(BattleState state)
        {
            BattleLevel level = controller.Level;
            if (level == null) return;
            while (wallCooldown.Count < level.Cells.Count) wallCooldown.Add(0f);
            foreach (EnemyState enemy in state.Enemies)
            {
                float wx = (float)enemy.X * 0.5f, wy = (float)enemy.Y * 0.5f;
                for (int i = 0; i < level.Cells.Count; i++)
                {
                    BattleCell cell = level.Cells[i];
                    if (cell.Role != GameRole.Wall) continue;
                    float cx = cell.Position.X * 0.5f + 0.25f, cy = cell.Position.Y * 0.5f + 0.25f;
                    if (Mathf.Abs(cx - wx) > 0.8f || Mathf.Abs(cy - wy) > 0.8f) continue;
                    if (wallCooldown[i] > 0f) continue;
                    wallCooldown[i] = 0.7f;
                    transients.Add(new Transient
                    {
                        Kind = AudienceRingKind.Ripple,
                        X = cx, Y = cy, StartRadius = 0.12f, EndRadius = 0.35f, Life = rippleSeconds
                    });
                    break;
                }
            }
        }

        private void EnsureSheet()
        {
            if (sheet != null) return;
            sheet = GameObject.CreatePrimitive(PrimitiveType.Quad);
            sheet.name = "AudienceSheet";
            sheet.layer = projectionLayer;
            sheet.transform.SetParent(root, false);
            sheet.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Collider collider = sheet.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            sheetRenderer = sheet.GetComponent<Renderer>();
            sheetRenderer.shadowCastingMode = ShadowCastingMode.Off;
            sheetRenderer.receiveShadows = false;
            sheetTexture = new Texture2D(AudienceProjectionRaster.DefaultResolution, AudienceProjectionRaster.DefaultResolution, TextureFormat.RGBA32, false)
            {
                name = "AudienceProjection",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            Shader shader = Shader.Find("Brick2Life/AudienceProjection");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            sheetMaterial = new Material(shader) { name = "AudienceProjection", mainTexture = sheetTexture, color = Color.white };
            sheetRenderer.sharedMaterial = sheetMaterial;
            sheet.SetActive(false);
        }

        private void PlaceSheet()
        {
            float board = BoardSize();
            sheet.transform.position = new Vector3(board * 0.5f, Mathf.Max(0.03f, ringHeight), board * 0.5f);
            sheet.transform.localScale = new Vector3(board, board, 1f);
        }

        private void PaintSheet()
        {
            if (sheetTexture == null) return;
            float board = BoardSize();
            sheetPixels = AudienceProjectionRaster.Draw(frame, board, board, sheetTexture.width, sheetPixels);
            sheetTexture.LoadRawTextureData(sheetPixels);
            sheetTexture.Apply(false, false);
        }

        private void ApplyCaptureVisibility()
        {
            if (sheetRenderer != null) sheetRenderer.forceRenderingOff = !captureVisible;
        }

        private float BoardSize()
        {
            if (controller != null && controller.Level != null)
                return controller.Level.Width / (float)BattleLevel.NavigationPerPhysical;
            return 10f;
        }

        private static Vector2 BoardCenter(BattleCell cell) =>
            new Vector2(cell.Position.X / 2f + 0.5f, cell.Position.Y / 2f + 0.5f);
    }
}
