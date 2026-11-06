using UnityEngine;
using UnityEngine.UI;

namespace Brick2Life.Simulation
{
    // Stacks overlay controls directly under the workspace title, sharing one size and left edge.
    public static class WorkspaceHeaderStack
    {
        public const float Width = 168f;
        public const float Height = 36f;
        public const float Gap = 8f;
        public static readonly Color ButtonColor = new Color(0.16f, 0.34f, 0.46f, 0.92f);

        public static void Place(RectTransform button, int slot)
        {
            if (!TryPlace(button, slot, Width, Height, 60)) return;
            Image image = button.GetComponent<Image>();
            if (image != null) image.color = ButtonColor;
        }

        // Top edge sits in the given slot, so a panel can start under the last header button.
        public static void PlaceBelow(RectTransform panel, int slot, float width)
        {
            if (!TryPlace(panel, slot, width, panel.sizeDelta.y, 55)) return;
            ContentSizeFitter fitter = panel.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static bool TryPlace(RectTransform item, int slot, float width, float height, int sortingOrder)
        {
            if (item == null || slot < 0) return false;
            GameObject titleObject = GameObject.Find("WorkspaceTitle");
            if (titleObject == null) return false;
            RectTransform title = titleObject.GetComponent<RectTransform>();
            if (title == null || !(title.parent is RectTransform parent)) return false;

            item.SetParent(parent, false);
            Canvas canvas = item.GetComponent<Canvas>();
            if (canvas == null) canvas = item.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;
            if (item.GetComponent<GraphicRaycaster>() == null)
                item.gameObject.AddComponent<GraphicRaycaster>();

            Canvas.ForceUpdateCanvases();
            Vector3 bottomLeft = title.TransformPoint(new Vector3(title.rect.xMin, title.rect.yMin, 0f));
            Vector3 local = parent.InverseTransformPoint(bottomLeft);
            Rect parentRect = parent.rect;
            float x = local.x - parentRect.xMin;
            float y = local.y - parentRect.yMax - Gap - slot * (Height + Gap);
            item.anchorMin = new Vector2(0f, 1f);
            item.anchorMax = new Vector2(0f, 1f);
            item.pivot = new Vector2(0f, 1f);
            item.sizeDelta = new Vector2(width, height);
            item.anchoredPosition = new Vector2(x, y);
            item.localScale = Vector3.one;
            return true;
        }
    }
}
