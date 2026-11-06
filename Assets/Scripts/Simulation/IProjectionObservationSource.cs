using Brick2Life.Core;
using Brick2Life.Vision;

namespace Brick2Life.Simulation
{
    // Known projector output and public calibration only. No scene occupancy, albedo or depth is returned.
    public interface IProjectionObservationSource
    {
        void GetObservationLighting(BoardDefinition definition, VisionCalibration calibration, SimulationObservationImage image,
            out PhotometricProfile profile, out CellProjection projection);
    }
}
