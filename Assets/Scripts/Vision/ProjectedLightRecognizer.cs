using System;
using Brick2Life.Core;

namespace Brick2Life.Vision
{
    // Classifies a real photograph taken while the projector stays on.
    // The projector command for that exposure is required; the recognizer never asks the projector to go dark.
    public static class ProjectedLightRecognizer
    {
        private const double MinimumBrightness = 0.045;
        private const double EmptySaturation = 0.16;
        private const double MaximumColorDistance = 0.35;
        private const double MinimumColorMargin = 0.12;
        private const double DecisionFraction = 0.58;

        public static FrameObservation Recognize(BoardDefinition definition, RgbImage image, GridSample[][] samples,
            VisionCalibration calibration, PhotometricProfile profile, CellProjection projection, string frameId,
            string sessionId, long sequence, DateTimeOffset capturedAtUtc)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (image == null) throw new ArgumentNullException(nameof(image));
            if (calibration == null) throw new ArgumentNullException(nameof(calibration));
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            if (string.IsNullOrWhiteSpace(frameId) || string.IsNullOrWhiteSpace(sessionId) || sequence < 0)
                throw new ArgumentException("Frame identity is required.");
            if (image.Width < 32 || image.Height < 32 || image.Width > 4096 || image.Height > 4096)
                return Reject(definition, calibration, frameId, sessionId, sequence, capturedAtUtc, "图像尺寸无效。");
            if (samples == null || samples.Length != definition.CellCount || projection.CellCount != definition.CellCount)
                return Reject(definition, calibration, frameId, sessionId, sequence, capturedAtUtc, "采样与棋盘不一致。");
            if (!profile.ExposureLocked || !projection.ExposureLocked)
                return Reject(definition, calibration, frameId, sessionId, sequence, capturedAtUtc, "相机曝光未锁定，光度标定不能用于这一帧。");
            if (!projection.StableDuringExposure)
                return Reject(definition, calibration, frameId, sessionId, sequence, capturedAtUtc, "曝光期间投影内容发生了变化。");
            if (projection.BlankingFraction > 1e-6)
                return Reject(definition, calibration, frameId, sessionId, sequence, capturedAtUtc, "识别不会插入消隐帧，否则投影会频闪。");
            if (profile.ColorReflectance.Count != calibration.Colors.Count)
                return Reject(definition, calibration, frameId, sessionId, sequence, capturedAtUtc, "标定颜色数量与反射率不一致。");
            foreach (ColorSwatch swatch in calibration.Colors)
                if (!definition.AllowsColor(swatch.Id))
                    return Reject(definition, calibration, frameId, sessionId, sequence, capturedAtUtc, "标定颜色不在棋盘定义中。");

            var palette = new Swatch[calibration.Colors.Count];
            for (int i = 0; i < palette.Length; i++)
                palette[i] = Swatch.FromColor(calibration.Colors[i].Id, profile.ColorReflectance[i].Encode());

            var cells = new ObservedCell[definition.CellCount];
            int unseen = 0;
            for (int index = 0; index < cells.Length; index++)
            {
                projection.At(index, out double commandRed, out double commandGreen, out double commandBlue);
                cells[index] = ClassifyCell(image, samples[index], palette, profile, commandRed, commandGreen, commandBlue, out bool sawBoard);
                if (!sawBoard) unseen++;
            }
            if (unseen > definition.CellCount / 2)
                return Reject(definition, calibration, frameId, sessionId, sequence, capturedAtUtc, "棋盘大部分不在画面内。");
            return new FrameObservation(definition, frameId, sessionId, sequence, capturedAtUtc, calibration.CalibrationId, cells);
        }

        private static ObservedCell ClassifyCell(RgbImage image, GridSample[] samples, Swatch[] palette, PhotometricProfile profile,
            double commandRed, double commandGreen, double commandBlue, out bool sawBoard)
        {
            sawBoard = false;
            if (samples == null || samples.Length == 0)
                return new ObservedCell(BoardCell.Unknown, 0, "没有顶面采样。");
            int inside = 0, empty = 0, unknown = 0;
            var votes = new int[palette.Length];
            foreach (GridSample sample in samples)
            {
                if (!sample.InFront || !image.TryRead(sample.PixelX, sample.PixelY, out byte red, out byte green, out byte blue)) continue;
                inside++;
                int color = ClassifyRecovered(red, green, blue, palette, profile, commandRed, commandGreen, commandBlue);
                if (color >= 0) votes[color]++;
                else if (color == -1) empty++;
                else unknown++;
            }
            sawBoard = inside > 0;
            if (inside < samples.Length * 0.6)
                return new ObservedCell(BoardCell.Unknown, inside == 0 ? 0 : (double)inside / samples.Length, "顶面采样超出画面。");
            int needed = (int)Math.Ceiling(inside * DecisionFraction);
            int best = 0;
            for (int i = 1; i < votes.Length; i++) if (votes[i] > votes[best]) best = i;
            int colorVotes = votes.Length == 0 ? 0 : votes[best];
            if (colorVotes >= needed && colorVotes > empty && colorVotes > unknown)
                return new ObservedCell(new BoardCell(Occupancy.Occupied, palette[best].Id), (double)colorVotes / samples.Length);
            if (empty >= needed && empty > colorVotes && empty > unknown)
                return new ObservedCell(BoardCell.Empty, (double)empty / samples.Length);
            return new ObservedCell(BoardCell.Unknown, (double)Math.Max(colorVotes, empty) / samples.Length, "格内颜色不稳定。");
        }

        // -1 empty, -2 unknown, otherwise palette index. Saturated channels stay unknown.
        private static int ClassifyRecovered(byte red, byte green, byte blue, Swatch[] palette, PhotometricProfile profile,
            double commandRed, double commandGreen, double commandBlue)
        {
            if (!profile.TryRecover(LinearRgb.Decode(new RgbColor(red, green, blue)), commandRed, commandGreen, commandBlue, out LinearRgb reflectance))
                return -2;
            RgbColor encoded = reflectance.Encode();
            return ClassifyPixel(encoded.R, encoded.G, encoded.B, palette);
        }

        private static int ClassifyPixel(byte r, byte g, byte b, Swatch[] palette)
        {
            double red = r / 255.0, green = g / 255.0, blue = b / 255.0;
            double brightness = Math.Max(red, Math.Max(green, blue));
            double min = Math.Min(red, Math.Min(green, blue));
            if (brightness < MinimumBrightness) return -2;
            if (brightness <= 0 || (brightness - min) / brightness < EmptySaturation) return -1;
            double nr = red / brightness, ng = green / brightness, nb = blue / brightness;
            int best = -1, second = -1;
            double bestDistance = double.MaxValue, secondDistance = double.MaxValue;
            for (int i = 0; i < palette.Length; i++)
            {
                double distance = palette[i].Distance(nr, ng, nb);
                if (distance < bestDistance) { secondDistance = bestDistance; second = best; bestDistance = distance; best = i; }
                else if (distance < secondDistance) { secondDistance = distance; second = i; }
            }
            if (best < 0 || bestDistance > MaximumColorDistance) return -2;
            if (second >= 0 && secondDistance - bestDistance < MinimumColorMargin) return -2;
            return best;
        }

        private static FrameObservation Reject(BoardDefinition definition, VisionCalibration calibration, string frameId,
            string sessionId, long sequence, DateTimeOffset capturedAtUtc, string reason)
        {
            return new FrameObservation(definition, frameId, sessionId, sequence, capturedAtUtc, calibration.CalibrationId, null, false, reason);
        }

        private sealed class Swatch
        {
            public readonly string Id;
            private readonly double red, green, blue;
            private Swatch(string id, double red, double green, double blue)
            {
                Id = id; this.red = red; this.green = green; this.blue = blue;
            }
            public static Swatch FromColor(string id, RgbColor color)
            {
                double max = Math.Max(color.R, Math.Max(color.G, color.B)) / 255.0;
                if (max <= 0) return new Swatch(id, 0, 0, 0);
                return new Swatch(id, color.R / 255.0 / max, color.G / 255.0 / max, color.B / 255.0 / max);
            }
            public double Distance(double r, double g, double b)
            {
                double dr = r - red, dg = g - green, db = b - blue;
                return Math.Sqrt(dr * dr + dg * dg + db * db);
            }
        }
    }
}
