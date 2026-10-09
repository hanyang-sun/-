using BrickDefense.Core;
using BrickDefense.Vision;
using BrickDefense.ThirdParty;

internal static class PhotoRecognitionChecks
{
    private static readonly Rgb24 Paper = new(245,245,245);
    private static readonly Rgb24[] Colors = { new(220,35,35),new(35,80,220),new(30,185,45) };
    private static readonly ColorCalibration Calibration = new(Paper,Colors,new[] { "red","blue","green" });
    private static readonly string[] Rows =
    { "..........","..........","...WWW.K..","...W......","...W......",
        "...W.T....","..........",".......K..","..........",".........." };

    public static void Run()
    {
        foreach (int id in new[] {125,248,511,742})
            Assert(ArucoCode.TryDecode(ArucoCode.Encode(id),out int decoded,out _) && decoded == id,
                "Selected marker ID is ambiguous or invalid.");
        var normal = Fixture();
        var scan = Recognize(normal);
        Assert(scan.Status == ResultStatus.Success, "Pixel recognition failed: " + Issues(scan));
        Assert(scan.PhysicalCells.Length == 100 && scan.Bricks.Length == 9 && scan.Board.IsReliable,
            "Photo scan grid or evidence missing.");
        Assert(scan.Bricks.Count(b => b.ColorId == "blue") == 6 && scan.Bricks.Select(b => b.BrickId).Distinct().Count() == 9,
            "Adjacent same-color bricks merged.");
        Assert(scan.Bricks.All(b => b.Evidence.Length == 1 && b.Evidence[0].FrameId == normal.FrameId &&
            b.Evidence[0].Region.Length == 4 && b.Confidence >= 0.88), "Photo evidence or confidence missing.");
        var level = LevelBuilder.Build(GridMapper.Map(scan),Calibration.CreateRules(),new GridPoint(1,18));
        Assert(level.Status == ResultStatus.Success && level.Route.Length-1 == 16, "Photo-to-level pipeline failed.");
        var rotated = normal;
        for (int rotation = 1; rotation <= 3; rotation++)
        {
            rotated = Rotate(rotated);
            var result = Recognize(rotated);
            Assert(result.Status == ResultStatus.Success && result.Bricks.All(b =>
                scan.Bricks.Any(original => original.C == b.C && original.R == b.R && original.ColorId == b.ColorId)),
                "Paper orientation failed after rotation: " + Issues(result));
        }
        var skewed = Recognize(Fixture(perspective:true));
        Assert(skewed.Status == ResultStatus.Success && skewed.Bricks.Length == 9,
            "Perspective photo failed: " + Issues(skewed));
        var missing = Recognize(Fixture(missingMarker:true));
        Assert(missing.Status == ResultStatus.Rejected && missing.Issues.Any(i => i.Code == "BOARD_MARKERS_MISSING"),
            "Missing corner marker accepted.");
        var unknown = Recognize(Fixture(unknown:true));
        Assert(unknown.Status == ResultStatus.Rejected && unknown.Issues.Any(i => i.Code == "CELL_UNCERTAIN" &&
            i.PhysicalCell?.X == 1 && i.PhysicalCell?.Y == 1), "Unknown color must reject with cell evidence.");
        var mixed = Recognize(Fixture(mixed:true));
        Assert(mixed.Status == ResultStatus.Rejected && mixed.Issues.Any(i => i.Code == "CELL_UNCERTAIN"),
            "Mixed/cross-cell color accepted.");
        var edgeMixed = Recognize(Fixture(edgeMixed:true));
        Assert(edgeMixed.Status == ResultStatus.Rejected && edgeMixed.Issues.Any(i => i.Code == "CELL_UNCERTAIN" &&
            i.PhysicalCell?.X == 1 && i.PhysicalCell?.Y == 1), "Edge-only second color accepted.");
        var edgeUnknown = Recognize(Fixture(edgeUnknown:true));
        Assert(edgeUnknown.Status == ResultStatus.Rejected && edgeUnknown.Issues.Any(i => i.Code == "CELL_UNCERTAIN" &&
            i.PhysicalCell?.X == 1 && i.PhysicalCell?.Y == 1), "Edge-only unknown color accepted.");
        Assert(PhotoRecognizer.Recognize(new CaptureSet("uncalibrated",normal),null).Issues[0].Code == "CALIBRATION_REQUIRED",
            "Missing calibration accepted.");
        Console.WriteLine("Photo checks passed: pixel-to-level, rotations, perspective, adjacent colors and rejection cases.");
        if (Environment.GetCommandLineArgs().Contains("--write-photo-fixture"))
        {
            Directory.CreateDirectory("Tests/Fixtures");
            WritePng(normal,"Tests/Fixtures/synthetic-reference.png");
            Console.WriteLine("Wrote synthetic image fixture (not a real photograph).");
        }
    }

    private static ScanResult Recognize(ImageFrame frame) => PhotoRecognizer.Recognize(new CaptureSet("photo-check",frame),Calibration);
    private static string Issues(ScanResult scan) => string.Join("; ",scan.Issues.Select(i => i.Code+":"+i.Message));
    private static void Assert(bool value,string message) { if (!value) throw new Exception(message); }

    // Independent raster oracle: invert this known image projection algebraically per pixel.
    // Does not use production marker encoder, locator or homography solver to generate the input.
    public static ImageFrame Fixture(bool perspective = false,bool missingMarker = false,bool unknown = false,bool mixed = false,
        bool edgeMixed = false,bool edgeUnknown = false)
    {
        const int size = 800; var pixels = new Rgb24[size*size];
        int[] patterns = { 16,23,9,14 }, ids = {125,248,511,742};
        double[] cx = {1,13,13,1}, cy = {1,1,13,13};
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                double u = x-40.0,v = y-40.0;
                double a = perspective ? 50-0.01*u : 50, b = perspective ? 4-0.005*u : 0;
                double c = perspective ? 3-0.01*v : 0, d = perspective ? 48-0.005*v : 50;
                double det = a*d-b*c, px = (u*d-b*v)/det, py = (a*v-u*c)/det;
                var color = new Rgb24(190,178,165);
                if (px >= 0 && py >= 0 && px < 14 && py < 14)
                {
                    color = Paper;
                    for (int i = 0; i < 4; i++)
                    {
                        if (missingMarker && i == 0) continue;
                        int mc = (int)Math.Floor((px-(cx[i]-0.7))/0.2), mr = (int)Math.Floor((py-(cy[i]-0.7))/0.2);
                        if (mc < 0 || mr < 0 || mc > 6 || mr > 6) continue;
                        bool white = mr > 0 && mr < 6 && mc > 0 && mc < 6 &&
                            (patterns[(ids[i] >> (2*(5-mr))) & 3] & (1 << (5-mc))) != 0;
                        color = white ? Paper : new Rgb24(10,10,10);
                    }
                    if (px >= 2 && py >= 2 && px < 12 && py < 12)
                    {
                        int col = (int)(px-2), row = (int)(py-2); char symbol = Rows[row][col];
                        color = symbol == 'K' ? Colors[0] : symbol == 'W' ? Colors[1] : symbol == 'T' ? Colors[2] : Paper;
                        if (unknown && col == 1 && row == 1) color = new Rgb24(230,200,25);
                        if (mixed && col == 1 && row == 1) color = px < 3.5 ? Colors[0] : Colors[1];
                        if (edgeMixed && col == 1 && row == 1) color = px < 3.2 ? Colors[0] : Colors[1];
                        if (edgeUnknown && col == 1 && row == 1) color = px < 3.2 ? new Rgb24(230,200,25) : Colors[1];
                        if ((px-2)%1 < 0.018 || (py-2)%1 < 0.018) color = new Rgb24(140,140,140);
                    }
                }
                pixels[y*size+x] = color;
            }
        return new ImageFrame("synthetic-rgb",size,size,pixels,DateTime.UtcNow);
    }

    private static ImageFrame Rotate(ImageFrame frame)
    {
        var pixels = new Rgb24[frame.Pixels.Length];
        for (int y = 0; y < frame.Height; y++)
            for (int x = 0; x < frame.Width; x++) pixels[x*frame.Height+(frame.Height-1-y)] = frame.Pixels[y*frame.Width+x];
        return new ImageFrame(frame.FrameId,frame.Height,frame.Width,pixels,frame.CapturedAtUtc);
    }

    private static void WritePng(ImageFrame frame,string path)
    {
        using var file = File.Create(path); file.Write(new byte[] {137,80,78,71,13,10,26,10});
        var header = new byte[13]; BigEndian(header,0,frame.Width); BigEndian(header,4,frame.Height); header[8]=8; header[9]=2;
        Chunk(file,"IHDR",header);
        using var compressed = new MemoryStream();
        using (var zlib = new System.IO.Compression.ZLibStream(compressed,System.IO.Compression.CompressionLevel.Optimal,true))
            for (int y = 0; y < frame.Height; y++)
            {
                zlib.WriteByte(0);
                for (int x = 0; x < frame.Width; x++)
                { var p = frame.Pixels[y*frame.Width+x]; zlib.WriteByte(p.R); zlib.WriteByte(p.G); zlib.WriteByte(p.B); }
            }
        Chunk(file,"IDAT",compressed.ToArray()); Chunk(file,"IEND",Array.Empty<byte>());
    }
    private static void Chunk(Stream stream,string name,byte[] data)
    {
        var length = new byte[4]; BigEndian(length,0,data.Length); stream.Write(length);
        byte[] kind = System.Text.Encoding.ASCII.GetBytes(name); stream.Write(kind); stream.Write(data);
        uint crc = uint.MaxValue;
        foreach (byte b in kind.Concat(data))
        { crc ^= b; for (int i=0;i<8;i++) crc=(crc&1)!=0 ? (crc>>1)^0xedb88320u : crc>>1; }
        BigEndian(length,0,unchecked((int)~crc)); stream.Write(length);
    }
    private static void BigEndian(byte[] b,int offset,int value)
    { for (int i=0;i<4;i++) b[offset+i]=(byte)(value>>(24-8*i)); }
}
