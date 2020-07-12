using UnityEditor;
using UnityEngine.UIElements;
using System.Collections;

namespace Unity.UI.Builder.EditorTests
{
    public static class UIETestHelpers
    {
        public static bool IsCompletelyVisible(EditorWindow window, VisualElement element)
        {
            if (!element.enabledInHierarchy)
            {
                return false;
            }

            if (!element.visible)
            {
                return false;
            }

            var windowBounds = window.rootVisualElement.worldBound;
            var elementBounds = element.worldBound;
            if (!(elementBounds.x >= windowBounds.x)
                || !(elementBounds.y >= windowBounds.y)
                || !(windowBounds.x + windowBounds.width  >= elementBounds.x + elementBounds.width)
                || !(windowBounds.y + windowBounds.height >= elementBounds.y + elementBounds.height))
            {
                return false;
            }
            return true;
        }

        public static IEnumerator Pause(int frames = 1)
        {
            for (var i = 0; i < frames; i++)
            {
                yield return null;
            }
        }

        public static VisualElement GetRoot(VisualElement visualElement)
        {
            while (visualElement.parent != null)
                visualElement = visualElement.parent;

            return visualElement;
        }

        public static IEnumerator ExpandTreeViewItem(VisualElement item)
        {
            VisualElement treeViewItemContainer = null;
            while (treeViewItemContainer == null &&  item != null)
            {
                if (item.name.Equals("unity-tree-view__item"))
                    treeViewItemContainer = item;
                else
                    item = item.parent;
            }

            if (treeViewItemContainer != null)
                yield return UIETestEvents.Mouse.SimulateClick(treeViewItemContainer.Q<Toggle>());
        }
    }
}
