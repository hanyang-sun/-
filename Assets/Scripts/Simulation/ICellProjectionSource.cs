using Brick2Life.Core;

namespace Brick2Life.Simulation
{
    // The live projector command for one exposure. Implemented by the projection surface; capture does not blank it.
    public interface ICellProjectionSource
    {
        bool TryCopyCellLight(BoardDefinition definition, double[] red, double[] green, double[] blue);
    }
}
