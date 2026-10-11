using System;
using System.Globalization;
using System.Text;
using BrickDefense.ThirdParty;

namespace BrickDefense.Vision
{
    // 按实测积木边长导出格纸，格线与四角标记共享同一纸面坐标系。
    public static class ScanBoardTemplate
    {
        public static string CreateSvg(double brickEdgeMm)
        {
            if (double.IsNaN(brickEdgeMm) || double.IsInfinity(brickEdgeMm) || brickEdgeMm <= 0 || brickEdgeMm > 100)
                throw new ArgumentOutOfRangeException(nameof(brickEdgeMm),"Enter the measured brick edge in millimetres.");
            string dimension = (14*brickEdgeMm).ToString("0.###",CultureInfo.InvariantCulture);
            var svg = new StringBuilder();
            svg.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{dimension}mm\" height=\"{dimension}mm\" viewBox=\"0 0 1400 1400\">\n");
            svg.Append("<title>BrickDefense 10x10 scan board; print at actual size</title>\n<rect width=\"1400\" height=\"1400\" fill=\"white\"/>\n");
            for (int line = 0; line <= 10; line++)
            {
                int p = 200+line*100;
                svg.Append($"<path d=\"M {p} 200 V 1200 M 200 {p} H 1200\" stroke=\"#999\" stroke-width=\"1\"/>\n");
            }
            for (int i = 0; i < 4; i++)
            {
                bool[,] code = ArucoCode.Encode(ScanBoardLayout.MarkerIds[i]);
                int x = (int)Math.Round(ScanBoardLayout.MarkerCenters[i].X*100)-70;
                int y = (int)Math.Round(ScanBoardLayout.MarkerCenters[i].Y*100)-70;
                svg.Append($"<g data-marker-id=\"{ScanBoardLayout.MarkerIds[i]}\" shape-rendering=\"crispEdges\">\n");
                for (int r = 0; r < 7; r++)
                    for (int c = 0; c < 7; c++)
                        if (!code[r,c]) svg.Append($"<rect x=\"{x+c*20}\" y=\"{y+r*20}\" width=\"20\" height=\"20\" fill=\"black\"/>\n");
                svg.Append("</g>\n");
            }
            for (int i = 0; i < 10; i++)
                svg.Append($"<text x=\"{250+i*100}\" y=\"185\" font-size=\"18\" text-anchor=\"middle\" fill=\"#777\">{i}</text>"+
                    $"<text x=\"180\" y=\"{256+i*100}\" font-size=\"18\" text-anchor=\"middle\" fill=\"#777\">{i}</text>\n");
            svg.Append("<text x=\"700\" y=\"1375\" text-anchor=\"middle\" font-size=\"18\">BrickDefense - 125 top-left - print at actual size, keep margins clear</text>\n</svg>\n");
            return svg.ToString();
        }
    }
}
