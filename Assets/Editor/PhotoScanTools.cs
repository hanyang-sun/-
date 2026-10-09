using System;
using System.IO;
using System.Linq;
using BrickDefense.Core;
using BrickDefense.Demo;
using BrickDefense.Infrastructure;
using BrickDefense.Vision;
using UnityEditor;
using UnityEngine;

namespace BrickDefense.Editor
{
    public sealed class PhotoScanTools : EditorWindow
    {
        private double measuredBrickEdgeMm;
        private string feedback = "Measure your cube edge, then enter that value. Print the exported SVG at 100%, with no fit-to-page scaling.";

        [MenuItem("Brick Defense/Export Scan Board")]
        public static void Open() { GetWindow<PhotoScanTools>("Scan board"); }
        private void OnGUI()
        {
            measuredBrickEdgeMm = EditorGUILayout.DoubleField("Measured brick edge (mm)",measuredBrickEdgeMm);
            EditorGUILayout.HelpBox(feedback,MessageType.Info);
            if (GUILayout.Button("Export SVG"))
            {
                try
                {
                    string svg = ScanBoardTemplate.CreateSvg(measuredBrickEdgeMm);
                    string path = EditorUtility.SaveFilePanel("Export printable scan board",Application.dataPath,"scan-board","svg");
                    if (string.IsNullOrEmpty(path)) return;
                    File.WriteAllText(path,svg);
                    feedback = "Exported. Sheet width = 14 x brick edge. Verify a printed cell matches your brick before use.";
                }
                catch (Exception e) { feedback = e.Message; }
            }
        }

        [MenuItem("Brick Defense/Verify Photo Integration")]
        public static void VerifyPhotoIntegration()
        {
            try
            {
                string path = Path.Combine(Application.dataPath,"../Tests/Fixtures/synthetic-reference.png");
                var texture = new Texture2D(2,2,TextureFormat.RGBA32,false);
                if (!ImageConversion.LoadImage(texture,File.ReadAllBytes(path),false)) throw new Exception("PNG decode failed.");
                ImageFrame frame = PhotoPixels.FromUnity("unity-png",texture.GetPixels32(),texture.width,texture.height);
                DestroyImmediate(texture);
                var calibration = new ColorCalibration(new Rgb24(245,245,245),
                    new[] {new Rgb24(220,35,35),new Rgb24(35,80,220),new Rgb24(30,185,45)},new[] {"red","blue","green"});
                ScanResult scan = PhotoRecognizer.Recognize(new CaptureSet("unity-image-check",frame),calibration);
                if (scan.Status != ResultStatus.Success || scan.Bricks.Length != 9) throw new Exception("Unity decoded photo recognition failed.");
                LevelResult level = LevelBuilder.Build(GridMapper.Map(scan),calibration.CreateRules(),new GridPoint(1,18));
                if (level.Status != ResultStatus.Success || level.Route.Length-1 != 16) throw new Exception("Unity photo-to-level mapping failed.");
                // Orientation adapter must agree with marker-based paper orientation.
                var rotated = PhotoPixels.ToTexture(frame);
                ImageFrame turned = PhotoPixels.FromUnity("rotated",rotated.GetPixels32(),rotated.width,rotated.height,90);
                DestroyImmediate(rotated);
                ScanResult rotatedScan = PhotoRecognizer.Recognize(new CaptureSet("rotated",turned),calibration);
                if (rotatedScan.Status != ResultStatus.Success || rotatedScan.Bricks.Length != 9) throw new Exception("Unity camera rotation adapter failed.");
                VerifyGeneratedPhoto();
                Debug.Log("PHOTO_INTEGRATION_PASSED: Unity PNG decode, orientation, 9 bricks, 36 cells, 16-step route.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e)
            { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); else throw; }
        }

        private static void VerifyGeneratedPhoto()
        {
            string path = Path.Combine(Application.dataPath,"../Tests/Fixtures/generated-overhead-bricks-v1.png");
            var texture = new Texture2D(2,2,TextureFormat.RGBA32,false);
            ImageFrame frame;
            try
            {
                if (!ImageConversion.LoadImage(texture,File.ReadAllBytes(path),false)) throw new Exception("Generated PNG decode failed.");
                frame = PhotoPixels.FromUnity("unity-generated-png",texture.GetPixels32(),texture.width,texture.height);
            }
            finally { DestroyImmediate(texture); }
            // Exercise the actual panel's JSON settings loader, including new defaults.
            var settings = PhotoProfileStore.ReadSettings();
            if (Math.Abs(settings.MinimumColorBrightness-0.12) > 1e-9 || Math.Abs(settings.MinimumCellCoverage-0.65) > 1e-9)
                throw new Exception("Photo panel settings did not load.");
            var calibration = new ColorCalibration(MedianPatch(frame,720,170),
                new[] { MedianPatch(frame,808,391),MedianPatch(frame,487,385),MedianPatch(frame,650,636) },
                new[] { "red","blue","green" });
            var scan = PhotoRecognizer.Recognize(new CaptureSet("unity-generated-color-check",frame),calibration,settings);
            var expected = new[] { (3,2,"blue"),(4,2,"blue"),(5,2,"blue"),(7,2,"red"),(3,3,"blue"),
                (3,4,"blue"),(3,5,"blue"),(5,5,"green"),(7,7,"red") };
            if (scan.Status != ResultStatus.Success || scan.Bricks.Length != 9 ||
                scan.PhysicalCells.Count(c => c.State == PhysicalCellState.Empty) != 91 ||
                !expected.All(e => scan.Bricks.Any(b => b.C == e.Item1 && b.R == e.Item2 && b.ColorId == e.Item3)))
                throw new Exception("Generated photo cell/color regression: "+string.Join("; ",scan.Issues.Select(i => i.Message)));
            var level = LevelBuilder.Build(GridMapper.Map(scan),calibration.CreateRules(),new GridPoint(1,18));
            if (level.Status != ResultStatus.Success || level.Route.Length-1 != 16)
                throw new Exception("Generated photo-to-level regression failed.");
            Debug.Log("GENERATED_PHOTO_COLOR_PASSED: original PNG, panel settings, 6 blue / 2 red / 1 green, 91 empty, 16-step route.");
        }

        private static Rgb24 MedianPatch(ImageFrame frame,int x,int y)
        {
            var r = new byte[49]; var g = new byte[49]; var b = new byte[49]; int i = 0;
            for (int dy = -3; dy <= 3; dy++) for (int dx = -3; dx <= 3; dx++)
            { var p=frame.Pixels[(y+dy)*frame.Width+x+dx]; r[i]=p.R; g[i]=p.G; b[i++]=p.B; }
            Array.Sort(r);Array.Sort(g);Array.Sort(b); return new Rgb24(r[24],g[24],b[24]);
        }
    }
}
