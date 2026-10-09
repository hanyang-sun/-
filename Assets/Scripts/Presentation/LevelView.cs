using BrickDefense.Core;
using UnityEngine;

namespace BrickDefense.Presentation
{
    public sealed class LevelView : MonoBehaviour
    {
        private Camera sceneCamera;
        private Transform boardRoot,entityRoot,routeRoot;
        private Material lightTile,darkTile,coreMaterial,wallMaterial,towerMaterial,routeMaterial,rejectedMaterial;

        private void Awake()
        {
            sceneCamera=Camera.main;
            if (sceneCamera == null)
            {
                var cameraObject=new GameObject("Demo camera"); cameraObject.tag="MainCamera";
                cameraObject.transform.SetParent(transform); sceneCamera=cameraObject.AddComponent<Camera>();
            }
            sceneCamera.transform.position=new Vector3(0,28,-18); sceneCamera.transform.LookAt(Vector3.zero);
            sceneCamera.orthographic=true; sceneCamera.orthographicSize=17.5f;
            sceneCamera.backgroundColor=new Color(0.07f,0.10f,0.15f); sceneCamera.clearFlags=CameraClearFlags.SolidColor;
            var lightObject=new GameObject("Demo light"); lightObject.transform.SetParent(transform);
            var light=lightObject.AddComponent<Light>(); light.type=LightType.Directional; light.intensity=1.5f;
            lightObject.transform.rotation=Quaternion.Euler(45,-35,0);
            RenderSettings.ambientLight=new Color(0.55f,0.58f,0.64f);
            lightTile=MakeMaterial(new Color(0.20f,0.29f,0.36f)); darkTile=MakeMaterial(new Color(0.17f,0.25f,0.32f));
            coreMaterial=MakeMaterial(new Color(0.13f,0.83f,0.88f)); wallMaterial=MakeMaterial(new Color(0.62f,0.63f,0.78f));
            towerMaterial=MakeMaterial(new Color(1,0.61f,0.18f)); routeMaterial=MakeMaterial(new Color(0.87f,0.98f,0.29f));
            rejectedMaterial=MakeMaterial(new Color(0.98f,0.28f,0.30f));
            boardRoot=NewRoot("Navigation grid");
            for (int y=0; y<BoardSize.Navigation; y++)
                for (int x=0; x<BoardSize.Navigation; x++)
                {
                    Vector3 center=WorldForNavigation(new GridPoint(x,y));
                    var tile=GameObject.CreatePrimitive(PrimitiveType.Cube); tile.name=$"Cell {x},{y}";
                    tile.transform.SetParent(boardRoot); tile.transform.position=new Vector3(center.x,-0.06f,center.z);
                    tile.transform.localScale=new Vector3(0.96f,0.1f,0.96f);
                    tile.GetComponent<Renderer>().sharedMaterial=(x+y)%2==0?lightTile:darkTile;
                }
        }

        public void Display(LevelResult level)
        {
            ReplaceRoot(ref entityRoot,"Physical bricks"); ReplaceRoot(ref routeRoot,"Route preview");
            foreach (LevelEntity entity in level.Entities)
            {
                var brick=GameObject.CreatePrimitive(PrimitiveType.Cube); brick.name=$"{entity.Role} {entity.BrickId}";
                brick.transform.SetParent(entityRoot); brick.transform.position=new Vector3(2*entity.C-9f,0.66f,9f-2*entity.R);
                brick.transform.localScale=new Vector3(1.9f,1.3f,1.9f);
                brick.GetComponent<Renderer>().sharedMaterial=entity.Role==BrickRole.Core?coreMaterial:
                    entity.Role==BrickRole.Wall?wallMaterial:towerMaterial;
            }
            foreach (GridPoint point in level.Route)
                Marker($"Route {point.X},{point.Y}",point,0.10f,new Vector3(0.32f,0.06f,0.32f),routeMaterial);
            GridPoint spawn=level.Spawn;
            if (spawn.X>=0 && spawn.Y>=0 && spawn.X<BoardSize.Navigation && spawn.Y<BoardSize.Navigation)
                Marker("Selected spawn",spawn,1.45f,new Vector3(0.55f,0.12f,0.55f),
                    level.Status==ResultStatus.Success?routeMaterial:rejectedMaterial);
        }

        public bool TryPickCell(Vector2 guiPosition,out GridPoint cell)
        {
            cell=default;
            Ray ray=sceneCamera.ScreenPointToRay(new Vector3(guiPosition.x,Screen.height-guiPosition.y,0));
            var plane=new Plane(Vector3.up,Vector3.zero);
            if (!plane.Raycast(ray,out float distance)) return false;
            Vector3 hit=ray.GetPoint(distance); int x=Mathf.FloorToInt(hit.x+10),y=Mathf.FloorToInt(10-hit.z);
            if (x<0 || y<0 || x>=BoardSize.Navigation || y>=BoardSize.Navigation) return false;
            cell=new GridPoint(x,y); return true;
        }

        private void Marker(string name,GridPoint cell,float height,Vector3 scale,Material material)
        {
            var marker=GameObject.CreatePrimitive(PrimitiveType.Cylinder); marker.name=name;
            marker.transform.SetParent(routeRoot); marker.transform.position=WorldForNavigation(cell)+Vector3.up*height;
            marker.transform.localScale=scale; marker.GetComponent<Renderer>().sharedMaterial=material;
        }
        private Transform NewRoot(string name) { var root=new GameObject(name).transform; root.SetParent(transform); return root; }
        private void ReplaceRoot(ref Transform root,string name)
        { if (root!=null) { root.gameObject.SetActive(false); Destroy(root.gameObject); } root=NewRoot(name); }
        private static Material MakeMaterial(Color color) { var material=new Material(Shader.Find("Standard")); material.color=color; return material; }
        private static Vector3 WorldForNavigation(GridPoint point) => new Vector3(point.X-9.5f,0,9.5f-point.Y);
        private void OnDestroy()
        {
            foreach (Material material in new[] {lightTile,darkTile,coreMaterial,wallMaterial,towerMaterial,routeMaterial,rejectedMaterial})
                if (material!=null) Destroy(material);
        }
    }
}
