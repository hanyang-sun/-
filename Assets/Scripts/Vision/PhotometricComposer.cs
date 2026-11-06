using System;

namespace Brick2Life.Vision
{
    // Turns a reflectance image into the photograph a locked camera would record under a known projector command.
    // Unity's transparent quads are not that photograph; real cameras enter through ProjectedLightRecognizer directly.
    public static class PhotometricComposer
    {
        public static RgbImage Illuminate(RgbImage albedo, GridSample[][] samples, PhotometricProfile profile, CellProjection projection)
        {
            if (albedo == null) throw new ArgumentNullException(nameof(albedo));
            if (samples == null) throw new ArgumentNullException(nameof(samples));
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            if (samples.Length != projection.CellCount) throw new ArgumentException("Samples and projector commands cover different boards.");
            var image = new RgbImage(albedo.Width, albedo.Height, albedo.CopyPacked());
            for (int cell = 0; cell < samples.Length; cell++)
            {
                projection.At(cell, out double commandRed, out double commandGreen, out double commandBlue);
                GridSample[] cellSamples = samples[cell];
                if (cellSamples == null) continue;
                foreach (GridSample sample in cellSamples)
                {
                    if (!sample.InFront || !albedo.TryRead(sample.PixelX, sample.PixelY, out byte red, out byte green, out byte blue)) continue;
                    LinearRgb predicted = profile.Predict(LinearRgb.Decode(new RgbColor(red, green, blue)), commandRed, commandGreen, commandBlue, out _);
                    RgbColor encoded = predicted.Encode();
                    image.SetPixel((int)Math.Floor(sample.PixelX + 0.5), (int)Math.Floor(sample.PixelY + 0.5), encoded.R, encoded.G, encoded.B);
                }
            }
            return image;
        }
    }
}
