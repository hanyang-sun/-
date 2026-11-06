using System;

namespace Brick2Life.Vision
{
    // The projector command that was actually on screen while the shutter was open.
    // BlankingFraction must stay 0 during play: a dark frame inserted for capture is visible flicker.
    public sealed class CellProjection
    {
        private readonly double[] red, green, blue;
        public int CellCount => red.Length;
        public bool ExposureLocked { get; }
        public bool StableDuringExposure { get; }
        public double BlankingFraction { get; }

        public CellProjection(double[] red, double[] green, double[] blue, bool exposureLocked,
            bool stableDuringExposure, double blankingFraction)
        {
            if (red == null || green == null || blue == null || red.Length == 0 || green.Length != red.Length || blue.Length != red.Length)
                throw new ArgumentException("Each cell needs a finite RGB projector command.");
            if (double.IsNaN(blankingFraction) || double.IsInfinity(blankingFraction) || blankingFraction < 0 || blankingFraction > 1)
                throw new ArgumentOutOfRangeException(nameof(blankingFraction));
            this.red = Copy(red);
            this.green = Copy(green);
            this.blue = Copy(blue);
            ExposureLocked = exposureLocked;
            StableDuringExposure = stableDuringExposure;
            BlankingFraction = blankingFraction;
        }

        // Continuous play: the projector shows the game frame for the whole exposure and never blanks.
        public static CellProjection Continuous(double[] red, double[] green, double[] blue)
        {
            return new CellProjection(red, green, blue, true, true, 0);
        }

        public void At(int index, out double commandRed, out double commandGreen, out double commandBlue)
        {
            if (index < 0 || index >= red.Length) throw new ArgumentOutOfRangeException(nameof(index));
            commandRed = red[index];
            commandGreen = green[index];
            commandBlue = blue[index];
        }

        private static double[] Copy(double[] source)
        {
            var copy = new double[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                double value = source[i];
                if (double.IsNaN(value) || double.IsInfinity(value) || value < 0 || value > 1)
                    throw new ArgumentOutOfRangeException(nameof(source), "Projector commands are in 0..1.");
                copy[i] = value;
            }
            return copy;
        }
    }
}
