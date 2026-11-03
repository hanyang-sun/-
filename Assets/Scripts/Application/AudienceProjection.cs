using System;
using System.Collections.Generic;

namespace Brick2Life.Application
{
    /// <summary>
    /// Board-space picture a projector should show. Coordinates are cell units: origin at the
    /// minimum corner, X to the right, Y forward. Simulation and a future hardware output both
    /// consume this frame; neither side puts Unity types in it.
    /// </summary>
    public enum AudienceRingKind
    {
        Range = 0,
        Spawn = 1,
        Goal = 2,
        Ripple = 3,
        Impact = 4
    }

    public readonly struct AudiencePoint
    {
        public float X { get; }
        public float Y { get; }
        public AudiencePoint(float x, float y)
        {
            if (!Finite(x) || !Finite(y)) throw new ArgumentOutOfRangeException(nameof(x), "Projection points must be finite.");
            X = x; Y = y;
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    public sealed class AudiencePath
    {
        public AudiencePoint[] Points { get; }
        public float Width { get; }
        public AudiencePath(IReadOnlyList<AudiencePoint> points, float width)
        {
            if (points == null || points.Count < 2) throw new ArgumentException("A path needs two points.", nameof(points));
            if (!FinitePositive(width)) throw new ArgumentOutOfRangeException(nameof(width));
            Points = new AudiencePoint[points.Count];
            for (int i = 0; i < points.Count; i++) Points[i] = points[i];
            Width = width;
        }
        private static bool FinitePositive(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
    }

    public readonly struct AudienceRing
    {
        public float X { get; }
        public float Y { get; }
        public float Radius { get; }
        public AudienceRingKind Kind { get; }
        public bool Alert { get; }
        public float Alpha { get; }
        public AudienceRing(float x, float y, float radius, AudienceRingKind kind, bool alert, float alpha)
        {
            if (!Finite(x) || !Finite(y) || !Finite(radius) || radius <= 0f)
                throw new ArgumentOutOfRangeException(nameof(radius), "Rings need a finite positive radius.");
            X = x; Y = y; Radius = radius; Kind = kind; Alert = alert; Alpha = Clamp01(alpha);
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static float Clamp01(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }
    }

    public readonly struct AudienceDot
    {
        public float X { get; }
        public float Y { get; }
        public float Radius { get; }
        public float Alpha { get; }
        public AudienceDot(float x, float y, float radius, float alpha)
        {
            if (!Finite(x) || !Finite(y) || !Finite(radius) || radius <= 0f)
                throw new ArgumentOutOfRangeException(nameof(radius));
            X = x; Y = y; Radius = radius; Alpha = Clamp01(alpha);
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }
    }

    public readonly struct AudienceBeam
    {
        public float X0 { get; }
        public float Y0 { get; }
        public float X1 { get; }
        public float Y1 { get; }
        public float Alpha { get; }
        public bool Aim { get; }
        public AudienceBeam(float x0, float y0, float x1, float y1, float alpha, bool aim)
        {
            if (!Finite(x0) || !Finite(y0) || !Finite(x1) || !Finite(y1))
                throw new ArgumentOutOfRangeException(nameof(x0), "Beams need finite endpoints.");
            X0 = x0; Y0 = y0; X1 = x1; Y1 = y1; Aim = aim; Alpha = Clamp01(alpha);
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }
    }

    public sealed class AudienceProjectionFrame
    {
        public List<AudiencePath> Paths { get; } = new List<AudiencePath>();
        public List<AudienceRing> Rings { get; } = new List<AudienceRing>();
        public List<AudienceDot> Dots { get; } = new List<AudienceDot>();
        public List<AudienceBeam> Beams { get; } = new List<AudienceBeam>();
        public float Flow { get; set; }

        public void Clear()
        {
            Paths.Clear();
            Rings.Clear();
            Dots.Clear();
            Beams.Clear();
            Flow = 0f;
        }
    }

    /// <summary>Paints an audience frame into a straight-alpha RGBA buffer, row 0 at board Y = 0.</summary>
    public static class AudienceProjectionRaster
    {
        public const int DefaultResolution = 512;

        public static byte[] Draw(AudienceProjectionFrame frame, float boardWidth, float boardHeight, int resolution, byte[] reuse = null)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));
            if (!(boardWidth > 0f) || !(boardHeight > 0f) || resolution < 8 || resolution > 2048)
                throw new ArgumentOutOfRangeException(nameof(resolution));
            int bytes = resolution * resolution * 4;
            byte[] pixels = reuse != null && reuse.Length == bytes ? reuse : new byte[bytes];
            if (ReferenceEquals(pixels, reuse)) Array.Clear(pixels, 0, pixels.Length);
            foreach (AudiencePath path in frame.Paths)
                DrawPath(pixels, resolution, boardWidth, boardHeight, path, frame.Flow);
            foreach (AudienceRing ring in frame.Rings)
                DrawRing(pixels, resolution, boardWidth, boardHeight, ring);
            foreach (AudienceBeam beam in frame.Beams)
                DrawBeam(pixels, resolution, boardWidth, boardHeight, beam);
            foreach (AudienceDot dot in frame.Dots)
                DrawDot(pixels, resolution, boardWidth, boardHeight, dot);
            return pixels;
        }

        private static void DrawPath(byte[] pixels, int resolution, float boardWidth, float boardHeight, AudiencePath path, float flow)
        {
            float along = 0f;
            for (int i = 0; i < path.Points.Length - 1; i++)
            {
                AudiencePoint from = path.Points[i];
                AudiencePoint to = path.Points[i + 1];
                float length = Distance(from.X, from.Y, to.X, to.Y);
                Stroke(pixels, resolution, boardWidth, boardHeight, from.X, from.Y, to.X, to.Y,
                    path.Width * 0.5f, along, flow, 40, 220, 255, 0.9f, true);
                along += length;
            }
        }

        private static void DrawRing(byte[] pixels, int resolution, float boardWidth, float boardHeight, AudienceRing ring)
        {
            bool dashed = ring.Kind == AudienceRingKind.Range;
            bool alert = ring.Alert || ring.Kind == AudienceRingKind.Impact;
            byte red = alert ? (byte)255 : (byte)50;
            byte green = alert ? (byte)(ring.Kind == AudienceRingKind.Impact ? 70 : 140) : (byte)230;
            byte blue = alert ? (byte)(ring.Kind == AudienceRingKind.Impact ? 80 : 40) : (byte)255;
            float stroke = dashed ? Math.Max(0.07f, ring.Radius * 0.035f) : Math.Max(0.09f, ring.Radius * 0.08f);
            int minX = Pixel(ring.X - ring.Radius - stroke, boardWidth, resolution) - 1;
            int maxX = Pixel(ring.X + ring.Radius + stroke, boardWidth, resolution) + 1;
            int minY = Pixel(ring.Y - ring.Radius - stroke, boardHeight, resolution) - 1;
            int maxY = Pixel(ring.Y + ring.Radius + stroke, boardHeight, resolution) + 1;
            minX = Clamp(minX, 0, resolution - 1);
            maxX = Clamp(maxX, 0, resolution - 1);
            minY = Clamp(minY, 0, resolution - 1);
            maxY = Clamp(maxY, 0, resolution - 1);
            for (int py = minY; py <= maxY; py++)
                for (int px = minX; px <= maxX; px++)
                {
                    float bx = Board(px, boardWidth, resolution);
                    float by = Board(py, boardHeight, resolution);
                    float dx = bx - ring.X, dy = by - ring.Y;
                    float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                    float band = Math.Abs(dist - ring.Radius);
                    if (band > stroke) continue;
                    if (dashed)
                    {
                        float turns = ((float)Math.Atan2(dy, dx) + (float)Math.PI) / (2f * (float)Math.PI);
                        if (Frac(turns * 28f) > 0.55f) continue;
                    }
                    float alpha = (1f - band / stroke) * ring.Alpha;
                    Blend(pixels, (py * resolution + px) * 4, red, green, blue, alpha);
                }
        }

        private static void DrawDot(byte[] pixels, int resolution, float boardWidth, float boardHeight, AudienceDot dot)
        {
            int minX = Clamp(Pixel(dot.X - dot.Radius, boardWidth, resolution) - 1, 0, resolution - 1);
            int maxX = Clamp(Pixel(dot.X + dot.Radius, boardWidth, resolution) + 1, 0, resolution - 1);
            int minY = Clamp(Pixel(dot.Y - dot.Radius, boardHeight, resolution) - 1, 0, resolution - 1);
            int maxY = Clamp(Pixel(dot.Y + dot.Radius, boardHeight, resolution) + 1, 0, resolution - 1);
            for (int py = minY; py <= maxY; py++)
                for (int px = minX; px <= maxX; px++)
                {
                    float dx = Board(px, boardWidth, resolution) - dot.X;
                    float dy = Board(py, boardHeight, resolution) - dot.Y;
                    float dist = (float)Math.Sqrt(dx * dx + dy * dy) / dot.Radius;
                    if (dist > 1f) continue;
                    float alpha = (1f - dist) * (1f - dist) * dot.Alpha;
                    Blend(pixels, (py * resolution + px) * 4, 255, 150, 30, alpha);
                }
        }

        private static void DrawBeam(byte[] pixels, int resolution, float boardWidth, float boardHeight, AudienceBeam beam)
        {
            Stroke(pixels, resolution, boardWidth, boardHeight, beam.X0, beam.Y0, beam.X1, beam.Y1,
                0.06f, 0f, 0f, 255, 170, 50, beam.Alpha, false);
        }

        private static void Stroke(byte[] pixels, int resolution, float boardWidth, float boardHeight,
            float x0, float y0, float x1, float y1, float halfWidth, float along0, float flow,
            byte red, byte green, byte blue, float alphaScale, bool flowing)
        {
            float dx = x1 - x0, dy = y1 - y0;
            float len2 = dx * dx + dy * dy;
            float len = (float)Math.Sqrt(len2);
            if (len < 1e-4f || halfWidth <= 0f) return;
            int minX = Clamp(Pixel(Math.Min(x0, x1) - halfWidth, boardWidth, resolution) - 1, 0, resolution - 1);
            int maxX = Clamp(Pixel(Math.Max(x0, x1) + halfWidth, boardWidth, resolution) + 1, 0, resolution - 1);
            int minY = Clamp(Pixel(Math.Min(y0, y1) - halfWidth, boardHeight, resolution) - 1, 0, resolution - 1);
            int maxY = Clamp(Pixel(Math.Max(y0, y1) + halfWidth, boardHeight, resolution) + 1, 0, resolution - 1);
            for (int py = minY; py <= maxY; py++)
                for (int px = minX; px <= maxX; px++)
                {
                    float bx = Board(px, boardWidth, resolution);
                    float by = Board(py, boardHeight, resolution);
                    float t = ((bx - x0) * dx + (by - y0) * dy) / len2;
                    if (t < 0f) t = 0f; else if (t > 1f) t = 1f;
                    float cx = x0 + dx * t, cy = y0 + dy * t;
                    float dist = Distance(bx, by, cx, cy);
                    if (dist > halfWidth) continue;
                    float edge = dist / halfWidth;
                    float alpha = (1f - edge) * (1f - edge) * alphaScale;
                    if (flowing)
                    {
                        float stripe = Frac((along0 + len * t) * 1.4f - flow);
                        if (stripe > 0.62f && stripe < 0.88f) alpha = Math.Min(1f, alpha + 0.45f);
                    }
                    Blend(pixels, (py * resolution + px) * 4, red, green, blue, alpha);
                }
        }

        private static void Blend(byte[] pixels, int index, byte red, byte green, byte blue, float alpha)
        {
            if (alpha <= 0f) return;
            if (alpha > 1f) alpha = 1f;
            float destA = pixels[index + 3] / 255f;
            float outA = alpha + destA * (1f - alpha);
            if (outA <= 1e-4f) return;
            pixels[index] = Channel((red / 255f * alpha + pixels[index] / 255f * destA * (1f - alpha)) / outA);
            pixels[index + 1] = Channel((green / 255f * alpha + pixels[index + 1] / 255f * destA * (1f - alpha)) / outA);
            pixels[index + 2] = Channel((blue / 255f * alpha + pixels[index + 2] / 255f * destA * (1f - alpha)) / outA);
            pixels[index + 3] = Channel(outA);
        }

        private static byte Channel(float value)
        {
            int rounded = (int)(value * 255f + 0.5f);
            if (rounded < 0) return 0;
            if (rounded > 255) return 255;
            return (byte)rounded;
        }

        private static float Board(int pixel, float size, int resolution) => (pixel + 0.5f) / resolution * size;
        private static int Pixel(float board, float size, int resolution) => (int)(board / size * resolution);
        private static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;
        private static float Distance(float x0, float y0, float x1, float y1)
        {
            float dx = x0 - x1, dy = y0 - y1;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
        private static float Frac(float value)
        {
            float fraction = value - (float)Math.Floor(value);
            return fraction < 0f ? fraction + 1f : fraction;
        }
    }
}
