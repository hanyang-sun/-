using Brick2Life.Application;

internal static class AudienceProjectionChecks
{
    public static void Run()
    {
        int assertions = 0;
        void Check(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new Exception(message + " (" + assertions + " assertions reached)");
        }

        var empty = new AudienceProjectionFrame();
        byte[] blank = AudienceProjectionRaster.Draw(empty, 10f, 10f, 32);
        Check(blank.Length == 32 * 32 * 4, "Raster size must cover every pixel.");
        bool any = false;
        for (int i = 3; i < blank.Length; i += 4) if (blank[i] != 0) any = true;
        Check(!any, "An empty frame must stay transparent.");

        var frame = new AudienceProjectionFrame();
        frame.Dots.Add(new AudienceDot(5f, 5f, 0.4f, 1f));
        frame.Paths.Add(new AudiencePath(new[] { new AudiencePoint(1f, 2f), new AudiencePoint(9f, 2f) }, 0.5f));
        frame.Rings.Add(new AudienceRing(5f, 8f, 1.2f, AudienceRingKind.Range, false, 1f));
        frame.Beams.Add(new AudienceBeam(1f, 6f, 4f, 6f, 1f, true));
        const int resolution = 128;
        byte[] pixels = AudienceProjectionRaster.Draw(frame, 10f, 10f, resolution);
        int center = Index(5f, 5f, resolution);
        Check(pixels[center] > 200 && pixels[center + 1] > 80 && pixels[center + 2] < 80 && pixels[center + 3] > 180,
            "Enemy dot should be an orange glow at its board center.");
        int corner = Index(0.2f, 0.2f, resolution);
        Check(pixels[corner + 3] == 0, "The enemy glow must not flood the board corner.");

        int path = Index(5f, 2f, resolution);
        Check(pixels[path + 1] > 140 && pixels[path + 2] > 140 && pixels[path] < 120 && pixels[path + 3] > 80,
            "The route should read as a cyan band.");
        int offPath = Index(5f, 3.2f, resolution);
        Check(pixels[offPath + 3] < 20, "The route band should stay narrow.");

        int lit = 0, gap = 0;
        for (int i = 0; i < 64; i++)
        {
            double angle = i / 64.0 * Math.PI * 2.0;
            float x = 5f + (float)Math.Cos(angle) * 1.2f;
            float y = 8f + (float)Math.Sin(angle) * 1.2f;
            int sample = Index(x, y, resolution);
            if (pixels[sample + 3] > 40) lit++; else gap++;
        }
        Check(lit > 8 && gap > 8, "Range rings should be dashed, with both marks and gaps.");

        bool beamLit = false;
        for (int dy = -1; dy <= 1 && !beamLit; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                int beam = Index(2.5f + dx * 0.08f, 6f + dy * 0.08f, resolution);
                if (pixels[beam] > 180 && pixels[beam + 1] > 80 && pixels[beam + 3] > 40) beamLit = true;
            }
        Check(beamLit, "Aim beams should be orange strokes.");

        frame.Flow = 0.35f;
        byte[] shifted = AudienceProjectionRaster.Draw(frame, 10f, 10f, resolution);
        bool moved = false;
        for (int i = 0; i < pixels.Length; i++) if (pixels[i] != shifted[i]) { moved = true; break; }
        Check(moved, "Path flow should move the bright stripe along the route.");

        byte[] reused = AudienceProjectionRaster.Draw(empty, 10f, 10f, resolution, shifted);
        Check(ReferenceEquals(reused, shifted), "Raster should reuse the supplied buffer.");
        bool cleared = true;
        for (int i = 3; i < reused.Length; i += 4) if (reused[i] != 0) cleared = false;
        Check(cleared, "Reused buffer must be cleared before an empty frame.");
        Console.WriteLine("Audience projection checks passed (" + assertions + ").");
    }

    private static int Index(float x, float y, int resolution)
    {
        int px = (int)(x / 10f * resolution);
        int py = (int)(y / 10f * resolution);
        if (px < 0) px = 0; else if (px >= resolution) px = resolution - 1;
        if (py < 0) py = 0; else if (py >= resolution) py = resolution - 1;
        return (py * resolution + px) * 4;
    }
}
