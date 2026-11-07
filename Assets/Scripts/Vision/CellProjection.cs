using System;

namespace Brick2Life.Vision
{
    // Known illumination during this exposure. Hardware uses projector commands; C can supply calibrated per-sample
    // total illumination (normalized by its profile gain), with an optional separate ambient reference.
    // BlankingFraction must stay 0 during play: a dark frame inserted for capture is visible flicker.
    public sealed class CellProjection
    {
        private readonly double[] red, green, blue;
        public int CellCount => red.Length;
        private double[][] sampleRed, sampleGreen, sampleBlue;
        private double[][] ambientRed, ambientGreen, ambientBlue;
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

        // Textured projector output varies inside a cell; callers must provide the command for each RGB sample.
        public static CellProjection FromSamples(double[][] red, double[][] green, double[][] blue,
            double[][] ambientRed = null, double[][] ambientGreen = null, double[][] ambientBlue = null)
        {
            if (red == null || green == null || blue == null || red.Length == 0 || green.Length != red.Length || blue.Length != red.Length)
                throw new ArgumentException("Sample commands must cover the same cells.");
            var result = Continuous(new double[red.Length], new double[red.Length], new double[red.Length]);
            result.sampleRed = new double[red.Length][]; result.sampleGreen = new double[red.Length][]; result.sampleBlue = new double[red.Length][];
            for (int cell = 0; cell < red.Length; cell++)
            {
                if (red[cell] == null || green[cell] == null || blue[cell] == null || red[cell].Length != green[cell].Length || red[cell].Length != blue[cell].Length)
                    throw new ArgumentException("Each sample needs an RGB command.");
                result.sampleRed[cell] = Copy(red[cell]); result.sampleGreen[cell] = Copy(green[cell]); result.sampleBlue[cell] = Copy(blue[cell]);
            }
            if (ambientRed != null || ambientGreen != null || ambientBlue != null)
            {
                var ambient = FromSamples(ambientRed, ambientGreen, ambientBlue);
                if (ambient.CellCount != result.CellCount) throw new ArgumentException("Ambient and total lighting must cover the same cells.");
                for (int cell = 0; cell < red.Length; cell++)
                    if (ambient.sampleRed[cell].Length != result.sampleRed[cell].Length) throw new ArgumentException("Ambient sample counts differ.");
                result.ambientRed = ambient.sampleRed; result.ambientGreen = ambient.sampleGreen; result.ambientBlue = ambient.sampleBlue;
            }
            return result;
        }

        public bool MatchesSamples(GridSample[][] samples)
        {
            if (samples == null || samples.Length != CellCount) return false;
            for (int i = 0; i < samples.Length; i++)
                if (sampleRed != null && (samples[i] == null || samples[i].Length != sampleRed[i].Length)) return false;
            return true;
        }

        public void At(int cell, int sample, out double r, out double g, out double b)
        {
            if (sampleRed == null) { At(cell, out r, out g, out b); return; }
            r = sampleRed[cell][sample]; g = sampleGreen[cell][sample]; b = sampleBlue[cell][sample];
        }

        public bool TryAmbientAt(int cell, int sample, out double r, out double g, out double b)
        {
            r = g = b = 0;
            if (ambientRed == null) return false;
            r = ambientRed[cell][sample]; g = ambientGreen[cell][sample]; b = ambientBlue[cell][sample]; return true;
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
