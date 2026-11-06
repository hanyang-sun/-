using System;
using System.Linq;
using Brick2Life.Core;
using Brick2Life.Vision;

internal static class ProjectedLightChecks
{
    private static readonly DateTimeOffset When = new DateTimeOffset(2026, 11, 6, 3, 0, 0, TimeSpan.Zero);

    public static void Run()
    {
        int assertions = 0;
        void Check(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new Exception(message);
        }
        CheckContinuousContract(Check);
        CheckRecognitionUnderLight(Check);
        CheckDimRoomDoesNotPaintTheTable(Check);
        CheckSaturationStaysUnknown(Check);
        CheckCalibrationRoundTrip(Check);
        CheckRejectedFrames(Check);
        CheckSampleCommandsAndResponse(Check);
        CheckAmbientShadow(Check);
        Console.WriteLine($"PROJECTED_LIGHT_CHECKS_PASSED: {assertions} assertions.");
    }

    private static void CheckContinuousContract(Action<bool, string> check)
    {
        var light = CellProjection.Continuous(Flat(0.2), Flat(0.1), Flat(0.4));
        check(light.BlankingFraction == 0 && light.ExposureLocked && light.StableDuringExposure,
            "Continuous projection still inserts a dark frame or an unlocked exposure.");
        bool threw = false;
        try { PhotometricProfile.Create("unlocked", false, new LinearRgb(0, 0, 0), new LinearRgb(0.2, 0.2, 0.2), new LinearRgb(0.2, 0.2, 0.2), new LinearRgb(0.5, 0.5, 0.5), new[] { new LinearRgb(1, 0, 0) }); }
        catch (InvalidOperationException) { threw = true; }
        check(threw, "An unlocked camera was accepted as a photometric calibration.");
    }

    private static void CheckAmbientShadow(Action<bool, string> check)
    {
        BoardDefinition definition = Board();
        VisionCalibration calibration = Palette(new RgbColor(255, 255, 255));
        LinearRgb Direct(RgbColor c) => new LinearRgb(c.R / 255.0, c.G / 255.0, c.B / 255.0);
        var profile = PhotometricProfile.Create(calibration.CalibrationId, true, new LinearRgb(0, 0, 0), new LinearRgb(0, 0, 0),
            new LinearRgb(8, 8, 8), new LinearRgb(1, 1, 1), calibration.Colors.Select(c => Direct(c.Value)).ToArray(), 1);
        double[][] Constant(double value) => Enumerable.Range(0, 100).Select(_ => new[] { value / 8 }).ToArray();
        var commands = CellProjection.FromSamples(Constant(0.55), Constant(0.42), Constant(0.5), Constant(0.4), Constant(0.4), Constant(0.4));
        var image = new RgbImage(32, 32, new byte[32 * 32 * 3]);
        var samples = Samples(definition);
        for (int cell = 0; cell < 100; cell++)
        {
            LinearRgb rho = cell == 0 ? Direct(calibration.Colors[0].Value) : new LinearRgb(1, 1, 1);
            RgbColor photo = profile.Encode(new LinearRgb(rho.R * 0.29, rho.G * 0.16, rho.B * 0.24));
            image.SetPixel(cell % 10, cell / 10, photo.R, photo.G, photo.B);
        }
        var observation = ProjectedLightRecognizer.Recognize(definition, image, samples, calibration, profile, commands, "shadow", "session", 1, When);
        check(observation.Cells.Skip(1).All(c => c.State.Occupancy == Occupancy.Empty), "An ambient shadow recolored the projected neutral table.");
        check(observation.Cells[0].State.ColorId == "red", "The table-shadow hypothesis erased a colored brick.");
        image.SetPixel(0, 0, 255, 255, 255);
        var clipped = ProjectedLightRecognizer.Recognize(definition, image, samples, calibration, profile, commands, "clip", "session", 2, When);
        check(clipped.Cells[0].State.Occupancy == Occupancy.Unknown, "Shadow compensation guessed a saturated pixel.");
    }

    private static void CheckSampleCommandsAndResponse(Action<bool, string> check)
    {
        BoardDefinition definition = Board();
        VisionCalibration calibration = Palette(new RgbColor(180, 180, 180));
        LinearRgb Direct(RgbColor c) => new LinearRgb(c.R / 255.0, c.G / 255.0, c.B / 255.0);
        var profile = PhotometricProfile.Create(calibration.CalibrationId, true, new LinearRgb(0, 0, 0),
            new LinearRgb(0.25, 0.25, 0.25), new LinearRgb(0.6, 0.6, 0.6), Direct(calibration.Background),
            calibration.Colors.Select(c => Direct(c.Value)).ToArray(), 1);
        var samples = new GridSample[100][];
        var red = new double[100][]; var green = new double[100][]; var blue = new double[100][];
        var image = new RgbImage(32, 32, new byte[32 * 32 * 3]);
        for (int cell = 0; cell < 100; cell++)
        {
            samples[cell] = new GridSample[3]; red[cell] = new double[3]; green[cell] = new double[3]; blue[cell] = new double[3];
            for (int sample = 0; sample < 3; sample++)
            {
                int x = cell % 10 * 3 + sample, y = cell / 10 * 3;
                samples[cell][sample] = new GridSample(cell % 10, cell / 10, x, y, true);
                blue[cell][sample] = sample == 0 ? 0.8 : 0.9;
                red[cell][sample] = sample == 2 ? 0.3 : 0;
                LinearRgb rho = Direct(cell == 0 ? calibration.Colors[0].Value : calibration.Background);
                RgbColor photo = profile.Encode(profile.Predict(rho, red[cell][sample], 0, blue[cell][sample], out _));
                image.SetPixel(x, y, photo.R, photo.G, photo.B);
            }
        }
        var command = CellProjection.FromSamples(red, green, blue);
        var observed = ProjectedLightRecognizer.Recognize(definition, image, samples, calibration, profile, command, "samples", "session", 1, When);
        check(observed.Accepted && observed.Cells[0].State.ColorId == "red" && observed.Cells.Skip(1).All(c => c.State.Occupancy == Occupancy.Empty),
            "Per-sample commands or the Gamma-renderer response adapter mislabeled the photograph.");
        var wrong = ProjectedLightRecognizer.Recognize(definition, image, samples, calibration, profile,
            CellProjection.Continuous(Flat(0), Flat(0), Flat(0)), "wrong", "session", 2, When);
        check(wrong.Cells.Any(c => c.State.Occupancy != Occupancy.Empty && c != wrong.Cells[0]) || wrong.Cells[0].State.ColorId != "red",
            "Recognition ignored the projector command.");
        // Source arrays are caller-owned; changing them after capture cannot mutate the command.
        blue[0][0] = 0;
        command.At(0, 0, out _, out _, out double ownedBlue);
        check(ownedBlue == 0.8, "A caller mutated an already captured projector command.");
        var tracker = new LayoutChangeTracker(3);
        tracker.Observe(observed); tracker.Observe(observed); tracker.DiscardCandidate();
        check(!tracker.Observe(observed).Confirmable && !tracker.Observe(observed).Confirmable,
            "A sampling-mode switch mixed old and new stability counts.");
        check(tracker.Observe(observed).Confirmable, "Fresh independent observations cannot establish a baseline after switching modes.");
        tracker.DiscardCandidate();
        check(tracker.HasBaseline && !tracker.Observe(observed).Confirmable, "Discarding a candidate destroyed the last confirmed layout.");
    }

    private static void CheckRecognitionUnderLight(Action<bool, string> check)
    {
        BoardDefinition definition = Board();
        VisionCalibration calibration = Palette(new RgbColor(180, 180, 180));
        PhotometricProfile profile = PhotometricComposer.ForLockedCamera(calibration);
        (int x, int y, string color, double pr, double pg, double pb)[] layout =
        {
            (0, 0, "red", 0.05, 0.10, 0.90),
            (1, 0, "green", 0.90, 0.10, 0.05),
            (2, 0, "blue", 0.10, 0.85, 0.10),
            (3, 0, "yellow", 0.80, 0.20, 0.10),
            (4, 0, null, 0.26, 0.54, 0.75)
        };
        RgbImage photo = Photograph(definition, calibration, profile, layout, out CellProjection projection, out GridSample[][] samples);
        FrameObservation recognized = ProjectedLightRecognizer.Recognize(definition, photo, samples, calibration, profile, projection,
            "lit", "session", 1, When);
        BoardSnapshot truth = Truth(definition, layout);
        RecognitionScore score = RecognitionScore.Compare(recognized, truth);
        check(score.ExactMatches == definition.CellCount, "Projected light changed a cell: " + string.Join("; ", score.Mismatches));

        RgbImage noisy = new RgbImage(photo.Width, photo.Height, CopyPixels(photo));
        noisy.SetPixel(0, 0, Bump(noisy, 0, 0, 0), Bump(noisy, 0, 0, 1), Bump(noisy, 0, 0, 2));
        FrameObservation afterNoise = ProjectedLightRecognizer.Recognize(definition, noisy, samples, calibration, profile, projection,
            "noise", "session", 1, When);
        check(afterNoise.Cells[definition.Index(0, 0)].State.ColorId == "red", "Two levels of sensor noise recolored the red brick.");
    }

    private static void CheckDimRoomDoesNotPaintTheTable(Action<bool, string> check)
    {
        BoardDefinition definition = Board();
        VisionCalibration calibration = Palette(new RgbColor(210, 210, 210));
        var reflectance = new LinearRgb[calibration.Colors.Count];
        for (int i = 0; i < reflectance.Length; i++) reflectance[i] = LinearRgb.Decode(calibration.Colors[i].Value);
        PhotometricProfile harsh = PhotometricProfile.Create(calibration.CalibrationId, true, new LinearRgb(0, 0, 0),
            new LinearRgb(0.05, 0.05, 0.05), new LinearRgb(0.95, 0.95, 0.95), LinearRgb.Decode(calibration.Background), reflectance);
        (int x, int y, string color, double pr, double pg, double pb)[] layout = { (2, 2, null, 0, 0, 1) };
        RgbImage photo = Photograph(definition, calibration, harsh, layout, out CellProjection projection, out GridSample[][] samples);
        FrameObservation recovered = ProjectedLightRecognizer.Recognize(definition, photo, samples, calibration, harsh, projection,
            "dim", "session", 1, When);
        check(recovered.Cells[definition.Index(2, 2)].State.Occupancy == Occupancy.Empty,
            "Strong blue projection on the empty table was not removed.");
        FrameObservation washed = FixedGridRecognizer.Recognize(definition, photo, samples, calibration, "dim-raw", "session", 1, When);
        ObservedCell washedCell = washed.Cells[definition.Index(2, 2)];
        check(washedCell.State.Occupancy != Occupancy.Empty,
            "The uncorrected classifier was expected to lose the empty table under this projector, but it still saw empty.");
    }

    private static void CheckSaturationStaysUnknown(Action<bool, string> check)
    {
        BoardDefinition definition = Board();
        VisionCalibration calibration = Palette(new RgbColor(180, 180, 180));
        var reflectance = new[] { new LinearRgb(0.92, 0.92, 0.92), new LinearRgb(0.2, 0.7, 0.2), new LinearRgb(0.2, 0.2, 0.7), new LinearRgb(0.8, 0.7, 0.2) };
        PhotometricProfile profile = PhotometricProfile.Create(calibration.CalibrationId, true, new LinearRgb(0, 0, 0),
            new LinearRgb(0.2, 0.2, 0.2), new LinearRgb(2, 2, 2), new LinearRgb(0.5, 0.5, 0.5), reflectance);
        var red = Flat(0);
        var green = Flat(0);
        var blue = Flat(0);
        red[definition.Index(1, 1)] = 1;
        green[definition.Index(1, 1)] = 1;
        blue[definition.Index(1, 1)] = 1;
        RgbImage albedo = Fill(definition, calibration.Background, (1, 1, reflectance[0].Encode()));
        GridSample[][] samples = Samples(definition);
        CellProjection projection = CellProjection.Continuous(red, green, blue);
        RgbImage photo = PhotometricComposer.Illuminate(albedo, samples, profile, projection);
        FrameObservation observation = ProjectedLightRecognizer.Recognize(definition, photo, samples, calibration, profile, projection,
            "clip", "session", 1, When);
        ObservedCell cell = observation.Cells[definition.Index(1, 1)];
        check(cell.State.Occupancy == Occupancy.Unknown, "A clipped projector channel was guessed as " + cell.State.Occupancy + " " + cell.State.ColorId + ".");
    }

    private static void CheckCalibrationRoundTrip(Action<bool, string> check)
    {
        BoardDefinition definition = Board();
        VisionCalibration calibration = Palette(new RgbColor(176, 176, 176));
        PhotometricProfile original = PhotometricProfile.Create(calibration.CalibrationId, true, new LinearRgb(0.02, 0.02, 0.02),
            new LinearRgb(0.30, 0.28, 0.31), new LinearRgb(0.40, 0.42, 0.38), LinearRgb.Decode(calibration.Background),
            calibration.Colors.Select(color => LinearRgb.Decode(color.Value)).ToArray());
        RgbColor dark = original.Bias.Encode();
        RgbColor tableBlack = original.Predict(original.TableReflectance, 0, 0, 0, out _).Encode();
        RgbColor tableWhite = original.Predict(original.TableReflectance, 1, 1, 1, out _).Encode();
        var swatches = new ColorSwatch[calibration.Colors.Count];
        for (int i = 0; i < swatches.Length; i++)
            swatches[i] = new ColorSwatch(calibration.Colors[i].Id, original.Predict(original.ColorReflectance[i], 0, 0, 0, out _).Encode());
        PhotometricProfile fitted = PhotometricProfile.Fit(calibration.CalibrationId, dark, tableBlack, tableWhite, calibration.Background, swatches, true);
        check(Math.Abs(fitted.Ambient.R - original.Ambient.R) < 0.04 && Math.Abs(fitted.Gain.B - original.Gain.B) < 0.05,
            "Black/white calibration did not recover ambient and projector gain.");

        (int x, int y, string color, double pr, double pg, double pb)[] layout =
        {
            (0, 0, "red", 0.4, 0.1, 0.2),
            (9, 9, "blue", 0.1, 0.2, 0.7),
            (4, 4, null, 0.3, 0.5, 0.8)
        };
        RgbImage albedo = Fill(definition, calibration.Background);
        foreach ((int x, int y, string color, double pr, double pg, double pb) cell in layout)
        {
            if (cell.color == null) continue;
            int index = IndexOf(calibration, cell.color);
            albedo.SetPixel(cell.x, cell.y, fitted.ColorReflectance[index].Encode().R, fitted.ColorReflectance[index].Encode().G, fitted.ColorReflectance[index].Encode().B);
        }
        var command = Commands(definition, layout);
        GridSample[][] samples = Samples(definition);
        RgbImage photo = PhotometricComposer.Illuminate(albedo, samples, fitted, command);
        FrameObservation observation = ProjectedLightRecognizer.Recognize(definition, photo, samples, calibration, fitted, command,
            "fit", "session", 1, When);
        check(RecognitionScore.Compare(observation, Truth(definition, layout)).ExactMatches == definition.CellCount,
            "A profile fitted from setup photos mislabeled the lit board.");
    }

    private static void CheckRejectedFrames(Action<bool, string> check)
    {
        BoardDefinition definition = Board();
        VisionCalibration calibration = Palette(new RgbColor(180, 180, 180));
        PhotometricProfile profile = PhotometricComposer.ForLockedCamera(calibration);
        RgbImage image = Fill(definition, calibration.Background);
        GridSample[][] samples = Samples(definition);
        FrameObservation blanked = ProjectedLightRecognizer.Recognize(definition, image, samples, calibration, profile,
            new CellProjection(Flat(0), Flat(0), Flat(0), true, true, 0.05), "blank", "session", 1, When);
        check(!blanked.Accepted && blanked.RejectionReason.Contains("消隐"), "A blanking capture was accepted.");
        FrameObservation moving = ProjectedLightRecognizer.Recognize(definition, image, samples, calibration, profile,
            new CellProjection(Flat(0), Flat(0), Flat(0), true, false, 0), "moving", "session", 1, When);
        check(!moving.Accepted && moving.RejectionReason.Contains("曝光期间"), "A shutter that crossed two projector frames was accepted.");
        FrameObservation unlocked = ProjectedLightRecognizer.Recognize(definition, image, samples, calibration, profile,
            new CellProjection(Flat(0), Flat(0), Flat(0), false, true, 0), "auto", "session", 1, When);
        check(!unlocked.Accepted && unlocked.RejectionReason.Contains("曝光未锁定"), "An auto-exposed frame was accepted.");
    }

    private static RgbImage Photograph(BoardDefinition definition, VisionCalibration calibration, PhotometricProfile profile,
        (int x, int y, string color, double pr, double pg, double pb)[] layout, out CellProjection projection, out GridSample[][] samples)
    {
        RgbImage albedo = Fill(definition, calibration.Background);
        foreach ((int x, int y, string color, double pr, double pg, double pb) cell in layout)
        {
            if (cell.color == null) continue;
            RgbColor color = calibration.Colors.First(item => item.Id == cell.color).Value;
            albedo.SetPixel(cell.x, cell.y, color.R, color.G, color.B);
        }
        projection = Commands(definition, layout);
        samples = Samples(definition);
        return PhotometricComposer.Illuminate(albedo, samples, profile, projection);
    }

    private static CellProjection Commands(BoardDefinition definition, (int x, int y, string color, double pr, double pg, double pb)[] layout)
    {
        var red = Flat(0);
        var green = Flat(0);
        var blue = Flat(0);
        foreach ((int x, int y, string color, double pr, double pg, double pb) cell in layout)
        {
            int index = definition.Index(cell.x, cell.y);
            red[index] = cell.pr;
            green[index] = cell.pg;
            blue[index] = cell.pb;
        }
        return CellProjection.Continuous(red, green, blue);
    }

    private static BoardSnapshot Truth(BoardDefinition definition, (int x, int y, string color, double pr, double pg, double pb)[] layout)
    {
        var cells = new BoardCell[definition.CellCount];
        for (int i = 0; i < cells.Length; i++) cells[i] = BoardCell.Empty;
        foreach ((int x, int y, string color, double pr, double pg, double pb) cell in layout)
            if (cell.color != null) cells[definition.Index(cell.x, cell.y)] = new BoardCell(Occupancy.Occupied, cell.color);
        return new BoardSnapshot(definition, 1, cells, SnapshotSource.Manual);
    }

    private static RgbImage Fill(BoardDefinition definition, RgbColor background, params (int x, int y, RgbColor color)[] bricks)
    {
        var packed = new byte[40 * 40 * 3];
        for (int i = 0; i < packed.Length; i += 3)
        {
            packed[i] = background.R;
            packed[i + 1] = background.G;
            packed[i + 2] = background.B;
        }
        var image = new RgbImage(40, 40, packed);
        foreach ((int x, int y, RgbColor color) brick in bricks) image.SetPixel(brick.x, brick.y, brick.color.R, brick.color.G, brick.color.B);
        return image;
    }

    private static GridSample[][] Samples(BoardDefinition definition)
    {
        var samples = new GridSample[definition.CellCount][];
        for (int y = 0; y < definition.Height; y++)
            for (int x = 0; x < definition.Width; x++)
                samples[definition.Index(x, y)] = new[] { new GridSample(x, y, x + 0.1, y + 0.1, true) };
        return samples;
    }

    private static byte Bump(RgbImage image, int x, int y, int channel)
    {
        image.TryRead(x, y, out byte r, out byte g, out byte b);
        int value = channel == 0 ? r : channel == 1 ? g : b;
        return (byte)Math.Min(255, value + 2);
    }

    private static int IndexOf(VisionCalibration calibration, string id)
    {
        for (int i = 0; i < calibration.Colors.Count; i++) if (calibration.Colors[i].Id == id) return i;
        throw new InvalidOperationException(id);
    }

    private static byte[] CopyPixels(RgbImage image)
    {
        var data = new byte[image.Width * image.Height * 3];
        for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++)
        { image.TryRead(x, y, out byte r, out byte g, out byte b); int i = (y * image.Width + x) * 3; data[i] = r; data[i + 1] = g; data[i + 2] = b; }
        return data;
    }

    private static double[] Flat(double value)
    {
        var buffer = new double[100];
        for (int i = 0; i < buffer.Length; i++) buffer[i] = value;
        return buffer;
    }

    private static BoardDefinition Board() => new BoardDefinition(colorIds: new[] { "red", "green", "blue", "yellow" });

    private static VisionCalibration Palette(RgbColor background)
    {
        return new VisionCalibration("projected-light", background, new[]
        {
            new ColorSwatch("red", new RgbColor(224, 64, 64)),
            new ColorSwatch("green", new RgbColor(64, 224, 64)),
            new ColorSwatch("blue", new RgbColor(64, 64, 224)),
            new ColorSwatch("yellow", new RgbColor(242, 191, 38))
        });
    }
}
