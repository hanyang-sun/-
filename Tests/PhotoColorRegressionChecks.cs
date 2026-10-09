using System.IO.Compression;
using BrickDefense.Core;
using BrickDefense.Vision;

internal static class PhotoColorRegressionChecks
{
    public static void Run()
    {
        var frame = ReadGeneratedFixture();
        var calibration = new ColorCalibration(Patch(frame,720,170),
            new[] { Patch(frame,808,391),Patch(frame,487,385),Patch(frame,650,636) },
            new[] { "red","blue","green" });
        var scan = PhotoRecognizer.Recognize(new CaptureSet("generated-color-regression",frame),calibration);
        AssertLayout(scan,"Generated overhead photo");
        var level = LevelBuilder.Build(GridMapper.Map(scan),calibration.CreateRules(),new GridPoint(1,18));
        Check(level.Status == ResultStatus.Success && level.Route.Length-1 == 16,
            "Generated photo no longer produces the reference level.");

        var synthetic = PhotoRecognitionChecks.Fixture();
        var shadowed = (Rgb24[])synthetic.Pixels.Clone();
        for (int y = 140; y < 640; y++)
            for (int x = 140; x < 640; x++)
            {
                var p = shadowed[y*synthetic.Width+x];
                double light = 0.35+0.65*(x-140)/499;
                shadowed[y*synthetic.Width+x] = new Rgb24((byte)(p.R*light),(byte)(p.G*light),(byte)(p.B*light));
            }
        var idealCalibration = new ColorCalibration(new Rgb24(245,245,245),
            new[] { new Rgb24(220,35,35),new Rgb24(35,80,220),new Rgb24(30,185,45) },
            new[] { "red","blue","green" });
        var shadedFrame = new ImageFrame("lighting-gradient",800,800,shadowed,DateTime.UnixEpoch);
        AssertLayout(PhotoRecognizer.Recognize(new CaptureSet("lighting-gradient",shadedFrame),idealCalibration),
            "Shadowed bricks and paper");

        // There is no reliable chromatic information in a nearly black cell.
        for (int y = 190; y < 240; y++)
            for (int x = 190; x < 240; x++) shadowed[y*800+x] = new Rgb24(4,4,4);
        var dark = PhotoRecognizer.Recognize(new CaptureSet("dark-cell",shadedFrame),idealCalibration);
        Check(dark.Status == ResultStatus.Rejected && dark.Issues.Any(i => i.Code == "CELL_UNCERTAIN" &&
            i.PhysicalCell?.X == 1 && i.PhysicalCell?.Y == 1),"Near-black cell was accepted as paper.");
        Console.WriteLine("Color regressions passed: unchanged generated photo (9 bricks), lighting gradient, dark-cell rejection.");
    }

    private static void AssertLayout(ScanResult scan,string name)
    {
        Check(scan.Status == ResultStatus.Success && scan.PhysicalCells.Length == 100 && scan.Bricks.Length == 9,
            name+" failed: "+string.Join("; ",scan.Issues.Select(i => i.Code+":"+i.Message)));
        var expected = new[] { (3,2,"blue"),(4,2,"blue"),(5,2,"blue"),(7,2,"red"),(3,3,"blue"),
            (3,4,"blue"),(3,5,"blue"),(5,5,"green"),(7,7,"red") };
        Check(expected.All(e => scan.Bricks.Any(b => b.C == e.Item1 && b.R == e.Item2 && b.ColorId == e.Item3)),
            name+" has an incorrect cell or color.");
        Check(scan.PhysicalCells.Count(c => c.State == PhysicalCellState.Empty) == 91,
            name+" classified a paper shadow as an occupied cell.");
    }

    public static ImageFrame ReadGeneratedFixture()
    {
        using var file = File.OpenRead(Path.Combine(AppContext.BaseDirectory,"Fixtures","generated-overhead-bricks-v1.rgb.gz"));
        using var gzip = new GZipStream(file,CompressionMode.Decompress);
        using var reader = new BinaryReader(gzip);
        int width = reader.ReadInt32(),height = reader.ReadInt32();
        Check(width == 1254 && height == 1254,"Generated RGB fixture dimensions changed.");
        var pixels = new Rgb24[width*height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Rgb24(reader.ReadByte(),reader.ReadByte(),reader.ReadByte());
        return new ImageFrame("generated-overhead-v1",width,height,pixels,DateTime.UnixEpoch);
    }

    // Same 7x7 channel median as the user's image-click calibration, with fixed locations.
    private static Rgb24 Patch(ImageFrame frame,int x,int y)
    {
        var red = new byte[49]; var green = new byte[49]; var blue = new byte[49]; int i = 0;
        for (int dy = -3; dy <= 3; dy++)
            for (int dx = -3; dx <= 3; dx++)
            { var p = frame.Pixels[(y+dy)*frame.Width+x+dx]; red[i]=p.R; green[i]=p.G; blue[i++]=p.B; }
        Array.Sort(red);Array.Sort(green);Array.Sort(blue);
        return new Rgb24(red[24],green[24],blue[24]);
    }

    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
}
