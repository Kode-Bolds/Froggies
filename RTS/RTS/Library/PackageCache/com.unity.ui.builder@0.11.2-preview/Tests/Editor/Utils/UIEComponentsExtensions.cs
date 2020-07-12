using System.Collections;
using System.Linq;
using UnityEngine.UIElements;

namespace Unity.UI.Builder.EditorTests
{
    static class TreeViewExtensions
    {
        public static ITreeViewItem GetSelectedItem(this TreeView treeView)
        {
#if UNITY_2020_1_OR_NEWER
            return treeView.selectedItems.FirstOrDefault();
#else
            return treeView.currentSelection.FirstOrDefault();
#endif
        }

        public static IEnumerator SelectAndScrollToItemWithId(this TreeView treeView, int id)
        {
#if UNITY_2020_1_OR_NEWER
            treeView.SetSelection(id);
            yield return UIETestHelpers.Pause(1);
            treeView.ScrollToItem(id);
#else
            // This is the only way to scroll to the item.
            treeView.SelectItem(id);
#endif
            yield return UIETestHelpers.Pause(1);
        }

    }
}
