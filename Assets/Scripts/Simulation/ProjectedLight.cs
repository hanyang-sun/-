namespace Brick2Life.Simulation
{
    /// <summary>
    /// The light the projector just drew, in board coordinates. Recognition samples it
    /// so a photo that includes the projection can be unblended without turning the lamp off.
    /// </summary>
    public interface IProjectedLightSample
    {
        float PlaneY { get; }
        bool TrySample(float boardX, float boardY, out float red, out float green, out float blue, out float alpha);
    }

    public static class ProjectedLight
    {
        public static IProjectedLightSample Current { get; private set; }

        public static void Register(IProjectedLightSample sample)
        {
            if (sample != null) Current = sample;
        }

        public static void Unregister(IProjectedLightSample sample)
        {
            if (ReferenceEquals(Current, sample)) Current = null;
        }
    }
}
