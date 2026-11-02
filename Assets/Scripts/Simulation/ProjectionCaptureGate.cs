using System;
using System.Collections.Generic;

namespace Brick2Life.Simulation
{
    /// <summary>
    /// A projection surface that can be hidden while the overhead camera takes a sample.
    /// Implemented by task C's projector; kept in the simulation assembly so the capture adapter
    /// never depends on the feedback assembly.
    /// </summary>
    public interface IProjectionGate
    {
        bool IsProjectionVisible { get; }
        void SetProjectionVisible(bool visible);
    }

    /// <summary>Shared capture visibility, including captures that span displayed frames.</summary>
    public static class ProjectionCaptureGate
    {
        private static readonly List<IProjectionGate> gates = new List<IProjectionGate>();
        private static int suppressionDepth;
        public static bool IsSuppressed => suppressionDepth > 0;
        public static event Action VisibilityChanged;
        public static IProjectionGate Current => gates.Count == 0 ? null : gates[gates.Count - 1];

        public static void Register(IProjectionGate gate)
        {
            if (gate != null && !gates.Contains(gate)) gates.Add(gate);
        }

        public static void Unregister(IProjectionGate gate)
        {
            gates.Remove(gate);
        }

        public static IDisposable Suppress() => new CaptureScope();

        private static void NotifyVisibilityChanged()
        {
            if (VisibilityChanged == null) return;
            foreach (Action listener in VisibilityChanged.GetInvocationList())
                try { listener(); }
                catch (Exception error) { UnityEngine.Debug.LogException(error); }
        }

        private sealed class CaptureScope : IDisposable
        {
            private readonly List<IProjectionGate> hidden = new List<IProjectionGate>();
            private bool disposed;
            public CaptureScope()
            {
                suppressionDepth++;
                try
                {
                    foreach (IProjectionGate gate in gates.ToArray())
                        if (gate.IsProjectionVisible)
                        {
                            hidden.Add(gate);
                            gate.SetProjectionVisible(false);
                        }
                    if (suppressionDepth == 1) NotifyVisibilityChanged();
                }
                catch { Dispose(); throw; }
            }
            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                try
                {
                    for (int i = hidden.Count - 1; i >= 0; i--)
                        try { hidden[i].SetProjectionVisible(true); }
                        catch (Exception error) { UnityEngine.Debug.LogException(error); }
                }
                finally
                {
                    hidden.Clear();
                    suppressionDepth--;
                    if (suppressionDepth == 0) NotifyVisibilityChanged();
                }
            }
        }
    }
}
