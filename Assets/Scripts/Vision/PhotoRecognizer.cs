using System;
using System.Collections.Generic;
using BrickDefense.Core;
using BrickDefense.ThirdParty;

namespace BrickDefense.Vision
{
    public static class PhotoRecognizer
    {
        public static ScanResult Recognize(CaptureSet capture, ColorCalibration calibration,
            PhotoScanSettings settings = null)
        {
            settings = settings ?? new PhotoScanSettings();
            var issues = new List<Issue>(); string scanId = capture?.ScanId;
            if (capture?.Frames == null || capture.Frames.Length != 1 ||
                string.IsNullOrWhiteSpace(scanId) || capture.Frames[0] == null)
                return Rejected(scanId,"CAPTURE_INVALID","需要一张俯拍照片和扫描编号。");
            ImageFrame frame = capture.Frames[0];
            if (string.IsNullOrWhiteSpace(frame.FrameId) || frame.FrameId != capture.PrimaryFrameId ||
                frame.Width < 100 || frame.Height < 100 || frame.Width > 4096 || frame.Height > 4096 ||
                frame.Pixels == null || frame.Pixels.Length != frame.Width*(long)frame.Height)
                return Rejected(scanId,"CAPTURE_INVALID","照片尺寸或像素无效；支持长边不超过 4096 的 RGB 照片。");
            if (!SettingsValid(settings))
                return Rejected(scanId,"SCAN_SETTINGS_INVALID","识别配置无效。");
            if (!CalibrationValid(calibration,settings))
                return Rejected(scanId,"CALIBRATION_REQUIRED","请标定纸面和三种积木颜色；颜色必须可区分。");
            if (!BoardLocator.TryLocate(frame,settings,out BoardObservation board,out Issue locationIssue))
                return new ScanResult(scanId,ResultStatus.Rejected,Array.Empty<PhysicalCellObservation>(),
                    Array.Empty<BrickObservation>(),new[] { locationIssue });
            var map = new ProjectiveMap(board.BoardToImage);
            var cells = new PhysicalCellObservation[100]; var bricks = new List<BrickObservation>();
            var palette = new Rgb24[4]; palette[0] = calibration.Paper;
            Array.Copy(calibration.BrickColors,0,palette,1,3);
            var colors = new NormalizedLab[4];
            for (int i = 0; i < 4; i++) colors[i] = NormalizedLab.From(palette[i]);
            for (int r = 0; r < 10; r++)
                for (int c = 0; c < 10; c++)
                {
                    var votes = new int[4];
                    for (int y = 0; y < 10; y++)
                        for (int x = 0; x < 10; x++)
                        {
                            ImagePoint p = map.Project(c+0.20+0.60*x/9,r+0.20+0.60*y/9);
                            if (!BoardLocator.Inside(frame,p)) continue;
                            int best = Classify(frame.Sample(p),colors,settings,out _);
                            if (best >= 0) votes[best]++;
                        }
                    int selected = 0;
                    for (int i = 1; i < 4; i++) if (votes[i] > votes[selected]) selected = i;
                    double confidence = votes[selected]/100.0;
                    // Keep wide coverage evidence: central sampling must not hide another
                    // brick color, unknown color, or a mostly uncovered physical cell.
                    int support = 0,foreign = 0,unknown = 0;
                    for (int y = 0; y < 10; y++)
                        for (int x = 0; x < 10; x++)
                        {
                            ImagePoint p = map.Project(c+0.08+0.84*x/9,r+0.08+0.84*y/9);
                            if (!BoardLocator.Inside(frame,p)) continue;
                            int color = Classify(frame.Sample(p),colors,settings,out bool informative);
                            if (color == selected) support++;
                            else if (color > 0) foreign++;
                            else if (color < 0 && informative) unknown++;
                        }
                    double allowedOutliers = 100*(1-settings.MinimumCellAgreement)+1e-9;
                    if (confidence < settings.MinimumCellAgreement || support/100.0 < settings.MinimumCellCoverage ||
                        foreign > allowedOutliers || unknown > allowedOutliers)
                    {
                        cells[r*10+c] = new PhysicalCellObservation(c,r,PhysicalCellState.Uncertain,confidence:confidence);
                        issues.Add(new Issue("CELL_UNCERTAIN",$"实体格 ({c},{r}) 颜色不明确或混杂；请调整摆放、光照后重拍。",
                            new GridPoint(c,r),frame.FrameId)); continue;
                    }
                    if (selected == 0)
                    { cells[r*10+c] = new PhysicalCellObservation(c,r,PhysicalCellState.Empty,confidence:confidence); continue; }
                    string brickId = $"brick-{r}-{c}";
                    cells[r*10+c] = new PhysicalCellObservation(c,r,PhysicalCellState.Occupied,brickId,confidence);
                    var region = new[] { map.Project(c,r),map.Project(c+1,r),map.Project(c+1,r+1),map.Project(c,r+1) };
                    bricks.Add(new BrickObservation(brickId,c,r,calibration.ColorIds[selected-1],confidence,
                        new[] { new ImageEvidence(frame.FrameId,map.Project(c+0.5,r+0.5),region) }));
                }
            return new ScanResult(scanId,issues.Count == 0 ? ResultStatus.Success : ResultStatus.Rejected,
                cells,bricks.ToArray(),issues.ToArray(),board);
        }

        public static bool CalibrationValid(ColorCalibration calibration, PhotoScanSettings settings)
        {
            if (calibration?.BrickColors == null || calibration.BrickColors.Length != 3 ||
                calibration.ColorIds == null || calibration.ColorIds.Length != 3) return false;
            var ids = new HashSet<string>();
            foreach (string id in calibration.ColorIds) if (string.IsNullOrWhiteSpace(id) || !ids.Add(id)) return false;
            var colors = new[] { calibration.Paper,calibration.BrickColors[0],calibration.BrickColors[1],calibration.BrickColors[2] };
            foreach (Rgb24 color in colors)
                if (NormalizedLab.From(color).Brightness < settings.MinimumColorBrightness) return false;
            for (int i = 0; i < 4; i++)
                for (int j = i+1; j < 4; j++)
                    if (ColorDistance(colors[i],colors[j]) < settings.MinimumCalibrationSeparation) return false;
            return true;
        }

        public static double ColorDistance(Rgb24 a, Rgb24 b) =>
            NormalizedLab.Distance(NormalizedLab.From(a),NormalizedLab.From(b));
        private static int Classify(Rgb24 rgb,NormalizedLab[] colors,PhotoScanSettings settings,out bool informative)
        {
            NormalizedLab sample = NormalizedLab.From(rgb);
            informative = sample.Brightness >= settings.MinimumColorBrightness;
            if (!informative) return -1;
            int best = -1; double first = double.MaxValue,second = double.MaxValue;
            for (int color = 0; color < colors.Length; color++)
            {
                double distance = NormalizedLab.Distance(sample,colors[color]);
                if (distance < first) { second=first; first=distance; best=color; }
                else if (distance < second) second=distance;
            }
            return first <= settings.MaximumColorDistance && second-first >= settings.MinimumColorMargin ? best : -1;
        }
        internal static bool SettingsValid(PhotoScanSettings s) => s.MaximumDetectionSide >= 300 &&
            s.MaximumDetectionSide <= 2000 && s.MaximumColorDistance > 0 && s.MaximumColorDistance < 1 &&
            s.MinimumColorMargin > 0 && s.MinimumColorMargin < 1 && s.MinimumCellAgreement >= 0.5 &&
            s.MinimumCellAgreement <= 1 && s.MinimumCalibrationSeparation > 0 && s.MinimumCalibrationSeparation < 1 &&
            s.MinimumCellPixels >= 10 && s.MaximumMarkerFitError > 0 && s.MaximumMarkerFitError < 0.5 &&
            s.MinimumColorBrightness > 0 && s.MinimumColorBrightness < 1 &&
            s.MinimumCellCoverage >= 0.5 && s.MinimumCellCoverage <= s.MinimumCellAgreement;
        private static ScanResult Rejected(string scanId,string code,string message) =>
            new ScanResult(scanId,ResultStatus.Rejected,Array.Empty<PhysicalCellObservation>(),
                Array.Empty<BrickObservation>(),new[] { new Issue(code,message) });
    }
}
