using UnityEngine;
using UnityEngine.UIElements;

namespace PrefabBoard.Editor.UI
{
    public sealed class SelectionOverlayElement : VisualElement
    {
        public SelectionOverlayElement()
        {
            AddToClassList("pb-selection-overlay");
            style.display = DisplayStyle.None;
            pickingMode = PickingMode.Ignore;
        }

        public void SetRect(Rect rect)
        {
            style.left = rect.xMin;
            style.top = rect.yMin;
            style.width = rect.width;
            style.height = rect.height;
        }

        public void SetVisible(bool visible)
        {
            style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
