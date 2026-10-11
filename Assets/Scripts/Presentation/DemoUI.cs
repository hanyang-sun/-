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
        public void Initialize(LevelSession session,LevelView view,PhotoScanPanel photoPanel)
        { this.session=session; this.view=view; this.photoPanel=photoPanel; }

        private void OnGUI()
        {
            if (session?.Current==null || photoPanel.IsOpen) return;
            theme??=new SoftStrategyTheme();
            Matrix4x4 oldMatrix=GUI.matrix; Color oldColor=GUI.color; bool oldEnabled=GUI.enabled;
            Vector2 screenMouse=Event.current.mousePosition;
            bool narrow=Screen.width/(float)Screen.height<0.95f;
            float scale=Mathf.Min(1.5f,Mathf.Min(Screen.width/(narrow?600f:900f),Screen.height/640f));
            float width=Screen.width/scale,height=Screen.height/scale;
            GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1)); GUI.color=Color.white; GUI.enabled=true;
            try
            {
                LevelContext context=session.Current; LevelResult level=context.Result;
                bool valid=level.Status==ResultStatus.Success;
                var titleArea=new Rect(22,20,narrow?260:286,74);
                var statusArea=new Rect(width-286,20,264,74);
                float dockWidth=narrow?width-44:700;
                var dock=new Rect((width-dockWidth)/2,height-96,dockWidth,74);
                var legend=new Rect(22,narrow?height-156:114,narrow?width-44:146,narrow?46:151);
                theme.Card(titleArea);
                GUI.Label(new Rect(titleArea.x+18,32,240,14),"A LITTLE WORLD, BUILT BY YOU",theme.Eyebrow);
                GUI.Label(new Rect(titleArea.x+18,50,240,30),"Brick Defense",theme.Title);
                theme.Card(statusArea);
                theme.Badge(new Rect(statusArea.x+14,30,valid?100:126,22),valid?"Route ready":"Choose a new start",valid);
                GUI.Label(new Rect(statusArea.x+14,59,236,21),valid?
                    $"{level.Entities.Length} bricks  /  {level.Route.Length-1} steps  /  start {level.Spawn}":
                    level.Issues[0].Code=="NO_ROUTE"?"Core enclosed. Open a path and rescan.":"This start is occupied. Pick an empty tile.",theme.Small);
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
                if (!narrow) GUI.Label(new Rect((width-340)/2,height-19,340,17),
                    "Click an empty floor tile to move the start.",theme.Small);
                Event current=Event.current;
                if (current.type==EventType.MouseDown && current.button==0 && !titleArea.Contains(current.mousePosition) &&
                    !statusArea.Contains(current.mousePosition) && !legend.Contains(current.mousePosition) && !dock.Contains(current.mousePosition))
                {
                    if (view.TryPickCell(screenMouse,out GridPoint cell)) { session.SelectSpawn(cell); current.Use(); }
                }
            }
            finally { GUI.matrix=oldMatrix; GUI.color=oldColor; GUI.enabled=oldEnabled; }
        }

        private void LegendItem(float x,float y,string name,Color color)
        {
            SoftStrategyTheme.Round(new Rect(x,y+2,14,14),color,4);
            GUI.Label(new Rect(x+24,y,94,20),name,theme.Body);
        }
    }
}
