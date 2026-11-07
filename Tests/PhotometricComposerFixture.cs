using System;

namespace Brick2Life.Vision
{
    // Turns a reflectance image into the photograph a locked camera would record under a known projector command.
    // Unity's transparent quads are not that photograph; real cameras enter through ProjectedLightRecognizer directly.
    public static class PhotometricComposer
    {
        // Synthetic stand-in used until a real camera runs Fit. The same numbers drive the composer and the recognizer.
        public static PhotometricProfile ForLockedCamera(VisionCalibration calibration)
        {
            if (calibration == null) throw new ArgumentNullException(nameof(calibration));
            var reflectance = new LinearRgb[calibration.Colors.Count];
            for (int i = 0; i < reflectance.Length; i++) reflectance[i] = LinearRgb.Decode(calibration.Colors[i].Value);
            return PhotometricProfile.Create(calibration.CalibrationId, true, new LinearRgb(0, 0, 0), new LinearRgb(0.22, 0.22, 0.22), new LinearRgb(0.62, 0.62, 0.62),
                LinearRgb.Decode(calibration.Background), reflectance);
        }

        public static RgbImage Illuminate(RgbImage albedo, GridSample[][] samples, PhotometricProfile profile, CellProjection projection)
        {
            if (albedo == null) throw new ArgumentNullException(nameof(albedo));
            if (samples == null) throw new ArgumentNullException(nameof(samples));
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            if (samples.Length != projection.CellCount) throw new ArgumentException("Samples and projector commands cover different boards.");
            var packed = new byte[albedo.Width * albedo.Height * 3];
            for (int y = 0; y < albedo.Height; y++) for (int x = 0; x < albedo.Width; x++)
            { albedo.TryRead(x, y, out byte r, out byte g, out byte b); int i = (y * albedo.Width + x) * 3; packed[i] = r; packed[i + 1] = g; packed[i + 2] = b; }
            var image = new RgbImage(albedo.Width, albedo.Height, packed);
            for (int cell = 0; cell < samples.Length; cell++)
            {
                projection.At(cell, out double commandRed, out double commandGreen, out double commandBlue);
                GridSample[] cellSamples = samples[cell];
                if (cellSamples == null) continue;
                foreach (GridSample sample in cellSamples)
                {
                    if (!sample.InFront || !albedo.TryRead(sample.PixelX, sample.PixelY, out byte red, out byte green, out byte blue)) continue;
                    LinearRgb predicted = profile.Predict(profile.Decode(new RgbColor(red, green, blue)), commandRed, commandGreen, commandBlue, out _);
                    RgbColor encoded = profile.Encode(predicted);
                    image.SetPixel((int)Math.Floor(sample.PixelX + 0.5), (int)Math.Floor(sample.PixelY + 0.5), encoded.R, encoded.G, encoded.B);
                }
            }
            return image;
        }
    }
}
