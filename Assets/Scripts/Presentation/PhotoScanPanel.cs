using System;
using BrickDefense.Core;
using BrickDefense.Infrastructure;
using BrickDefense.Presentation;
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
        private SoftStrategyTheme theme;
        private Vector2 pageScroll;
        private Rect shownPhoto;
        private float uiScale;

        private void OnGUI()
        {
            if (!IsOpen) return;
            theme??=new SoftStrategyTheme();
            Matrix4x4 oldMatrix=GUI.matrix; int oldDepth=GUI.depth;
            Color oldColor=GUI.color; bool oldEnabled=GUI.enabled;
            GUISkin oldSkin=GUI.skin;
            bool narrow=Screen.width/(float)Screen.height<1.1f;
            uiScale=Mathf.Min(1.5f,Mathf.Min(Screen.width/(narrow?560f:1000f),Screen.height/(narrow?800f:820f)));
            float width=Screen.width/uiScale,height=Screen.height/uiScale;
            GUI.matrix=Matrix4x4.Scale(new Vector3(uiScale,uiScale,1)); GUI.depth=-20; GUI.color=Color.white;
            try
            {
                GUI.skin=theme.ScrollSkin;
                // Opaque across the whole viewport, including gaps and rounded card corners.
                SoftStrategyTheme.Solid(new Rect(0,0,width,height),SoftStrategyTheme.Background);
                float bodyWidth=Mathf.Min(width-44,1320),left=(width-bodyWidth)/2;
                GUI.Label(new Rect(left,22,360,16),"BRING YOUR BUILD INTO THE WORLD",theme.Eyebrow);
                GUI.Label(new Rect(left,44,360,32),"Scan your bricks",theme.Title);
                GUI.enabled=!busy;
                if (theme.Button(new Rect(left+bodyWidth-126,32,126,38),"Back to map")) Close();
                float x=left,slot=narrow?(bodyWidth-24)/4:112;
                if (theme.Button(new Rect(x,96,narrow?slot:144,36),"Import photo")) Import();
                x+=narrow?slot+8:152;
                GUI.enabled=!busy && !cameraStarting;
                if (theme.Button(new Rect(x,96,slot,36),"Camera")) StartCamera();
                x+=slot+8;
                GUI.enabled=!busy && webcam!=null && webcam.width>16;
                if (theme.Button(new Rect(x,96,slot,36),"Take photo")) CapturePhoto();
                x+=slot+8; GUI.enabled=!busy;
                if (theme.Button(new Rect(x,96,slot,36),"Stop camera")) StopCamera();
                float modeY=narrow?144:96,modeX=narrow?left:left+bodyWidth-354,modeWidth=narrow?bodyWidth:354;
                if (theme.Button(new Rect(modeX,modeY,(modeWidth-8)/2,36),"Board paper",selected:!plain)) SetInputMode(PhotoInputMode.MarkedBoard);
                if (theme.Button(new Rect(modeX+(modeWidth+8)/2,modeY,(modeWidth-8)/2,36),"Plain background",selected:plain)) SetInputMode(PhotoInputMode.PlainBackground);
                float guideY=narrow?190:144;
                GUI.enabled=true;
                GUI.Label(new Rect(left,guideY,bodyWidth,30),plain?
                    "Single layer, equal cubes, near-overhead. Keep every brick and one isolated cube in view.":
                    "Use the supplied 10 x 10 paper. Keep it flat, with all four markers visible.",theme.Small);
                float bodyY=guideY+36;
#if !UNITY_EDITOR
                GUI.enabled=!busy;
                inputPath=GUI.TextField(new Rect(left,bodyY,bodyWidth,34),inputPath,theme.Field);
                bodyY+=46;
#endif
                GUI.enabled=true;
                if (narrow)
                {
                    float contentWidth=bodyWidth-20;
                    pageScroll=GUI.BeginScrollView(new Rect(left,bodyY,bodyWidth,height-bodyY-18),pageScroll,
                        new Rect(0,0,contentWidth,1190));
                    DrawPicture(new Rect(0,0,contentWidth,450));
                    DrawCalibration(new Rect(0,466,contentWidth,332));
                    DrawResult(new Rect(0,814,contentWidth,360));
                    GUI.EndScrollView();
                }
                else
                {
                    float bodyHeight=height-bodyY-22,rightWidth=Mathf.Clamp(bodyWidth*0.35f,326,408);
                    float rightX=left+bodyWidth-rightWidth;
                    DrawPicture(new Rect(left,bodyY,bodyWidth-rightWidth-18,bodyHeight));
                    DrawCalibration(new Rect(rightX,bodyY,rightWidth,332));
                    DrawResult(new Rect(rightX,bodyY+348,rightWidth,bodyHeight-348));
                }
            }
            finally { GUI.skin=oldSkin; GUI.color=oldColor; GUI.enabled=oldEnabled; GUI.matrix=oldMatrix; GUI.depth=oldDepth; }
        }

        private void DrawPicture(Rect area)
        {
            theme.Card(area);
            GUI.Label(new Rect(area.x+18,area.y+16,area.width-36,26),"01 / Your photograph",theme.Heading);
            Rect pictureArea=new Rect(area.x+16,area.y+54,area.width-32,area.height-148);
            SoftStrategyTheme.Round(pictureArea,new Color32(229,231,218,255),8);
            shownPhoto=default;
            if (webcam!=null && webcam.width>16) DrawCamera(pictureArea);
            else if (photo!=null)
            {
                Rect shown=Fit(pictureArea,photo.width,photo.height); shownPhoto=shown;
                GUI.color=Color.white;
                GUI.DrawTexture(shown,photo,ScaleMode.StretchToFill,false);
                DrawEvidence(shown);
                if (scan?.Board!=null && !plain)
                    foreach (ImagePoint p in scan.Board.Corners)
                        SoftStrategyTheme.Solid(new Rect(shown.x+(float)p.X/frame.Width*shown.width-4,
                            shown.y+(float)p.Y/frame.Height*shown.height-4,8,8),Color.cyan);
                if (!busy && activeSample>=0 && Event.current.type==EventType.MouseDown && Event.current.button==0 && shown.Contains(Event.current.mousePosition))
                {
                    int px=Mathf.Clamp((int)((Event.current.mousePosition.x-shown.x)/shown.width*frame.Width),0,frame.Width-1);
                    int py=Mathf.Clamp((int)((Event.current.mousePosition.y-shown.y)/shown.height*frame.Height),0,frame.Height-1);
                    samples[activeSample]=SamplePatch(px,py); sampled[activeSample]=true; activeSample=-1; analysis.Invalidate();
                    message="Sample stored. Choose the next role, then save all four colors."; Event.current.Use();
                }
            }
            else GUI.Label(pictureArea,"A small build, a new map.\n\nImport a photograph or take one with the camera.",theme.Centered);
            bool rejected=scan?.Status==ResultStatus.Rejected;
            Rect note=new Rect(area.x+16,area.yMax-78,area.width-32,62);
            SoftStrategyTheme.Round(note,rejected?SoftStrategyTheme.PaleDanger:SoftStrategyTheme.PaleSage,8);
            GUI.Label(new Rect(note.x+12,note.y+10,note.width-24,note.height-16),
                activeSample>=0?"Click a flat, evenly lit "+(activeSample==0 && plain?"background":SampleNames[activeSample])+" area in the photo.":message,theme.Small);
        }

        private void DrawCalibration(Rect area)
        {
            theme.Card(area);
            GUI.Label(new Rect(area.x+16,area.y+16,160,24),"02 / Role colors",theme.Heading);
            GUI.enabled=!busy;
            if (theme.Button(new Rect(area.xMax-128,area.y+12,112,32),calibrating?"Editing colors":"Color setup",selected:calibrating))
            { calibrating=true; analysis.Invalidate(); }
            GUI.Label(new Rect(area.x+16,area.y+45,area.width-32,18),ReadyCalibration()?"Saved palette / roles chosen by you":"Sample the background and three roles",theme.Small);
            for (int i=0;i<4;i++)
            {
                GUI.enabled=!busy && calibrating && frame!=null && webcam==null;
                string sampleName=i==0 && plain?"Background":SampleNames[i];
                if (theme.Button(new Rect(area.x+16,area.y+74+i*38,area.width-86,30),
                    (activeSample==i?"Pick: ":"Sample ")+sampleName,selected:activeSample==i)) activeSample=i;
                Rect swatch=new Rect(area.xMax-54,area.y+74+i*38,38,30);
                SoftStrategyTheme.Round(swatch,SoftStrategyTheme.Background,6);
                if (sampled[i]) SoftStrategyTheme.Solid(new Rect(swatch.x+3,swatch.y+3,swatch.width-6,swatch.height-6),samples[i]);
                else GUI.Label(swatch,"?",theme.Centered);
            }
            GUI.enabled=!busy && calibrating && ReadyCalibration();
            float half=(area.width-40)/2;
            if (theme.Button(new Rect(area.x+16,area.y+236,half,34),"Save colors",true))
            { profiles.SaveCalibration(samples); calibrating=false; activeSample=-1; message="Colors saved for this set of bricks. Analyze the photo."; }
            GUI.enabled=!busy;
            if (theme.Button(new Rect(area.x+24+half,area.y+236,half,34),"Reset colors"))
            { Array.Clear(sampled,0,4); profiles.ResetCalibration(); analysis.Invalidate(); calibrating=true; activeSample=-1; }
            GUI.enabled=true;
            GUI.Label(new Rect(area.x+16,area.y+284,area.width-32,38),
                "Sample top faces, away from shadows. Each mode keeps its own palette; photos retain their real colors.",theme.Small);
        }

        private void DrawResult(Rect area)
        {
            theme.Card(area);
            GUI.Label(new Rect(area.x+16,area.y+14,area.width-32,26),"03 / Check the map",theme.Heading);
            bool success=scan?.Status==ResultStatus.Success;
            theme.Badge(new Rect(area.x+16,area.y+44,144,22),busy?"Analyzing...":scan==null?"Awaiting analysis":success?"Check before using":"Needs attention",scan==null || success);
            float footerY=area.yMax-50;
            if (scan?.PhysicalCells.Length==100)
            {
                float side=scan.Issues.Length>0?Mathf.Min(112,Mathf.Max(80,area.height-204)):
                    Mathf.Min(144,Mathf.Max(80,area.height-158));
                DrawMap(new Rect(area.x+16,area.y+78,side,side));
                float textX=area.x+side+30,textWidth=area.width-side-46;
                GUI.Label(new Rect(textX,area.y+80,textWidth,90),$"{scan.Bricks.Length} bricks\n\nK  Core\nW  Wall\nT  Tower",theme.Small);
                if (scan.Issues.Length==0) GUI.Label(new Rect(textX,area.y+174,textWidth,40),plain?"Relative grid\nNo paper origin":"Fixed paper grid\n10 x 10 cells",theme.Small);
                if (scan.Issues.Length>0)
                {
                    float issueY=area.y+Mathf.Max(174,side+92);
                    DrawIssues(new Rect(area.x+16,issueY,area.width-32,footerY-issueY-8));
                }
                else if (area.height>=314) GUI.Label(new Rect(area.x+16,area.y+side+90,area.width-32,footerY-area.y-side-100),
                    "Check count, roles and positions. If they differ, retake the photo.",theme.Small);
            }
            else if (scan?.Issues.Length>0)
                DrawIssues(new Rect(area.x+16,area.y+78,area.width-32,footerY-area.y-90));
            else GUI.Label(new Rect(area.x+16,area.y+78,area.width-32,footerY-area.y-90),
                "Your 10 x 10 map appears here after analysis.\n\nCalibrate once, analyze, then check each brick.",theme.Centered);
            bool hasCore=scan!=null && Array.Exists(scan.Bricks,b=>b.ColorId=="physical-color-0");
            if (success && !hasCore) GUI.Label(new Rect(area.x+16,footerY-30,area.width-32,24),"No core. Add one and take another photo.",theme.Small);
            GUI.enabled=!busy && frame!=null && webcam==null && ReadyCalibration() && !calibrating;
            if (theme.Button(new Rect(area.x+16,footerY,118,34),busy?"Analyzing...":"Analyze photo")) Analyze();
            GUI.enabled=!busy && success && hasCore && !calibrating && webcam==null;
            if (theme.Button(new Rect(area.x+144,footerY,area.width-160,34),"Use this map",true))
            { if (levels.TryLoad(analysis.ConfirmLayout())) Close(); }
            GUI.enabled=true;
        }

        private void DrawIssues(Rect area)
        {
            issueScroll=GUI.BeginScrollView(area,issueScroll,new Rect(0,0,area.width-20,scan.Issues.Length*72));
            for (int i=0;i<scan.Issues.Length;i++)
            {
                GUI.Label(new Rect(0,i*72,area.width-26,18),scan.Issues[i].Code,theme.Eyebrow);
                GUI.Label(new Rect(0,i*72+22,area.width-26,48),scan.Issues[i].Message,theme.Small);
            }
            GUI.EndScrollView();
        }

        private void DrawMap(Rect area)
        {
            float step=area.width/10;
            string[] ids=Calibration().ColorIds;
            foreach (PhysicalCellObservation cell in scan.PhysicalCells)
            {
                string symbol="."; Color color=new Color32(230,233,220,255);
                if (cell.State==PhysicalCellState.Uncertain) { symbol="?"; color=SoftStrategyTheme.Danger; }
                else if (cell.State==PhysicalCellState.Occupied)
                {
                    var brick=Array.Find(scan.Bricks,b=>b.BrickId==cell.BrickId);
                    int index=brick==null?-1:Array.IndexOf(ids,brick.ColorId);
                    symbol=index==0?"K":index==1?"W":index==2?"T":"?";
                    color=index>=0?samples[index+1]:SoftStrategyTheme.Danger;
                }
                var tile=new Rect(area.x+cell.C*step,area.y+cell.R*step,step-1,step-1);
                SoftStrategyTheme.Solid(tile,color);
                GUI.Label(tile,symbol,0.2126f*color.r+0.7152f*color.g+0.0722f*color.b>0.45f?theme.DarkCell:theme.LightCell);
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
            Rect guide=new Rect(shown.center.x-side/2,shown.center.y-side/2,side,side);
            SoftStrategyTheme.Solid(new Rect(guide.x,guide.y,guide.width,2),Color.white);
            SoftStrategyTheme.Solid(new Rect(guide.x,guide.yMax-2,guide.width,2),Color.white);
            SoftStrategyTheme.Solid(new Rect(guide.x,guide.y,2,guide.height),Color.white);
            SoftStrategyTheme.Solid(new Rect(guide.xMax-2,guide.y,2,guide.height),Color.white);
        }
        private static Rect Fit(Rect area,int width,int height)
        { float factor = Mathf.Min(area.width/width,area.height/height); return new Rect(area.center.x-width*factor/2,area.center.y-height*factor/2,width*factor,height*factor); }
        private void OnDestroy()
        {
            analysis?.Invalidate();
            if (cameraCapture!=null) { cameraCapture.StatusChanged-=CameraStatus; cameraCapture.Stop(); }
            if (photo!=null) Destroy(photo);
            theme?.Dispose();
        }
    }
}
