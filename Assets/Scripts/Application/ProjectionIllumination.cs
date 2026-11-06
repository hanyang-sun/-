using System;
using Brick2Life.Core;

namespace Brick2Life.Application
{
    // Light actually landing on each cell center while the projector stays on. Rings are outlines and do not cover that center.
    public static class ProjectionIllumination
    {
        public static void Accumulate(ProjectionPlan plan, double intensity, double changeRemaining, double[] red, double[] green, double[] blue)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (red == null || green == null || blue == null || red.Length != plan.Definition.CellCount ||
                green.Length != red.Length || blue.Length != red.Length)
                throw new ArgumentException("Illumination buffers must match the board.");
            if (double.IsNaN(intensity) || intensity < 0 || intensity > 1) throw new ArgumentOutOfRangeException(nameof(intensity));
            if (double.IsNaN(changeRemaining) || changeRemaining < 0 || changeRemaining > 1)
                throw new ArgumentOutOfRangeException(nameof(changeRemaining));
            foreach (ProjectionMark mark in plan.Marks)
            {
                if (mark.Shape == ProjectionShape.Ring || mark.Color.Alpha <= 0 || intensity <= 0) continue;
                double alpha = mark.Color.Alpha * intensity;
                if (mark.Kind == ProjectionMarkKind.ChangeHighlight) alpha *= changeRemaining;
                if (alpha <= 0) continue;
                int x0 = (int)Math.Floor(mark.X);
                int y0 = (int)Math.Floor(mark.Y);
                int x1 = (int)Math.Ceiling(mark.X + mark.SizeX) - 1;
                int y1 = (int)Math.Ceiling(mark.Y + mark.SizeY) - 1;
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                    {
                        if (!plan.Definition.Contains(x, y)) continue;
                        double centerX = x + 0.5, centerY = y + 0.5;
                        if (centerX < mark.X || centerY < mark.Y || centerX > mark.X + mark.SizeX || centerY > mark.Y + mark.SizeY) continue;
                        int index = plan.Definition.Index(x, y);
                        red[index] = Math.Min(1, red[index] + mark.Color.Red * alpha);
                        green[index] = Math.Min(1, green[index] + mark.Color.Green * alpha);
                        blue[index] = Math.Min(1, blue[index] + mark.Color.Blue * alpha);
                    }
            }
        }
    }
}
