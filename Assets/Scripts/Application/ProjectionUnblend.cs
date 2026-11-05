using System;

namespace Brick2Life.Application
{
    /// <summary>
    /// Inverse of the audience sheet blend: observed = light * alpha + surface * (1 - alpha).
    /// A result outside 0..1 means this pixel is not that blend (the light is hidden by a brick),
    /// so the caller keeps the camera color.
    /// </summary>
    public static class ProjectionUnblend
    {
        public static bool TryRecover(byte observedRed, byte observedGreen, byte observedBlue,
            float lightRed, float lightGreen, float lightBlue, float alpha,
            out byte red, out byte green, out byte blue)
        {
            red = observedRed;
            green = observedGreen;
            blue = observedBlue;
            if (float.IsNaN(alpha) || alpha <= 0.02f) return false;
            if (alpha > 0.97f) alpha = 0.97f;
            float inverse = 1f / (1f - alpha);
            float recoveredRed = (observedRed / 255f - lightRed * alpha) * inverse;
            float recoveredGreen = (observedGreen / 255f - lightGreen * alpha) * inverse;
            float recoveredBlue = (observedBlue / 255f - lightBlue * alpha) * inverse;
            if (recoveredRed < -0.05f || recoveredGreen < -0.05f || recoveredBlue < -0.05f ||
                recoveredRed > 1.05f || recoveredGreen > 1.05f || recoveredBlue > 1.05f)
                return false;
            red = Channel(recoveredRed);
            green = Channel(recoveredGreen);
            blue = Channel(recoveredBlue);
            return true;
        }

        public static void Blend(byte surfaceRed, byte surfaceGreen, byte surfaceBlue,
            float lightRed, float lightGreen, float lightBlue, float alpha,
            out byte red, out byte green, out byte blue)
        {
            if (float.IsNaN(alpha) || alpha < 0f) alpha = 0f;
            if (alpha > 1f) alpha = 1f;
            red = Channel(lightRed * alpha + surfaceRed / 255f * (1f - alpha));
            green = Channel(lightGreen * alpha + surfaceGreen / 255f * (1f - alpha));
            blue = Channel(lightBlue * alpha + surfaceBlue / 255f * (1f - alpha));
        }

        private static byte Channel(float value)
        {
            if (value < 0f) value = 0f;
            if (value > 1f) value = 1f;
            return (byte)(value * 255f + 0.5f);
        }
    }
}
