using System.Collections.Generic;
using Brick2Life.Core;
using UnityEngine;

namespace Brick2Life.Feedback
{
    /// <summary>
    /// Battle presentation counters and commands. SimulationBattleOverlay owns all output content.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SimulationBattleView : MonoBehaviour
    {
        private SimulationBattleOverlay overlay;

        public int EnemyCount => Overlay != null ? Overlay.ActiveEnemyCount : 0;
        public int RouteCount => Overlay != null ? Overlay.RouteCount : 0;
        public int SpawnMarkerCount => Overlay != null ? Overlay.SpawnMarkerCount : 0;
        public SimulationBattleOverlay Overlay
        {
            get
            {
                if (overlay == null) overlay = GetComponent<SimulationBattleOverlay>();
                return overlay;
            }
        }

        public void RefreshLayout()
        {
            if (Overlay != null) Overlay.RefreshLayout();
        }

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
