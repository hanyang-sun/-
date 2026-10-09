using BrickDefense.Presentation;
using BrickDefense.Workflow;
using UnityEngine;

namespace BrickDefense.Demo
{
    // Scene composition only. The existing script GUID remains attached to the scene.
    public sealed class BrickDefenseDemo : MonoBehaviour
    {
        private LevelView view;
        public LevelSession Session { get; private set; }

        private void Start()
        {
            Application.targetFrameRate=60;
            Session=new LevelSession();
            view=gameObject.AddComponent<LevelView>();
            Session.Changed+=Display;
            var photoPanel=gameObject.AddComponent<PhotoScanPanel>();
            photoPanel.Initialize(Session);
            gameObject.AddComponent<DemoUI>().Initialize(Session,view,photoPanel);
            Session.LoadExample(false);
        }

        private void Display(LevelContext context) { view.Display(context.Result); }
        private void OnDestroy() { if (Session != null) Session.Changed-=Display; }
    }
}
