using System;
using System.Collections.Generic;
using Brick2Life.Battle;
using Brick2Life.Core;
using UnityEngine;

namespace Brick2Life.Feedback
{
    /// <summary>
    /// Thin facade kept for serialized scene references and controller call sites. Everything the battle
    /// needs is drawn by SimulationBattleOverlay on the projection layer (31); this component creates no
    /// layer-30 stand-ins any more, so recognition never sees combat props.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SimulationBattleView : MonoBehaviour
    {
        [Tooltip("保留的旧字段：实体替身已关闭，这里只用于场景序列化兼容。")]
        [SerializeField] private Transform root;
        [SerializeField] private Material enemyMaterial;
        [SerializeField] private Material routeMaterial;
        [SerializeField] private float enemyScale = 0.45f;
        [SerializeField] private float routeWidth = 0.08f;
        [SerializeField] private string gameLayerName = "Brick2LifeGame";

        private SimulationBattleOverlay overlay;

        public int EnemyCount => Overlay != null ? Overlay.ActiveEnemyCount : 0;
        public int RouteCount
        {
            get
            {
                if (Overlay != null) return Overlay.RouteCount;
                var controller = FindFirstObjectByType<SimulationBattleController>();
                return controller != null && controller.DisplayRoutes != null ? controller.DisplayRoutes.Count : 0;
            }
        }
        public int SpawnMarkerCount => Overlay != null ? Overlay.SpawnMarkerCount : 0;
        public SimulationBattleOverlay Overlay
        {
            get
            {
                if (overlay == null) overlay = GetComponent<SimulationBattleOverlay>();
                if (overlay == null) overlay = FindFirstObjectByType<SimulationBattleOverlay>();
                return overlay;
            }
        }

        private void Awake()
        {
            if (root == null) root = transform;
        }

        // Routes and enemies are owned by the overlay; Build only asks it to refresh confirmed geometry.
        public void Build(BattleLevel level, IReadOnlyList<BattleRoute> routes = null)
        {
            if (Overlay != null) Overlay.RefreshLayout();
        }

        public void Render(BattleState state, BattleLevel level)
        {
            // The overlay animates itself in Update; nothing to draw here.
        }

        public void ClearEnemies() { }

        public void ClearRoutes() { }

        public void SetSpawns(IReadOnlyList<GridPoint> cells)
        {
            if (Overlay != null) Overlay.SetSpawnMarkers(cells);
        }

        public void ClearSpawns()
        {
            if (Overlay != null) Overlay.ClearSpawnMarkers();
        }
    }
}
