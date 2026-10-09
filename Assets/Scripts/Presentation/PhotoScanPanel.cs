using System;
using BrickDefense.Core;
using BrickDefense.Infrastructure;
using BrickDefense.Vision;
using BrickDefense.Workflow;
using UnityEngine;

namespace BrickDefense.Demo
{
    public sealed class PhotoScanPanel : MonoBehaviour
    {
        private static readonly string[] SampleNames={"Paper","Core color","Wall color","Tower color"};
        private readonly Color32[] samples=new Color32[4];
        private readonly bool[] sampled=new bool[4];
        private LevelSession levels;
        private PhotoScanSession analysis;
        private PhotoProfileStore profiles;
        private ScanReportStore reports;
        private CameraCapture cameraCapture;
        private ImportedPhoto imported;
        private Texture2D photo;
        private bool calibrating;
        private int activeSample=-1;
        private string inputPath="",message="Import a photo or use the camera. Keep all four board markers in view.";
        private Vector2 issueScroll;
        private ImageFrame frame=>analysis.Frame;
        private ScanResult scan=>analysis.Result;
        private bool busy=>analysis.IsBusy;
        private bool cameraStarting=>cameraCapture.IsStarting;
        private WebCamTexture webcam=>cameraCapture.Texture;
        private bool plain=>analysis.InputMode==PhotoInputMode.PlainBackground;
        public bool IsOpen { get; private set; }
        public PhotoInputMode InputMode=>analysis.InputMode;

        public void SetInputMode(PhotoInputMode mode)
        {
            if (mode==analysis.InputMode) return;
            analysis.SetInputMode(mode); StopCamera(); LoadProfile(); activeSample=-1; calibrating=false;
            message=plain?"No paper markers needed. Sample background and three role colors; layout is relative.":
                "Use the supplied paper with all four markers. This mode restores its fixed grid.";
        }

        public void Initialize(LevelSession owner)
        {
            levels=owner; analysis=new PhotoScanSession(PhotoProfileStore.ReadSettings());
            reports=new ScanReportStore(Application.persistentDataPath);
            cameraCapture=gameObject.AddComponent<CameraCapture>(); cameraCapture.StatusChanged+=CameraStatus;
            LoadProfile();
        }
        private void LoadProfile()
        {
            profiles=new PhotoProfileStore(plain?"BrickDefense.PhotoCalibration.plain.v1":"BrickDefense.PhotoCalibration.v1");
            Array.Clear(sampled,0,4); Array.Clear(samples,0,4);
            Color32[] stored=profiles.LoadCalibration();
            if (stored!=null) for (int i=0; i<4; i++) { samples[i]=stored[i]; sampled[i]=true; }
        }
        public void Open() { IsOpen=true; }
        private void Close() { IsOpen=false; StopCamera(); activeSample=-1; }
        private void CameraStatus(string value) { message=value; }
        private void Import()
        {
            string selected=PhotoFileSource.PickPath(inputPath);
            if (string.IsNullOrEmpty(selected)) return;
            inputPath=selected; LoadPhoto(inputPath);
        }
        public void LoadPhoto(string path)
        {
            if (busy) return;
            try { ImportedPhoto loaded=PhotoFileSource.Load(path); SetFrame(loaded.Frame); imported=loaded; }
            catch (Exception e) { message="Photo could not be loaded: "+e.Message; }
        }
        private void SetFrame(ImageFrame value)
        {
            StopCamera(); analysis.SetFrame(value); imported=null;
            if (photo!=null) Destroy(photo);
            photo=PhotoPixels.ToTexture(value);
            message=ReadyCalibration()?"Photo loaded. Analyze, check the map, then confirm.":
                "Choose Color setup, then sample the background and all three role colors from the photo.";
        }
        private void StartCamera() { analysis.Invalidate(); activeSample=-1; cameraCapture.Begin(); }
        private void CapturePhoto()
        {
            try { SetFrame(cameraCapture.Capture()); }
            catch (Exception e) { message="Capture failed: "+e.Message; }
        }
        private void StopCamera() { if (cameraCapture!=null) cameraCapture.Stop(); }
        private ColorCalibration Calibration()
        {
            Rgb24 ToRgb(Color32 c)=>new Rgb24(c.r,c.g,c.b);
            return new ColorCalibration(ToRgb(samples[0]),new[] {ToRgb(samples[1]),ToRgb(samples[2]),ToRgb(samples[3])},
                new[] {"physical-color-0","physical-color-1","physical-color-2"});
        }
        private bool ReadyCalibration()=>Array.TrueForAll(sampled,value=>value) && analysis.CalibrationReady(Calibration());
        private async void Analyze()
        {
            if (frame==null || busy || !ReadyCalibration()) return;
            activeSample=-1; calibrating=false; message="Analyzing photo...";
            try
            {
                await analysis.AnalyzeAsync(Calibration());
                if (this==null || scan==null) return;
                message=scan.Status==ResultStatus.Success?
                    $"Found {scan.Bricks.Length} bricks. "+(plain?"Relative layout: check the detection boxes and digital map.":"Verify each cell below before generating the level."):
                    "Scan rejected. Check the reasons below, adjust the photo or calibration, then analyze again.";
                try { reports.Save(new PhotoScanRecord { Frame=frame,Result=scan,Settings=analysis.Settings,
                    Calibration=samples,Imported=imported,ProcessingMilliseconds=analysis.ProcessingMilliseconds,InputMode=analysis.InputMode }); }
                catch (Exception e) { message+=" Scan record could not be saved: "+e.Message; }
            }
            catch (Exception e) { if (this!=null) message="Recognition failed: "+e.Message; }
        }
        private void OnGUI()
        {
            if (!IsOpen) return;
            Matrix4x4 previousMatrix = GUI.matrix; int previousDepth = GUI.depth;
            Color previousColor = GUI.color;
            float scale = Mathf.Min(Screen.width/1000f,Screen.height/880f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale,scale,1)); GUI.depth = -20;
            GUI.color = new Color32(8,10,15,255);
            GUI.DrawTexture(new Rect(0,0,Screen.width/scale,Screen.height/scale),Texture2D.whiteTexture,ScaleMode.StretchToFill,false);
            GUI.color = Color.white;
            GUI.Box(new Rect(0,0,Screen.width/scale,Screen.height/scale),"Photo scan");
            GUI.enabled = !busy;
            if (GUI.Button(new Rect(16,30,140,34),"Import PNG / JPG")) Import();
            GUI.enabled = !busy && !cameraStarting;
            if (GUI.Button(new Rect(166,30,105,34),"Camera")) StartCamera();
            GUI.enabled = !busy && webcam != null && webcam.width > 16;
            if (GUI.Button(new Rect(281,30,105,34),"Take photo")) CapturePhoto();
            GUI.enabled = !busy;
            if (GUI.Button(new Rect(396,30,105,34),"Stop camera")) StopCamera();
            int mode=GUI.Toolbar(new Rect(511,30,345,34),(int)analysis.InputMode,new[] {"Board paper","Plain background"});
            if (mode!=(int)analysis.InputMode) SetInputMode((PhotoInputMode)mode);
            if (GUI.Button(new Rect(866,30,118,34),"Back")) Close();
#if !UNITY_EDITOR
            inputPath = GUI.TextField(new Rect(16,72,605,26),inputPath);
            GUI.Label(new Rect(636,72,348,26),"Local PNG/JPG path (camera works without a file)");
#else
            GUI.Label(new Rect(16,72,960,26),plain?"Plain background: single-layer equal cubes, near-overhead view, one isolated cube visible.":
                "Use the supplied 10 x 10 paper. Keep it flat and all four markers visible.");
#endif
            Rect pictureArea = new Rect(16,110,605,405);
            GUI.Box(pictureArea,"");
            if (webcam != null && webcam.width > 16) DrawCamera(pictureArea);
            else if (photo != null)
            {
                Rect shown = Fit(pictureArea,photo.width,photo.height);
                GUI.DrawTexture(shown,photo,ScaleMode.StretchToFill);
                DrawEvidence(shown);
                if (scan?.Board != null)
                    foreach (ImagePoint p in scan.Board.Corners)
                    { GUI.color = Color.cyan; GUI.DrawTexture(new Rect(shown.x+(float)p.X/frame.Width*shown.width-4,
                        shown.y+(float)p.Y/frame.Height*shown.height-4,8,8),Texture2D.whiteTexture); GUI.color = Color.white; }
                if (!busy && activeSample >= 0 && Event.current.type == EventType.MouseDown && shown.Contains(Event.current.mousePosition))
                {
                    int x = Mathf.Clamp((int)((Event.current.mousePosition.x-shown.x)/shown.width*frame.Width),0,frame.Width-1);
                    int y = Mathf.Clamp((int)((Event.current.mousePosition.y-shown.y)/shown.height*frame.Height),0,frame.Height-1);
                    samples[activeSample] = SamplePatch(x,y); sampled[activeSample] = true; activeSample = -1; analysis.Invalidate();
                    message = "Sample stored. Choose the next color, then save colors when all four are ready."; Event.current.Use();
                }
            }
            else GUI.Label(new Rect(60,270,540,80),"Import a photograph,\nor start the camera and take a photo.");

            if (GUI.Button(new Rect(636,110,165,34),"Color setup")) { calibrating = true; analysis.Invalidate(); }
            GUI.Label(new Rect(811,110,175,34),ReadyCalibration() ? "Colors ready" : "Setup required");
            for (int i = 0; i < 4; i++)
            {
                GUI.enabled = !busy && calibrating && frame != null && webcam == null;
                string sampleName=i==0 && plain?"Background":SampleNames[i];
                if (GUI.Button(new Rect(636,155+i*44,220,34),(activeSample == i ? "Click photo: " : "Sample ")+sampleName)) activeSample = i;
                GUI.color = sampled[i] ? samples[i] : Color.gray;
                GUI.DrawTexture(new Rect(868,155+i*44,38,34),Texture2D.whiteTexture); GUI.color = Color.white;
            }
            GUI.enabled = !busy && calibrating && ReadyCalibration();
            if (GUI.Button(new Rect(636,339,165,34),"Save colors"))
            { profiles.SaveCalibration(samples); calibrating = false; activeSample = -1; message = "Colors saved for this set of bricks. Analyze the photo."; }
            GUI.enabled = !busy;
            if (GUI.Button(new Rect(811,339,173,34),"Reset colors"))
            { Array.Clear(sampled,0,4); profiles.ResetCalibration(); analysis.Invalidate(); calibrating = true; }
            GUI.Label(new Rect(636,386,348,105),plain?
                "Sample a clean background patch, then the top face of a cube for Core, Wall and Tower.\nEach mode keeps its own colors.\nUnknown objects or unclear shapes cause rejection.":
                "For initial color setup, click a flat, well-lit area of the paper and each brick color.\nColors are kept for later scans.\nMixed or unknown cell colors cause rejection.");
            GUI.enabled = !busy && frame != null && webcam == null && ReadyCalibration() && !calibrating;
            if (GUI.Button(new Rect(636,481,348,34),busy ? "Analyzing..." : "Analyze photo")) Analyze();
            GUI.enabled = true;
            GUI.Label(new Rect(16,525,968,48),message);
            DrawMap(new Rect(16,580,300,300));
            GUI.Label(new Rect(336,580,648,44),"K = core, W = wall, T = tower, . = empty, ? = uncertain.\n"+
                (plain?"Relative grid inferred from cube size; no physical board origin. Check boxes and positions.":
                    "Verify the fixed digital map. If incorrect, retake the photo; cells cannot be edited."));
            if (scan != null && scan.Issues.Length > 0)
            {
                issueScroll = GUI.BeginScrollView(new Rect(336,635,648,185),issueScroll,new Rect(0,0,620,scan.Issues.Length*48));
                for (int i = 0; i < scan.Issues.Length; i++) GUI.Label(new Rect(0,i*48,615,48),scan.Issues[i].Code+" — "+scan.Issues[i].Message);
                GUI.EndScrollView();
            }
            bool hasCore = scan != null && Array.Exists(scan.Bricks,b => b.ColorId == "physical-color-0");
            GUI.enabled = !busy && scan?.Status == ResultStatus.Success && hasCore && !calibrating && webcam == null;
            if (GUI.Button(new Rect(336,833,648,38),"Map is correct — generate level"))
            { if (levels.TryLoad(analysis.ConfirmLayout())) Close(); }
            GUI.enabled = true;
            if (scan?.Status == ResultStatus.Success && !hasCore) GUI.Label(new Rect(336,780,620,40),"No core found. Add a core brick and take another photo.");
            GUI.color = previousColor; GUI.matrix = previousMatrix; GUI.depth = previousDepth;
        }

        private void DrawMap(Rect area)
        {
            if (scan == null || scan.PhysicalCells.Length != 100)
            { GUI.Box(area,scan?.Status==ResultStatus.Rejected?"Scan rejected\nCheck red regions in the photo":"Digital map appears after analysis"); return; }
            foreach (PhysicalCellObservation cell in scan.PhysicalCells)
            {
                string symbol = "."; Color color = new Color(0.7f,0.7f,0.7f);
                if (cell.State == PhysicalCellState.Uncertain) { symbol = "?"; color = Color.red; }
                else if (cell.State == PhysicalCellState.Occupied)
                {
                    var brick = Array.Find(scan.Bricks,b => b.BrickId == cell.BrickId);
                    int i = Array.IndexOf(Calibration().ColorIds,brick.ColorId);
                    symbol = i == 0 ? "K" : i == 1 ? "W" : "T"; color = samples[i+1];
                }
                GUI.color = color; GUI.Box(new Rect(area.x+cell.C*30,area.y+cell.R*30,29,29),symbol); GUI.color = Color.white;
            }
        }

        private Color32 SamplePatch(int x,int y)
        { Rgb24 color=analysis.SampleColor(x,y); return new Color32(color.R,color.G,color.B,255); }
        private void DrawEvidence(Rect shown)
        {
            if (!plain || scan==null) return;
            foreach (BrickObservation brick in scan.Bricks)
            {
                if (brick.Evidence.Length==0) continue;
                ImagePoint[] region=brick.Evidence[0].Region; if (region.Length!=4) continue;
                int index=Array.IndexOf(Calibration().ColorIds,brick.ColorId);
                DrawRegion(shown,region,index>=0?samples[index+1]:Color.yellow);
            }
            foreach (Issue issue in scan.Issues)
                if (issue.FrameId==frame.FrameId && issue.ImageRegion.Length==4) DrawRegion(shown,issue.ImageRegion,Color.red);
        }
        private void DrawRegion(Rect shown,ImagePoint[] region,Color color)
        {
            Color previous=GUI.color; GUI.color=color;
            float left=shown.x+(float)region[0].X/frame.Width*shown.width,top=shown.y+(float)region[0].Y/frame.Height*shown.height;
            float right=shown.x+(float)region[2].X/frame.Width*shown.width,bottom=shown.y+(float)region[2].Y/frame.Height*shown.height;
            GUI.DrawTexture(new Rect(left,top,right-left,2),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(left,bottom-2,right-left,2),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(left,top,2,bottom-top),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(right-2,top,2,bottom-top),Texture2D.whiteTexture);
            GUI.color=previous;
        }
        private void DrawCamera(Rect area)
        {
            int angle = webcam.videoRotationAngle;
            bool swapped = angle == 90 || angle == 270;
            Rect shown = Fit(area,swapped ? webcam.height : webcam.width,swapped ? webcam.width : webcam.height);
            Rect raw = swapped ? new Rect(shown.center.x-shown.height/2,shown.center.y-shown.width/2,shown.height,shown.width) : shown;
            Matrix4x4 old = GUI.matrix; GUIUtility.RotateAroundPivot(angle,shown.center);
            GUI.DrawTextureWithTexCoords(raw,webcam,new Rect(0,webcam.videoVerticallyMirrored ? 1 : 0,1,webcam.videoVerticallyMirrored ? -1 : 1));
            GUI.matrix = old;
            float side = Mathf.Min(shown.width,shown.height)*0.9f;
            GUI.Box(new Rect(shown.center.x-side/2,shown.center.y-side/2,side,side),"Entire paper inside guide");
        }
        private static Rect Fit(Rect area,int width,int height)
        { float factor = Mathf.Min(area.width/width,area.height/height); return new Rect(area.center.x-width*factor/2,area.center.y-height*factor/2,width*factor,height*factor); }
        private void OnDestroy()
        {
            analysis?.Invalidate();
            if (cameraCapture!=null) { cameraCapture.StatusChanged-=CameraStatus; cameraCapture.Stop(); }
            if (photo!=null) Destroy(photo);
        }
    }
}
