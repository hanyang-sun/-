using BrickDefense.Core;
using BrickDefense.Demo;
using BrickDefense.Workflow;
using UnityEngine;

namespace BrickDefense.Presentation
{
    public sealed class DemoUI : MonoBehaviour
    {
        private LevelSession session;
        private LevelView view;
        private PhotoScanPanel photoPanel;
        private SoftStrategyTheme theme;
        private int orbitControl;
        private int suppressMouseUntil=-1;
        private Vector2 lastOrbitPointer;
        private BoardTouchControls touchControls;
        public void Initialize(LevelSession session,LevelView view,PhotoScanPanel photoPanel)
        {
            this.session=session; this.view=view; this.photoPanel=photoPanel;
            touchControls=new BoardTouchControls(view.RotateView,view.ZoomView,SelectSpawn,
                p=>new BoardUiLayout(Screen.width,Screen.height).Contains(p));
        }

        private bool SuppressMapMouse=>Input.touchCount>0 || Time.frameCount<=suppressMouseUntil;

        private void OnGUI()
        {
            if (session?.Current==null || photoPanel.IsOpen) { CancelOrbit(); touchControls?.Cancel(); return; }
            int orbitId=GUIUtility.GetControlID("BrickDefense.BoardOrbit".GetHashCode(),FocusType.Passive);
            theme??=new SoftStrategyTheme();
            Matrix4x4 oldMatrix=GUI.matrix; Color oldColor=GUI.color; bool oldEnabled=GUI.enabled;
            Vector2 screenMouse=Event.current.mousePosition;
            var layout=new BoardUiLayout(Screen.width,Screen.height);
            bool narrow=layout.Narrow;
            float scale=layout.Scale,width=layout.Width,height=layout.Height;
            GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1)); GUI.color=Color.white; GUI.enabled=true;
            try
            {
                LevelContext context=session.Current; LevelResult level=context.Result;
                bool valid=level.Status==ResultStatus.Success;
                Rect titleArea=layout.Title,statusArea=layout.Status,viewArea=layout.View,dock=layout.Dock,legend=layout.Legend;
                bool overUI=layout.Contains(screenMouse);
                if(Event.current.type==EventType.ScrollWheel && !overUI && !SuppressMapMouse && GUIUtility.hotControl==0)
                { view.ZoomView(-Event.current.delta.y/3f); Event.current.Use(); }
                HandleOrbit(Event.current,orbitId,screenMouse,overUI);
                theme.Card(titleArea);
                GUI.Label(new Rect(titleArea.x+18,32,240,14),"A LITTLE WORLD, BUILT BY YOU",theme.Eyebrow);
                GUI.Label(new Rect(titleArea.x+18,50,240,30),"Brick Defense",theme.Title);
                theme.Card(statusArea);
                theme.Badge(new Rect(statusArea.x+14,30,valid?100:126,22),valid?"Route ready":"Choose a new start",valid);
                GUI.Label(new Rect(statusArea.x+14,59,236,21),valid?
                    $"{level.Entities.Length} bricks  /  {level.Route.Length-1} steps  /  start {level.Spawn}":
                    level.Issues[0].Code=="NO_ROUTE"?"Core enclosed. Open a path and rescan.":"This start is occupied. Pick an empty tile.",theme.Small);
                theme.Card(viewArea);
                GUI.Label(new Rect(viewArea.x+14,viewArea.y+12,236,16),
                    $"{(Application.isMobilePlatform?"DRAG & PINCH":"RIGHT-DRAG & SCROLL")} / {view.ZoomFactor*100:0}%",theme.Eyebrow);
                float viewSlot=(viewArea.width-44)/3;
                if (theme.Button(new Rect(viewArea.x+14,viewArea.y+34,viewSlot,32),"Left")) view.TurnView(-45);
                if (theme.Button(new Rect(viewArea.x+22+viewSlot,viewArea.y+34,viewSlot,32),"Right")) view.TurnView(45);
                if (theme.Button(new Rect(viewArea.x+30+viewSlot*2,viewArea.y+34,viewSlot,32),"Reset")) view.ResetView();
                float tiltSlot=(viewArea.width-46)/4;
                if (theme.Button(new Rect(viewArea.x+14,viewArea.y+74,tiltSlot,28),"Lower")) view.TurnView(0,-10);
                if (theme.Button(new Rect(viewArea.x+20+tiltSlot,viewArea.y+74,tiltSlot,28),"Higher")) view.TurnView(0,10);
                if (theme.Button(new Rect(viewArea.x+26+tiltSlot*2,viewArea.y+74,tiltSlot,28),"-")) view.ZoomView(-1);
                if (theme.Button(new Rect(viewArea.x+32+tiltSlot*3,viewArea.y+74,tiltSlot,28),"+")) view.ZoomView(1);
                theme.Card(legend);
                if (narrow)
                {
                    LegendItem(legend.x+18,legend.y+13,"Core",SoftStrategyTheme.Core);
                    LegendItem(legend.x+152,legend.y+13,"Wall",SoftStrategyTheme.Wall);
                    LegendItem(legend.x+286,legend.y+13,"Tower",SoftStrategyTheme.Tower);
                    LegendItem(legend.x+422,legend.y+13,"Route",SoftStrategyTheme.Route);
                }
                else
                {
                    GUI.Label(new Rect(legend.x+16,legend.y+14,120,18),"YOUR BUILD",theme.Eyebrow);
                    LegendItem(legend.x+16,legend.y+41,"Core",SoftStrategyTheme.Core);
                    LegendItem(legend.x+16,legend.y+67,"Wall",SoftStrategyTheme.Wall);
                    LegendItem(legend.x+16,legend.y+93,"Tower",SoftStrategyTheme.Tower);
                    LegendItem(legend.x+16,legend.y+119,"Route",SoftStrategyTheme.Route);
                }
                theme.Card(dock);
                float slot=(dock.width-44)/3;
                if (theme.Button(new Rect(dock.x+14,dock.y+14,slot,42),"Reference layout",
                    selected:context.Definition.Source==LayoutSource.Reference)) session.LoadExample(false);
                if (theme.Button(new Rect(dock.x+22+slot,dock.y+14,slot,42),"Enclosed core",
                    selected:context.Definition.Source==LayoutSource.Blocked)) session.LoadExample(true);
                if (theme.Button(new Rect(dock.x+30+slot*2,dock.y+14,slot,42),"Scan your bricks",true)) photoPanel.Open();
                if (!narrow) GUI.Label(new Rect((width-700)/2,height-19,700,17),Application.isMobilePlatform?
                    "Tap a tile to move the start. Drag to orbit. Pinch to zoom.":
                    "Right-drag to orbit. Scroll to zoom. Left-click a tile to move the start.",theme.Small);
                Event current=Event.current;
                if (current.type==EventType.MouseDown && current.button==0 && GUIUtility.hotControl==0 && !overUI && !SuppressMapMouse)
                {
                    SelectSpawn(screenMouse); current.Use();
                }
            }
            finally { GUI.matrix=oldMatrix; GUI.color=oldColor; GUI.enabled=oldEnabled; }
        }

        private void HandleOrbit(Event current,int id,Vector2 screenMouse,bool overUI)
        {
            if(SuppressMapMouse)
            {
                if(current.isMouse && current.button!=0) current.Use();
                return;
            }
            if (current.type==EventType.MouseDown && current.button==1 && !overUI && GUIUtility.hotControl==0)
            { orbitControl=id; GUIUtility.hotControl=id; lastOrbitPointer=screenMouse; current.Use(); }
            else if (orbitControl!=0 && GUIUtility.hotControl==orbitControl)
            {
                if (current.type==EventType.MouseDrag &&
                    (screenMouse.x<0 || screenMouse.y<0 || screenMouse.x>=Screen.width || screenMouse.y>=Screen.height))
                { CancelOrbit(); current.Use(); }
                else if (current.type==EventType.MouseDrag && current.button==1)
                { view.RotateView(screenMouse-lastOrbitPointer); lastOrbitPointer=screenMouse; current.Use(); }
                else if (current.rawType==EventType.MouseUp && current.button==1)
                { CancelOrbit(); current.Use(); }
                else if (current.type==EventType.MouseLeaveWindow || current.type==EventType.Ignore) CancelOrbit();
            }
            else if (overUI && current.button==1 &&
                (current.type==EventType.MouseDown || current.type==EventType.MouseDrag || current.type==EventType.MouseUp)) current.Use();
        }

        private void CancelOrbit()
        { if (orbitControl!=0 && GUIUtility.hotControl==orbitControl) GUIUtility.hotControl=0; orbitControl=0; }
        private void Update()
        {
            int count=Input.touchCount;
            HandleTouches(count,count>0?Input.GetTouch(0):default,count>1?Input.GetTouch(1):default);
            if (orbitControl==0) return;
            Vector3 pointer=Input.mousePosition;
            // A release outside the Game view may never arrive as an IMGUI MouseUp.
            if (!Input.GetMouseButton(1) || pointer.x<0 || pointer.y<0 || pointer.x>=Screen.width || pointer.y>=Screen.height)
                CancelOrbit();
        }
        private void HandleTouches(int count,Touch first,Touch second)
        {
            if(count>0) { suppressMouseUntil=Time.frameCount+1; CancelOrbit(); }
            if(session?.Current==null || photoPanel.IsOpen)
            {
                touchControls?.Cancel();
                if(count==0) touchControls?.Process(0,default,default,new Vector2(Screen.width,Screen.height));
                return;
            }
            touchControls.Process(count,first,second,new Vector2(Screen.width,Screen.height));
        }
        private void SelectSpawn(Vector2 screenPosition)
        { if(view.TryPickCell(screenPosition,out GridPoint cell)) session.SelectSpawn(cell); }
        private void OnApplicationFocus(bool focused) { if (!focused) { CancelOrbit(); touchControls?.Cancel(); } }
        private void OnApplicationPause(bool paused) { if(paused) { CancelOrbit(); touchControls?.Cancel(); } }
        private void OnDisable() { CancelOrbit(); touchControls?.Cancel(); }

        private void LegendItem(float x,float y,string name,Color color)
        {
            SoftStrategyTheme.Round(new Rect(x,y+2,14,14),color,4);
            GUI.Label(new Rect(x+24,y,94,20),name,theme.Body);
        }
    }
}
