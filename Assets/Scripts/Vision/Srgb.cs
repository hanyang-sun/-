using System;

namespace Brick2Life.Vision
{
    // Gamma 2.2 at the photo boundary. Real cameras should hand over linear raw when they have it;
    // sRGB bytes remain the interchange with the existing capture buffer.
    public static class Srgb
    {
        public static double Decode(byte value)
        {
            return Math.Pow(value / 255.0, 2.2);
        }

        public static byte Encode(double linear)
        {
            if (double.IsNaN(linear) || double.IsInfinity(linear)) throw new ArgumentOutOfRangeException(nameof(linear));
            if (linear <= 0) return 0;
            if (linear >= 1) return 255;
            int encoded = (int)Math.Round(Math.Pow(linear, 1.0 / 2.2) * 255.0);
            if (encoded < 0) return 0;
            if (encoded > 255) return 255;
            return (byte)encoded;
        }
    }

    public readonly struct LinearRgb
    {
        public double R { get; }
        public double G { get; }
        public double B { get; }

        public LinearRgb(double r, double g, double b)
        {
            // Reflectance and camera values stay in 0..1. Projector gain may be brighter than that scale, up to 8.
            if (!Finite(r) || !Finite(g) || !Finite(b)) throw new ArgumentOutOfRangeException(nameof(r));
            R = r; G = g; B = b;
        }

        public static LinearRgb Decode(RgbColor color) => new LinearRgb(Srgb.Decode(color.R), Srgb.Decode(color.G), Srgb.Decode(color.B));

        public RgbColor Encode() => new RgbColor(Srgb.Encode(R), Srgb.Encode(G), Srgb.Encode(B));

        private static bool Finite(double value) =>
            !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0 && value <= 8;
    }
}
