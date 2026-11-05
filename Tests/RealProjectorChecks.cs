using Brick2Life.Application;
using Brick2Life.Core;
using Brick2Life.Vision;

internal static class RealProjectorChecks
{
    private static readonly double[,] Response =
    {
        { 0.22, 0.02, 0.01 },
        { 0.015, 0.20, 0.03 },
        { 0.01, 0.02, 0.24 }
    };

    public static void Run()
    {
        int assertions = 0;
        void Check(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new Exception(message);
        }

        CheckHomography(Check);
        CheckCalibrationRoundTrip(Check);
        CheckRecognition(Check);
        Console.WriteLine("Real projector checks passed (" + assertions + ").");
    }

    private static void CheckHomography(Action<bool, string> check)
    {
        var corners = new List<BoardPixelPair>();
        foreach (var point in new[] { (0f, 0f), (10f, 0f), (10f, 10f), (0f, 10f) })
        {
            Project(point.Item1, point.Item2, out float pixelX, out float pixelY);
            corners.Add(new BoardPixelPair(point.Item1, point.Item2, pixelX, pixelY));
        }
        BoardHomography map = BoardHomography.Fit(corners);
        Project(4.5f, 6.5f, out float expectedX, out float expectedY);
        check(map.TryMap(4.5f, 6.5f, out float actualX, out float actualY), "Board center did not map into the camera.");
        check(Math.Abs(actualX - expectedX) < 0.05f && Math.Abs(actualY - expectedY) < 0.05f,
            "Fitted camera map drifted. Expected " + expectedX.ToString("0.00") + "," + expectedY.ToString("0.00") +
            " got " + actualX.ToString("0.00") + "," + actualY.ToString("0.00"));
    }

    private static void CheckCalibrationRoundTrip(Action<bool, string> check)
    {
        const float table = 168f / 255f;
        var patches = new List<LightObservation>();
        foreach (var light in new[]
        {
            (1f, 0f, 0f, 1f), (0f, 1f, 0f, 1f), (0f, 0f, 1f, 1f),
            (1f, 1f, 1f, 0.8f), (0.2f, 0.9f, 1f, 0.7f), (1f, 0.55f, 0.15f, 0.6f)
        })
        {
            Observe(table, table, table, light.Item1, light.Item2, light.Item3, light.Item4, out float or, out float og, out float ob);
            patches.Add(new LightObservation(table, table, table, light.Item1, light.Item2, light.Item3, light.Item4, or, og, ob));
        }
        AdditiveLightModel model = AdditiveLightModel.Fit(patches);
        Observe(200f / 255f, 48f / 255f, 42f / 255f, 0.2f, 0.9f, 1f, 0.5f, out float seenR, out float seenG, out float seenB);
        check(model.TryRecover(ToByte(seenR), ToByte(seenG), ToByte(seenB), 0.2f, 0.9f, 1f, 0.5f, out byte red, out byte green, out byte blue),
            "Calibrated removal rejected a lit brick.");
        check(Math.Abs(red - 200) <= 1 && Math.Abs(green - 48) <= 1 && Math.Abs(blue - 42) <= 1,
            "Lit brick did not return to its color. Got " + red + "," + green + "," + blue);
    }

    private static void CheckRecognition(Action<bool, string> check)
    {
        var definition = new BoardDefinition(colorIds: new[] { "red", "green", "blue", "yellow" });
        var calibration = new VisionCalibration("real-camera", new RgbColor(168, 168, 168), new[]
        {
            new ColorSwatch("red", new RgbColor(200, 48, 42)),
            new ColorSwatch("green", new RgbColor(46, 168, 52)),
            new ColorSwatch("blue", new RgbColor(42, 74, 198)),
            new ColorSwatch("yellow", new RgbColor(214, 188, 46))
        }, surfaceY: 0, samplesPerAxis: 1);
        var corners = new List<BoardPixelPair>();
        foreach (var point in new[] { (0f, 0f), (10f, 0f), (10f, 10f), (0f, 10f) })
        {
            Project(point.Item1, point.Item2, out float pixelX, out float pixelY);
            corners.Add(new BoardPixelPair(point.Item1, point.Item2, pixelX, pixelY));
        }
        BoardHomography map = BoardHomography.Fit(corners);
        const float table = 168f / 255f;
        var patches = new List<LightObservation>();
        foreach (var light in new[]
        {
            (1f, 0f, 0f, 1f), (0f, 1f, 0f, 1f), (0f, 0f, 1f, 1f), (0.2f, 0.9f, 1f, 0.75f), (1f, 0.55f, 0.15f, 0.55f)
        })
        {
            Observe(table, table, table, light.Item1, light.Item2, light.Item3, light.Item4, out float or, out float og, out float ob);
            patches.Add(new LightObservation(table, table, table, light.Item1, light.Item2, light.Item3, light.Item4, or, og, ob));
        }
        AdditiveLightModel response = AdditiveLightModel.Fit(patches);

        var surfaces = new Dictionary<(int, int), (byte R, byte G, byte B, string Color)>();
        var lights = new Dictionary<(int, int), (float R, float G, float B, float A)>();
        surfaces[(1, 1)] = (200, 48, 42, "red");
        surfaces[(2, 2)] = (46, 168, 52, "green");
        surfaces[(3, 3)] = (42, 74, 198, "blue");
        surfaces[(4, 4)] = (214, 188, 46, "yellow");
        lights[(3, 3)] = (0.2f, 0.9f, 1f, 0.5f);
        lights[(5, 5)] = (0.2f, 0.9f, 1f, 0.75f);
        lights[(6, 6)] = (1f, 0.55f, 0.15f, 0.55f);

        const int width = 360, height = 360;
        var raw = new byte[width * height * 3];
        var cleaned = new byte[width * height * 3];
        for (int y = 0; y < 10; y++)
            for (int x = 0; x < 10; x++)
            {
                byte sr = 168, sg = 168, sb = 168;
                if (surfaces.TryGetValue((x, y), out var surface)) { sr = surface.R; sg = surface.G; sb = surface.B; }
                float lr = 0, lg = 0, lb = 0, la = 0;
                if (lights.TryGetValue((x, y), out var light)) { lr = light.R; lg = light.G; lb = light.B; la = light.A; }
                Observe(sr / 255f, sg / 255f, sb / 255f, lr, lg, lb, la, out float or, out float og, out float ob);
                CameraProjection.TopPoint(definition, x, y, 0, 0, 1, calibration.Inset, 0, out double worldX, out double _, out double worldZ);
                check(map.TryMap((float)worldX, (float)worldZ, out float pixelX, out float pixelY), "Sample fell outside the camera map.");
                int px = (int)Math.Floor(pixelX + 0.5), py = (int)Math.Floor(pixelY + 0.5);
                int index = (py * width + px) * 3;
                raw[index] = cleaned[index] = ToByte(or);
                raw[index + 1] = cleaned[index + 1] = ToByte(og);
                raw[index + 2] = cleaned[index + 2] = ToByte(ob);
                if (la > 0.02f && response.TryRecover(cleaned[index], cleaned[index + 1], cleaned[index + 2], lr, lg, lb, la,
                    out byte rr, out byte rg, out byte rb))
                {
                    cleaned[index] = rr; cleaned[index + 1] = rg; cleaned[index + 2] = rb;
                }
            }

        GridSample[][] samples = CameraProjection.CreateSamples(definition, calibration,
            (double x, double y, double z, out double pixelX, out double pixelY) =>
            {
                bool ok = map.TryMap((float)x, (float)z, out float mappedX, out float mappedY);
                pixelX = mappedX; pixelY = mappedY;
                return ok;
            });
        var seen = new RgbImage(width, height, raw);
        var restored = new RgbImage(width, height, cleaned);
        var when = new DateTimeOffset(2026, 11, 5, 0, 0, 0, TimeSpan.Zero);
        FrameObservation polluted = FixedGridRecognizer.Recognize(definition, seen, samples, calibration, "raw", "real", 0, when);
        FrameObservation recovered = FixedGridRecognizer.Recognize(definition, restored, samples, calibration, "clean", "real", 1, when);
        check(polluted.Accepted && polluted.Cells[definition.Index(5, 5)].State.Occupancy != Occupancy.Empty,
            "A cyan path on an empty cell should fool recognition before the light is removed.");
        check(recovered.Accepted, "Recovered frame was rejected: " + recovered.RejectionReason);
        for (int y = 0; y < 10; y++)
            for (int x = 0; x < 10; x++)
            {
                BoardCell cell = recovered.Cells[definition.Index(x, y)].State;
                if (surfaces.TryGetValue((x, y), out var surface))
                    check(cell.Occupancy == Occupancy.Occupied && cell.ColorId == surface.Color,
                        "Cell " + x + "," + y + " lost its color after removing projector light.");
                else
                    check(cell.Occupancy == Occupancy.Empty, "Empty cell " + x + "," + y + " still looks occupied.");
            }
    }

    private static void Project(float x, float y, out float pixelX, out float pixelY)
    {
        double weight = 0.003 * x + 0.002 * y + 1;
        pixelX = (float)((26 * x + 0.4 * y + 18) / weight);
        pixelY = (float)((0.3 * x + 24 * y + 16) / weight);
    }

    private static void Observe(float surfaceRed, float surfaceGreen, float surfaceBlue,
        float lightRed, float lightGreen, float lightBlue, float alpha,
        out float observedRed, out float observedGreen, out float observedBlue)
    {
        float commandRed = lightRed * alpha, commandGreen = lightGreen * alpha, commandBlue = lightBlue * alpha;
        observedRed = Clamp(surfaceRed + (float)(Response[0, 0] * commandRed + Response[0, 1] * commandGreen + Response[0, 2] * commandBlue));
        observedGreen = Clamp(surfaceGreen + (float)(Response[1, 0] * commandRed + Response[1, 1] * commandGreen + Response[1, 2] * commandBlue));
        observedBlue = Clamp(surfaceBlue + (float)(Response[2, 0] * commandRed + Response[2, 1] * commandGreen + Response[2, 2] * commandBlue));
    }

    private static float Clamp(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
    private static byte ToByte(float value) => (byte)(Clamp(value) * 255f + 0.5f);
}
