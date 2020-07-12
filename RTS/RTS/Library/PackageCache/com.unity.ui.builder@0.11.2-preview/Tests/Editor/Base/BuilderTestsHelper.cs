using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder.EditorTests
{
    public static class BuilderTestsHelper
    {
        static readonly Rect k_TestWindowMinSize = new Rect(Vector2.zero, new Vector2(1100, 600));
        static bool s_BuilderDocumentNotificationShown;

        internal static Builder MakeNewBuilderWindow()
        {
            if (!Application.isBatchMode && Builder.ActiveWindow != null)
            {
                if (Builder.ActiveWindow.document.hasUnsavedChanges)
                {
                    if(!s_BuilderDocumentNotificationShown)
                        EditorUtility.DisplayDialog("Failed", "Save Builder Document before running the tests.", "Ok");

                    s_BuilderDocumentNotificationShown = true;
                    return null;
                }
            }

            s_BuilderDocumentNotificationShown = false;
            var builderWindow = ScriptableObject.CreateInstance<Builder>();
            builderWindow.DisableViewDataPersistence();
            builderWindow.disableInputEvents = true;
            builderWindow.Show();

            builderWindow.position = new Rect(builderWindow.position.position, k_TestWindowMinSize.size);

            // Install our contextual menu manager.
            var panel = builderWindow.rootVisualElement.panel as BaseVisualElementPanel;
            panel.contextualMenuManager = new BuilderTestContextualMenuManager();

            return builderWindow;
        }

        internal static List<BuilderExplorerItem> GetExplorerItemsWithName(BuilderPaneContent paneContent, string name)
        {
            var list = paneContent.Query<BuilderExplorerItem>()
                .Where(item => item.Q<Label>().text.Equals(name)).ToList();

            if (list.Count == 0)
            {
                name = name + BuilderConstants.ToolbarUnsavedFileSuffix;
                list = paneContent.Query<BuilderExplorerItem>()
                    .Where(item => item.Q<Label>().text.Equals(name)).ToList();
            }

            return list;
        }

        internal static List<BuilderExplorerItem> GetExplorerItems(BuilderPaneContent paneContent)
        {
            return paneContent.Query<BuilderExplorerItem>()
                .Where(item => !item.row().classList.Contains(BuilderConstants.ExplorerHeaderRowClassName)).ToList();
        }

        internal static BuilderExplorerItem GetExplorerItemWithName(BuilderPaneContent paneContent, string name)
        {
            var items = GetExplorerItemsWithName(paneContent, name);
            return items.FirstOrDefault();
        }

        internal static Label GetLabelWithName(VisualElement container, string name)
        {
            return container.Query<Label>()
                .Where(item => item.text.Equals(name)).First();
        }

        internal static VisualElement GetLinkedDocumentElement(VisualElement hierarchyItem)
        {
           return (VisualElement) hierarchyItem.GetProperty(BuilderConstants.ElementLinkedDocumentVisualElementVEPropertyName);
        }

        internal static BuilderExplorerItem GetLinkedExplorerItem(VisualElement hierarchyItem)
        {
            return (BuilderExplorerItem) hierarchyItem.GetProperty(BuilderConstants.ElementLinkedExplorerItemVEPropertyName);
        }

        internal static BuilderExplorerItem GetHeaderItem(BuilderPaneContent paneContent)
        {
            var row = paneContent.Q<VisualElement>(className: BuilderConstants.ExplorerHeaderRowClassName);
            return row.Q<BuilderExplorerItem>();
        }
    }
}