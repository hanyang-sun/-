using BrickDefense.Core;
using BrickDefense.Vision;

byte[] bytes = File.ReadAllBytes(args[0]);
int width = BitConverter.ToInt32(bytes,0),height = BitConverter.ToInt32(bytes,4);
var pixels = new Rgb24[width*height];
for (int i = 0; i < pixels.Length; i++) pixels[i] = new Rgb24(bytes[8+3*i],bytes[9+3*i],bytes[10+3*i]);
var frame = new ImageFrame("generated-photo",width,height,pixels,DateTime.UnixEpoch);
Rgb24 Patch(int x,int y)
{
    var r = new byte[49]; var g = new byte[49]; var b = new byte[49]; int index = 0;
    for (int dy = -3; dy <= 3; dy++) for (int dx = -3; dx <= 3; dx++)
    { var p = pixels[(y+dy)*width+x+dx]; r[index]=p.R; g[index]=p.G; b[index++]=p.B; }
    Array.Sort(r);Array.Sort(g);Array.Sort(b);return new Rgb24(r[24],g[24],b[24]);
}
var calibration = new ColorCalibration(Patch(720,170),new[] {Patch(808,391),Patch(487,385),Patch(650,636)},new[] {"red","blue","green"});
Console.WriteLine("Calibration: "+string.Join(" / ",new[] {calibration.Paper}.Concat(calibration.BrickColors).Select(c=>$"{c.R},{c.G},{c.B}")));
var settings = new PhotoScanSettings();
if (args.Contains("--relax-colors")) { settings.MaximumColorDistance=0.50;settings.MinimumColorMargin=0.01; }
if (args.Contains("--inner-grid")) settings.MinimumCellAgreement=0.5;
if (args.Contains("--relax-marker-fit")) settings.MaximumMarkerFitError=0.49;
if (args.Contains("--detection-full")) settings.MaximumDetectionSide=2000;
var scan = PhotoRecognizer.Recognize(new CaptureSet("generated-repro",frame),calibration,settings);
Console.WriteLine($"status={scan.Status} cells={scan.PhysicalCells.Length} bricks={scan.Bricks.Length} board={scan.Board != null}");
Console.WriteLine(string.Join("; ",scan.Issues.Select(i=>i.Code+":"+i.Message)));
if (scan.Board != null) Console.WriteLine("Corners: "+string.Join("; ",scan.Board.Corners.Select(p=>$"{p.X:F1},{p.Y:F1}")));
Console.WriteLine("Bricks: "+string.Join("; ",scan.Bricks.Select(b=>$"({b.C},{b.R})={b.ColorId}/{b.Confidence:F2}")));
Console.WriteLine("Uncertain: "+string.Join("; ",scan.PhysicalCells.Where(c=>c.State==PhysicalCellState.Uncertain).Select(c=>$"({c.C},{c.R})={c.Confidence:F2}")));
if (args.Contains("--color-probe") && scan.Board != null)
{
    var map = new ProjectiveMap(scan.Board.BoardToImage);
    var palette = new[] {calibration.Paper}.Concat(calibration.BrickColors).Select(BrickDefense.ThirdParty.NormalizedLab.From).ToArray();
    for (int y = 0; y < 10; y++)
    {
        string row = "";
        for (int x = 0; x < 10; x++)
        {
            Rgb24 rgb = frame.Sample(map.Project(3+.08+.84*x/9,2+.08+.84*y/9));
            var feature = BrickDefense.ThirdParty.NormalizedLab.From(rgb);
            var matches = palette.Select((p,i)=>(i,d:BrickDefense.ThirdParty.NormalizedLab.Distance(feature,p))).OrderBy(p=>p.d).ToArray();
            bool accepted = feature.Brightness >= settings.MinimumColorBrightness && matches[0].d <= settings.MaximumColorDistance && matches[1].d-matches[0].d >= settings.MinimumColorMargin;
            row += accepted ? ".RBG"[matches[0].i] : '?';
            if (!accepted) Console.WriteLine($"[DEBUG-color] ({x},{y}) rgb={rgb.R},{rgb.G},{rgb.B} closest={matches[0].i} d={matches[0].d:F3} margin={matches[1].d-matches[0].d:F3} brightness={feature.Brightness:F3}");
        }
        Console.WriteLine("[DEBUG-color] "+row);
    }
}
bool pass = scan.Status==ResultStatus.Success && scan.Bricks.Length==9 && scan.Bricks.Count(b=>b.ColorId=="red")==2 &&
    scan.Bricks.Count(b=>b.ColorId=="blue")==6 && scan.Bricks.Count(b=>b.ColorId=="green")==1;
int cellArgument = Array.IndexOf(args,"--cell");
if (cellArgument >= 0)
{
    string[] coordinates = args[cellArgument+1].Split(','); int c = int.Parse(coordinates[0]),r = int.Parse(coordinates[1]);
    PhysicalCellObservation cell = scan.PhysicalCells.First(p=>p.C==c&&p.R==r);
    bool expectedEmpty = c==4 && r==3;
    pass = expectedEmpty ? cell.State==PhysicalCellState.Empty : cell.State==PhysicalCellState.Occupied &&
        scan.Bricks.Any(b=>b.C==c&&b.R==r&&b.ColorId=="blue");
    Console.WriteLine($"Single-cell check ({c},{r}): expected={(expectedEmpty?"Empty":"blue")}, actual={cell.State}/{cell.Confidence:F2}");
}
Console.WriteLine(pass ? "PHOTO_COLOR_REPRO_PASSED" : "PHOTO_COLOR_REPRO_FAILED");
return pass ? 0 : 1;
