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
        public void Initialize(LevelSession session,LevelView view,PhotoScanPanel photoPanel)
        { this.session=session; this.view=view; this.photoPanel=photoPanel; }

        private void OnGUI()
        {
            if (session?.Current==null || photoPanel.IsOpen) return;
            GUI.Box(new Rect(12,12,475,232),"Brick Defense | Level Prototype");
            GUI.Label(new Rect(28,42,440,24),"Click a floor cell to choose the enemy spawn.");
            GUI.Label(new Rect(28,65,440,24),"Cyan = core, gray = wall, orange = tower.");
            GUI.Label(new Rect(28,88,440,24),session.Current.LayoutText);
            GUI.Label(new Rect(28,113,440,34),session.Current.StatusText);
            if (GUI.Button(new Rect(28,154,180,30),"Reference layout")) session.LoadExample(false);
            if (GUI.Button(new Rect(220,154,180,30),"Blocked core")) session.LoadExample(true);
            if (GUI.Button(new Rect(28,194,372,32),"Photo scan / Camera")) photoPanel.Open();
            Event current=Event.current;
            if (current.type==EventType.MouseDown && current.button==0 && !new Rect(12,12,475,232).Contains(current.mousePosition))
            {
                if (view.TryPickCell(current.mousePosition,out GridPoint cell)) session.SelectSpawn(cell);
                current.Use();
            }
        }
    }
}
