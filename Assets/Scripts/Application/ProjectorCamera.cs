using System;
using System.Collections.Generic;

namespace Brick2Life.Application
{
    /// <summary>
    /// How a real camera records projector light: observed = surface + M * (commandedColor * alpha).
    /// M is fit once from photographs of known patches. The virtual sheet uses a different blend.
    /// </summary>
    public sealed class AdditiveLightModel
    {
        private readonly float[] redRow;
        private readonly float[] greenRow;
        private readonly float[] blueRow;

        private AdditiveLightModel(float[] redRow, float[] greenRow, float[] blueRow)
        {
            this.redRow = redRow;
            this.greenRow = greenRow;
            this.blueRow = blueRow;
        }

        public static AdditiveLightModel Fit(IReadOnlyList<LightObservation> observations)
        {
            if (observations == null || observations.Count < 3)
                throw new ArgumentException("Fitting the camera response needs at least three patches.", nameof(observations));
            var ata = new double[3, 3];
            var redTarget = new double[3];
            var greenTarget = new double[3];
            var blueTarget = new double[3];
            int used = 0;
            foreach (LightObservation sample in observations)
            {
                float alpha = sample.Alpha;
                if (float.IsNaN(alpha) || alpha < 0.2f) continue;
                double lightRed = sample.LightRed * alpha;
                double lightGreen = sample.LightGreen * alpha;
                double lightBlue = sample.LightBlue * alpha;
                if (lightRed * lightRed + lightGreen * lightGreen + lightBlue * lightBlue < 1e-4) continue;
                if (Saturated(sample.ObservedRed) || Saturated(sample.ObservedGreen) || Saturated(sample.ObservedBlue))
                    continue;
                Accumulate(ata, lightRed, lightGreen, lightBlue);
                double addedRed = sample.ObservedRed - sample.SurfaceRed;
                double addedGreen = sample.ObservedGreen - sample.SurfaceGreen;
                double addedBlue = sample.ObservedBlue - sample.SurfaceBlue;
                redTarget[0] += lightRed * addedRed;
                redTarget[1] += lightGreen * addedRed;
                redTarget[2] += lightBlue * addedRed;
                greenTarget[0] += lightRed * addedGreen;
                greenTarget[1] += lightGreen * addedGreen;
                greenTarget[2] += lightBlue * addedGreen;
                blueTarget[0] += lightRed * addedBlue;
                blueTarget[1] += lightGreen * addedBlue;
                blueTarget[2] += lightBlue * addedBlue;
                used++;
            }
            if (used < 3) throw new ArgumentException("Need unsaturated patches with visible light.", nameof(observations));
            if (!LinearSolver.Solve3(ata, redTarget, out double[] redRow) ||
                !LinearSolver.Solve3(ata, greenTarget, out double[] greenRow) ||
                !LinearSolver.Solve3(ata, blueTarget, out double[] blueRow))
                throw new InvalidOperationException("The calibration patches do not separate red, green and blue.");
            return new AdditiveLightModel(ToFloat(redRow), ToFloat(greenRow), ToFloat(blueRow));
        }

        public bool TryRecover(byte observedRed, byte observedGreen, byte observedBlue,
            float lightRed, float lightGreen, float lightBlue, float alpha,
            out byte red, out byte green, out byte blue)
        {
            red = observedRed;
            green = observedGreen;
            blue = observedBlue;
            if (float.IsNaN(alpha) || alpha <= 0.02f) return false;
            float commandRed = lightRed * alpha;
            float commandGreen = lightGreen * alpha;
            float commandBlue = lightBlue * alpha;
            float recoveredRed = observedRed / 255f - Dot(redRow, commandRed, commandGreen, commandBlue);
            float recoveredGreen = observedGreen / 255f - Dot(greenRow, commandRed, commandGreen, commandBlue);
            float recoveredBlue = observedBlue / 255f - Dot(blueRow, commandRed, commandGreen, commandBlue);
            if (recoveredRed < -0.05f || recoveredGreen < -0.05f || recoveredBlue < -0.05f ||
                recoveredRed > 1.05f || recoveredGreen > 1.05f || recoveredBlue > 1.05f)
                return false;
            red = ToByte(recoveredRed);
            green = ToByte(recoveredGreen);
            blue = ToByte(recoveredBlue);
            return true;
        }

        private static bool Saturated(float value) => value <= 0.02f || value >= 0.98f;

        private static void Accumulate(double[,] ata, double red, double green, double blue)
        {
            double[] light = { red, green, blue };
            for (int row = 0; row < 3; row++)
                for (int column = 0; column < 3; column++)
                    ata[row, column] += light[row] * light[column];
        }

        private static float Dot(float[] row, float red, float green, float blue) =>
            row[0] * red + row[1] * green + row[2] * blue;

        private static float[] ToFloat(double[] values) =>
            new[] { (float)values[0], (float)values[1], (float)values[2] };

        private static byte ToByte(float value)
        {
            if (value < 0f) value = 0f;
            if (value > 1f) value = 1f;
            return (byte)(value * 255f + 0.5f);
        }
    }

    public readonly struct LightObservation
    {
        public float SurfaceRed { get; }
        public float SurfaceGreen { get; }
        public float SurfaceBlue { get; }
        public float LightRed { get; }
        public float LightGreen { get; }
        public float LightBlue { get; }
        public float Alpha { get; }
        public float ObservedRed { get; }
        public float ObservedGreen { get; }
        public float ObservedBlue { get; }

        public LightObservation(float surfaceRed, float surfaceGreen, float surfaceBlue,
            float lightRed, float lightGreen, float lightBlue, float alpha,
            float observedRed, float observedGreen, float observedBlue)
        {
            SurfaceRed = surfaceRed; SurfaceGreen = surfaceGreen; SurfaceBlue = surfaceBlue;
            LightRed = lightRed; LightGreen = lightGreen; LightBlue = lightBlue; Alpha = alpha;
            ObservedRed = observedRed; ObservedGreen = observedGreen; ObservedBlue = observedBlue;
        }
    }

    /// <summary>Maps board coordinates (X right, Y forward) to camera pixels. Fit from the four board corners.</summary>
    public sealed class BoardHomography
    {
        private readonly double[] h;

        private BoardHomography(double[] h) { this.h = h; }

        public static BoardHomography Fit(IReadOnlyList<BoardPixelPair> pairs)
        {
            if (pairs == null || pairs.Count < 4)
                throw new ArgumentException("A board-to-camera map needs four corners.", nameof(pairs));
            var ata = new double[8, 8];
            var atb = new double[8];
            foreach (BoardPixelPair pair in pairs)
            {
                AddRow(ata, atb, pair.BoardX, pair.BoardY, pair.PixelX, pair.PixelY, true);
                AddRow(ata, atb, pair.BoardX, pair.BoardY, pair.PixelX, pair.PixelY, false);
            }
            if (!LinearSolver.Solve8(ata, atb, out double[] coefficients))
                throw new InvalidOperationException("The corner correspondences do not determine a camera map.");
            var values = new double[9];
            for (int i = 0; i < 8; i++) values[i] = coefficients[i];
            values[8] = 1;
            return new BoardHomography(values);
        }

        public bool TryMap(float boardX, float boardY, out float pixelX, out float pixelY)
        {
            double weight = h[6] * boardX + h[7] * boardY + h[8];
            pixelX = 0f;
            pixelY = 0f;
            if (Math.Abs(weight) < 1e-8 || double.IsNaN(weight)) return false;
            pixelX = (float)((h[0] * boardX + h[1] * boardY + h[2]) / weight);
            pixelY = (float)((h[3] * boardX + h[4] * boardY + h[5]) / weight);
            return !float.IsNaN(pixelX) && !float.IsNaN(pixelY);
        }

        private static void AddRow(double[,] ata, double[] atb, double x, double y, double u, double v, bool horizontal)
        {
            double target = horizontal ? u : v;
            var row = new double[8];
            if (horizontal)
            {
                row[0] = x; row[1] = y; row[2] = 1;
                row[6] = -u * x; row[7] = -u * y;
            }
            else
            {
                row[3] = x; row[4] = y; row[5] = 1;
                row[6] = -v * x; row[7] = -v * y;
            }
            for (int r = 0; r < 8; r++)
            {
                atb[r] += row[r] * target;
                for (int c = 0; c < 8; c++) ata[r, c] += row[r] * row[c];
            }
        }
    }

    public readonly struct BoardPixelPair
    {
        public float BoardX { get; }
        public float BoardY { get; }
        public float PixelX { get; }
        public float PixelY { get; }
        public BoardPixelPair(float boardX, float boardY, float pixelX, float pixelY)
        {
            BoardX = boardX; BoardY = boardY; PixelX = pixelX; PixelY = pixelY;
        }
    }

    internal static class LinearSolver
    {
        public static bool Solve3(double[,] ata, double[] atb, out double[] solution)
        {
            var matrix = new double[3, 3];
            var right = new double[3];
            for (int r = 0; r < 3; r++)
            {
                right[r] = atb[r];
                for (int c = 0; c < 3; c++) matrix[r, c] = ata[r, c];
            }
            return Solve(matrix, right, out solution);
        }

        public static bool Solve8(double[,] ata, double[] atb, out double[] solution)
        {
            var matrix = new double[8, 8];
            var right = new double[8];
            for (int r = 0; r < 8; r++)
            {
                right[r] = atb[r];
                for (int c = 0; c < 8; c++) matrix[r, c] = ata[r, c];
            }
            return Solve(matrix, right, out solution);
        }

        private static bool Solve(double[,] matrix, double[] right, out double[] solution)
        {
            int n = right.Length;
            solution = new double[n];
            for (int column = 0; column < n; column++)
            {
                int pivot = column;
                double best = Math.Abs(matrix[column, column]);
                for (int row = column + 1; row < n; row++)
                {
                    double value = Math.Abs(matrix[row, column]);
                    if (value > best) { best = value; pivot = row; }
                }
                if (best < 1e-10) return false;
                if (pivot != column)
                {
                    for (int k = column; k < n; k++)
                    {
                        double swap = matrix[column, k];
                        matrix[column, k] = matrix[pivot, k];
                        matrix[pivot, k] = swap;
                    }
                    double swapRight = right[column];
                    right[column] = right[pivot];
                    right[pivot] = swapRight;
                }
                double scale = matrix[column, column];
                for (int k = column; k < n; k++) matrix[column, k] /= scale;
                right[column] /= scale;
                for (int row = 0; row < n; row++)
                {
                    if (row == column) continue;
                    double factor = matrix[row, column];
                    if (factor == 0) continue;
                    for (int k = column; k < n; k++) matrix[row, k] -= factor * matrix[column, k];
                    right[row] -= factor * right[column];
                }
            }
            for (int i = 0; i < n; i++) solution[i] = right[i];
            return true;
        }
    }
}
