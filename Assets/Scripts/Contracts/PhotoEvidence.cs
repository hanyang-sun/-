namespace BrickDefense.Core
{
    public readonly struct ImagePoint
    {
        public double X { get; }
        public double Y { get; }
        public ImagePoint(double x, double y) { X = x; Y = y; }
    }

    public readonly struct Rgb24
    {
        public byte R { get; }
        public byte G { get; }
        public byte B { get; }
        public Rgb24(byte r, byte g, byte b) { R = r; G = g; B = b; }
        public int Grey => (299 * R + 587 * G + 114 * B + 500) / 1000;
    }

    public enum LayoutCoordinateBasis { MarkedBoard, RelativeBrickScale }

    public sealed class BoardObservation
    {
        public ImagePoint[] Corners { get; }
        public double[] BoardToImage { get; }
        public int[] MarkerIds { get; }
        public double Confidence { get; }
        public bool IsReliable { get; }
        public LayoutCoordinateBasis CoordinateBasis { get; }
        public string Direction => CoordinateBasis == LayoutCoordinateBasis.MarkedBoard ?
            "125=top-left,248=top-right,511=bottom-right,742=bottom-left" : "image-top-left; relative brick-scale layout";
        public BoardObservation(ImagePoint[] corners, double[] transform, int[] ids,
            double confidence, bool reliable, LayoutCoordinateBasis coordinateBasis = LayoutCoordinateBasis.MarkedBoard)
        { Corners = corners; BoardToImage = transform; MarkerIds = ids;
            Confidence = confidence; IsReliable = reliable; CoordinateBasis = coordinateBasis; }
    }

    public sealed class ImageEvidence
    {
        public string FrameId { get; }
        public ImagePoint Center { get; }
        public ImagePoint[] Region { get; }
        public ImageEvidence(string frameId, ImagePoint center, ImagePoint[] region)
        { FrameId = frameId; Center = center; Region = region; }
    }
}
