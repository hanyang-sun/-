"""Preserve the existing GUI layout while extracting platform and workflow code."""
from pathlib import Path
root=Path(__file__).resolve().parents[2]
path=root/'Assets/Scripts/Presentation/PhotoScanPanel.cs'
source=path.read_text(encoding='utf-8-sig')
gui=source[source.index('        private void OnGUI()'):source.index('        private Color32 SamplePatch')]
gui=gui.replace('StartCoroutine(StartCamera())','StartCamera()').replace('scan = null;','analysis.Invalidate();')
old='PlayerPrefs.SetString(ProfileKey,JsonUtility.ToJson(new CalibrationData { colors=(Color32[])samples.Clone() }));\n                PlayerPrefs.Save();'
assert old in gui
gui=gui.replace(old,'profiles.SaveCalibration(samples);')
gui=gui.replace('PlayerPrefs.DeleteKey(ProfileKey); PlayerPrefs.Save();','profiles.ResetCalibration();')
gui=gui.replace('demo.ApplyPhotoScan(scan,Calibration().CreateRules())','levels.TryLoad(analysis.ConfirmLayout())')
draw=source[source.index('        private void DrawCamera('):source.index('        private void OnDestroy()')]
header='''using System;
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
        public bool IsOpen { get; private set; }

        public void Initialize(LevelSession owner)
        {
            levels=owner; analysis=new PhotoScanSession(PhotoProfileStore.ReadSettings());
            profiles=new PhotoProfileStore(); reports=new ScanReportStore(Application.persistentDataPath);
            cameraCapture=gameObject.AddComponent<CameraCapture>(); cameraCapture.StatusChanged+=CameraStatus;
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
                "First choose Color setup, then sample paper and all three brick colors from the photo.";
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
                    $"Found {scan.Bricks.Length} bricks. Verify each cell below before generating the level.":
                    "Scan rejected. Check the marked cells or paper markers, then retake the photo.";
                try { reports.Save(new PhotoScanRecord { Frame=frame,Result=scan,Settings=analysis.Settings,
                    Calibration=samples,Imported=imported,ProcessingMilliseconds=analysis.ProcessingMilliseconds }); }
                catch (Exception e) { message+=" Scan record could not be saved: "+e.Message; }
            }
            catch (Exception e) { if (this!=null) message="Recognition failed: "+e.Message; }
        }
'''
footer='''        private Color32 SamplePatch(int x,int y)
        { Rgb24 color=analysis.SampleColor(x,y); return new Color32(color.R,color.G,color.B,255); }
'''+draw+'''        private void OnDestroy()
        {
            analysis?.Invalidate();
            if (cameraCapture!=null) { cameraCapture.StatusChanged-=CameraStatus; cameraCapture.Stop(); }
            if (photo!=null) Destroy(photo);
        }
    }
}
'''
path.write_text(header+gui+footer,encoding='utf-8')
print('Photo panel now delegates recognition, input, camera and persistence.')
