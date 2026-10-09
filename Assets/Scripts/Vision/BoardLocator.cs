using System;
using System.Collections.Generic;
using BrickDefense.Core;
using BrickDefense.ThirdParty;

namespace BrickDefense.Vision
{
    public static class ScanBoardLayout
    {
        public static readonly int[] MarkerIds = { 125, 248, 511, 742 };
        public static readonly ImagePoint[] MarkerCenters =
        { new ImagePoint(1,1), new ImagePoint(13,1), new ImagePoint(13,13), new ImagePoint(1,13) };
        public const double MarkerHalfSize = 0.7;
        public static ImagePoint[] MarkerCorners(int index)
        {
            ImagePoint p = MarkerCenters[index]; double s = MarkerHalfSize;
            return new[] { new ImagePoint(p.X-s,p.Y-s), new ImagePoint(p.X+s,p.Y-s),
                new ImagePoint(p.X+s,p.Y+s), new ImagePoint(p.X-s,p.Y+s) };
        }
    }

    public static class BoardLocator
    {
        public static bool TryLocate(ImageFrame frame, PhotoScanSettings settings,
            out BoardObservation observation, out Issue issue)
        {
            observation = null; issue = null;
            int factor = Math.Max(1, (int)Math.Ceiling((double)Math.Max(frame.Width, frame.Height) /
                settings.MaximumDetectionSide));
            int w = (frame.Width + factor - 1)/factor, h = (frame.Height + factor - 1)/factor;
            var grey = new byte[w*h];
            var histogram = new int[256];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                { int g = frame.Pixels[y*factor*frame.Width+x*factor].Grey;
                    grey[y*w+x] = (byte)g; histogram[g]++; }
            int threshold = Math.Max(25, Math.Min(170, Otsu(histogram, grey.Length)));
            var visited = new bool[grey.Length];
            var queue = new int[grey.Length];
            var markers = new Dictionary<int, ImagePoint[]>();
            for (int start = 0; start < grey.Length; start++)
            {
                if (visited[start] || grey[start] > threshold) continue;
                int count = 1, head = 0, minX = w, minY = h, maxX = 0, maxY = 0;
                queue[0] = start; visited[start] = true;
                var boundary = new List<ImagePoint>();
                while (head < count)
                {
                    int current = queue[head++], x = current%w, y = current/w;
                    minX = Math.Min(minX,x); maxX = Math.Max(maxX,x);
                    minY = Math.Min(minY,y); maxY = Math.Max(maxY,y);
                    bool edge = false;
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int nx = x+dx, ny = y+dy;
                            if (nx < 0 || ny < 0 || nx >= w || ny >= h) { edge = true; continue; }
                            int next = ny*w+nx;
                            if (grey[next] > threshold) { edge = true; continue; }
                            if (!visited[next]) { visited[next] = true; queue[count++] = next; }
                        }
                    if (edge) boundary.Add(new ImagePoint(x*factor,y*factor));
                }
                if (count < 80 || minX == 0 || minY == 0 || maxX == w-1 || maxY == h-1 ||
                    maxX-minX < 21 || maxY-minY < 21 ||
                    (maxX-minX)*(maxY-minY) > w*h/6) continue;
                ImagePoint[] quad = FitQuad(boundary);
                if (quad == null || !TryReadMarker(frame,quad,out int id,out ImagePoint[] ordered)) continue;
                if (Array.IndexOf(ScanBoardLayout.MarkerIds,id) < 0) continue;
                if (markers.ContainsKey(id))
                { issue = new Issue("BOARD_MARKER_DUPLICATE", $"定位标记 {id} 重复，请只拍一张格纸。",frameId:frame.FrameId); return false; }
                markers.Add(id,ordered);
            }
            if (markers.Count != 4)
            { issue = new Issue("BOARD_MARKERS_MISSING", $"仅找到 {markers.Count}/4 个定位标记；请使用配套格纸，将四角完整拍入。",frameId:frame.FrameId); return false; }

            var centers = new ImagePoint[4];
            for (int i = 0; i < 4; i++)
            {
                ProjectiveMap.TryFit(ProjectiveMap.UnitSquare,markers[ScanBoardLayout.MarkerIds[i]],out ProjectiveMap markerMap);
                centers[i] = markerMap.Project(0.5,0.5);
            }
            if (!Convex(centers) || !ProjectiveMap.TryFit(ScanBoardLayout.MarkerCenters,centers,out ProjectiveMap paper))
            { issue = new Issue("BOARD_GEOMETRY_INVALID", "格纸标记位置或方向不一致，请平铺格纸并俯拍。",frameId:frame.FrameId); return false; }
            double maximumError = 0;
            for (int i = 0; i < 4; i++)
            {
                ImagePoint[] actual = markers[ScanBoardLayout.MarkerIds[i]], expected = ScanBoardLayout.MarkerCorners(i);
                double side = Math.Min(Distance(actual[0],actual[1]),Distance(actual[1],actual[2]));
                for (int c = 0; c < 4; c++)
                    maximumError = Math.Max(maximumError, Distance(paper.Project(expected[c].X,expected[c].Y),actual[c])/side);
            }
            if (maximumError > settings.MaximumMarkerFitError)
            { issue = new Issue("BOARD_GEOMETRY_INVALID", "标记与格纸透视不一致；请检查打印比例、方向或斜拍。",frameId:frame.FrameId); return false; }
            var corners = new[] { paper.Project(2,2),paper.Project(12,2),paper.Project(12,12),paper.Project(2,12) };
            for (int i = 0; i < 4; i++)
                if (!Inside(frame,corners[i]) || Distance(corners[i],corners[(i+1)%4])/10 < settings.MinimumCellPixels)
                { issue = new Issue("IMAGE_QUALITY_INSUFFICIENT", "格纸区域不完整或成像过小，请靠近后将整纸拍入。",frameId:frame.FrameId); return false; }
            if (!ProjectiveMap.TryFit(new[] { new ImagePoint(0,0),new ImagePoint(10,0),
                new ImagePoint(10,10),new ImagePoint(0,10) },corners,out ProjectiveMap board))
            { issue = new Issue("BOARD_GEOMETRY_INVALID", "格纸投影无法计算。",frameId:frame.FrameId); return false; }
            for (int r = 0; r < 10; r++)
                for (int c = 0; c < 10; c++)
                {
                    var region = new[] { board.Project(c,r),board.Project(c+1,r),board.Project(c+1,r+1),board.Project(c,r+1) };
                    for (int i = 0; i < 4; i++)
                        if (!Inside(frame,region[i]) || Distance(region[i],region[(i+1)%4]) < settings.MinimumCellPixels)
                        { issue = new Issue("IMAGE_QUALITY_INSUFFICIENT", "局部格位成像过小或透视过强，请靠近并竖直俯拍。",new GridPoint(c,r),frame.FrameId); return false; }
                }
            observation = new BoardObservation(corners,board.Coefficients,(int[])ScanBoardLayout.MarkerIds.Clone(),
                Math.Max(0,1-maximumError),true); return true;
        }

        private static bool TryReadMarker(ImageFrame frame, ImagePoint[] quad, out int id, out ImagePoint[] ordered)
        {
            id = -1; ordered = null;
            if (!ProjectiveMap.TryFit(ProjectiveMap.UnitSquare,quad,out ProjectiveMap map)) return false;
            var values = new int[7,7]; int min = 255, max = 0;
            for (int r = 0; r < 7; r++)
                for (int c = 0; c < 7; c++)
                {
                    int total = 0;
                    for (int sy = -1; sy <= 1; sy++)
                        for (int sx = -1; sx <= 1; sx++)
                        {
                            ImagePoint p = map.Project((c+0.5+sx*0.18)/7,(r+0.5+sy*0.18)/7);
                            if (!Inside(frame,p)) return false;
                            total += frame.Sample(p).Grey;
                        }
                    values[r,c] = total/9; min = Math.Min(min,values[r,c]); max = Math.Max(max,values[r,c]);
                }
            if (max-min < 60) return false;
            var bits = new bool[7,7]; int cut = (min+max)/2;
            for (int r = 0; r < 7; r++)
                for (int c = 0; c < 7; c++) bits[r,c] = values[r,c] > cut;
            if (!ArucoCode.TryDecode(bits,out id,out int rotations)) return false;
            ordered = new ImagePoint[4];
            for (int i = 0; i < 4; i++) ordered[i] = quad[(4-rotations+i)%4];
            return true;
        }

        // Convex hull reduces each isolated black candidate to its outer contour. Raster stair steps
        // are simplified to a quad; residual checks reject circles and irregular components.
        private static ImagePoint[] FitQuad(List<ImagePoint> points)
        {
            points.Sort((a,b) => a.X != b.X ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y));
            var hull = new List<ImagePoint>();
            foreach (ImagePoint p in points)
            {
                while (hull.Count >= 2 && Cross(hull[hull.Count-2],hull[hull.Count-1],p) <= 0) hull.RemoveAt(hull.Count-1);
                hull.Add(p);
            }
            int lower = hull.Count;
            for (int i = points.Count-2; i >= 0; i--)
            {
                ImagePoint p = points[i];
                while (hull.Count > lower && Cross(hull[hull.Count-2],hull[hull.Count-1],p) <= 0) hull.RemoveAt(hull.Count-1);
                hull.Add(p);
            }
            if (hull.Count > 0) hull.RemoveAt(hull.Count-1);
            if (hull.Count < 4) return null;
            ImagePoint[] original = hull.ToArray();
            while (hull.Count > 4)
            {
                int selected = 0; double smallest = double.MaxValue;
                for (int i = 0; i < hull.Count; i++)
                {
                    double area = Math.Abs(Cross(hull[(i+hull.Count-1)%hull.Count],hull[i],hull[(i+1)%hull.Count]));
                    if (area < smallest) { smallest = area; selected = i; }
                }
                hull.RemoveAt(selected);
            }
            ImagePoint[] quad = hull.ToArray();
            double minSide = double.MaxValue, maxSide = 0;
            for (int i = 0; i < 4; i++)
            { double side = Distance(quad[i],quad[(i+1)%4]); minSide = Math.Min(minSide,side); maxSide = Math.Max(maxSide,side); }
            if (minSide < 21 || maxSide/minSide > 3) return null;
            foreach (ImagePoint p in original)
            {
                double nearest = double.MaxValue;
                for (int i = 0; i < 4; i++) nearest = Math.Min(nearest,
                    Math.Abs(Cross(quad[i],quad[(i+1)%4],p))/Distance(quad[i],quad[(i+1)%4]));
                if (nearest > minSide*0.06) return null;
            }
            return quad;
        }

        private static int Otsu(int[] histogram, int count)
        {
            double sum = 0, sumLow = 0, best = -1; int low = 0, cut = 0;
            for (int i = 0; i < 256; i++) sum += i*(double)histogram[i];
            for (int i = 0; i < 256; i++)
            {
                low += histogram[i]; sumLow += i*(double)histogram[i];
                int high = count-low; if (low == 0) continue; if (high == 0) break;
                double delta = sumLow/low - (sum-sumLow)/high, variance = low*(double)high*delta*delta;
                if (variance > best) { best = variance; cut = i; }
            }
            return cut;
        }

        internal static bool Inside(ImageFrame f, ImagePoint p) =>
            !double.IsNaN(p.X) && !double.IsNaN(p.Y) && p.X >= 0 && p.Y >= 0 && p.X <= f.Width-1 && p.Y <= f.Height-1;
        internal static double Distance(ImagePoint a, ImagePoint b) => Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Y-b.Y)*(a.Y-b.Y));
        private static double Cross(ImagePoint a, ImagePoint b, ImagePoint c) => (b.X-a.X)*(c.Y-a.Y)-(b.Y-a.Y)*(c.X-a.X);
        private static bool Convex(ImagePoint[] p)
        {
            double sign = Cross(p[0],p[1],p[2]); if (sign <= 0) return false;
            for (int i = 1; i < 4; i++) if (Cross(p[i],p[(i+1)%4],p[(i+2)%4]) <= 0) return false;
            return true;
        }
    }
}
