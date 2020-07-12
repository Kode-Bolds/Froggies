using System;
using UnityEngine.UIElements;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;

namespace Unity.UI.Builder
{
    internal abstract class BuilderExplorer : BuilderPaneContent, IBuilderSelectionNotifier
    {
        static readonly string s_UssClassName = "unity-builder-explorer";

        [Flags]
        internal enum BuilderElementInfoVisibilityState
        {
            TypeName = 1 << 0,
            ClassList = 1 << 1,

            All = ~0
        }

        VisualElement m_DocumentElementRoot;
        bool m_IncludeDocumentElementRoot;
        VisualElement m_DocumentElement;
        protected BuilderPaneWindow m_PaneWindow;
        protected BuilderViewport m_Viewport;
        protected ElementHierarchyView m_ElementHierarchyView;
        protected BuilderSelection m_Selection;
        bool m_SelectionMadeExternally;

        BuilderClassDragger m_ClassDragger;
        BuilderHierarchyDragger m_HierarchyDragger;
        BuilderElementContextMenu m_ContextMenuManipulator;

        public VisualElement container
        {
            get { return m_ElementHierarchyView.container; }
        }

        // Caching whether we need to rebuild the hierarchy on a style change.
        // We need to rebuild the hierarchy to update the file name to indicate to the user that there
        // are unsaved changes.  But Style changes do not change the hierarchy.  Thus, we only need to
        // rebuild the hierarchy to indicate that there are unsaved changes due to style when:
        //     1. the document has no unsaved changes
        //     2. and it's the first style change event.
        // Otherwise there is no need.
        private bool m_ShouldRebuildHierarchyOnStyleChange;

        public BuilderExplorer(
            BuilderPaneWindow paneWindow,
            BuilderViewport viewport,
            BuilderSelection selection,
            BuilderClassDragger classDragger,
            BuilderHierarchyDragger hierarchyDragger,
            BuilderElementContextMenu contextMenuManipulator,
            VisualElement documentElementRoot,
            bool includeDocumentElementRoot,
            HighlightOverlayPainter highlightOverlayPainter,
            string toolbarUxmlPath)
        {
            m_PaneWindow = paneWindow;
            m_Viewport = viewport;
            m_DocumentElementRoot = documentElementRoot;
            m_IncludeDocumentElementRoot = includeDocumentElementRoot;
            m_DocumentElement = viewport.documentElement;
            AddToClassList(s_UssClassName);

            m_ClassDragger = classDragger;
            m_HierarchyDragger = hierarchyDragger;
            m_ContextMenuManipulator = contextMenuManipulator;

            m_SelectionMadeExternally = false;

            m_Selection = selection;


            // Query the UI
            if (!string.IsNullOrEmpty(toolbarUxmlPath))
            {
                var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(toolbarUxmlPath);
                template.CloneTree(this);
            }

            // Create the Hierarchy View.
            m_ElementHierarchyView = new ElementHierarchyView(
                m_PaneWindow,
                m_DocumentElement,
                selection, classDragger, hierarchyDragger,
                contextMenuManipulator, ElementSelected, highlightOverlayPainter);
            m_ElementHierarchyView.style.flexGrow = 1;
            Add(m_ElementHierarchyView);

            // Enable horizontal scrolling.
#if UNITY_2020_2_OR_NEWER
            m_ElementHierarchyView.Q<TreeView>().horizontalScrollingEnabled = true;
#endif

            // Make sure the Hierarchy View gets focus when the pane gets focused.
            primaryFocusable = m_ElementHierarchyView.Q<ListView>();

            UpdateHierarchyAndSelection(false);
            m_ShouldRebuildHierarchyOnStyleChange = true;
        }

        public void ClearHighlightOverlay()
        {
            m_ElementHierarchyView.ClearHighlightOverlay();
        }

        public void ResetHighlightOverlays()
        {
            m_ElementHierarchyView.ResetHighlightOverlays();
        }

        protected virtual void ElementSelected(VisualElement element)
        {
            if (m_SelectionMadeExternally)
                return;

            if (element == null)
            {
                m_Selection.ClearSelection(this);
                return;
            }
            else if (element.ClassListContains(BuilderConstants.ExplorerItemUnselectableClassName))
            {
                m_SelectionMadeExternally = true;
                m_ElementHierarchyView.ClearSelection();
                m_SelectionMadeExternally = false;
                m_Selection.ClearSelection(this);
                return;
            }

            m_Selection.Select(this, element);
        }

        protected void UpdateHierarchy(bool hasUnsavedChanges)
        {
            m_ElementHierarchyView.hierarchyHasChanged = true;
            m_ElementHierarchyView.hasUnsavedChanges = hasUnsavedChanges;
            m_ElementHierarchyView.RebuildTree(m_DocumentElementRoot, m_IncludeDocumentElementRoot);
        }

        public void UpdateHierarchyAndSelection(bool hasUnsavedChanges)
        {
            m_SelectionMadeExternally = true;

            UpdateHierarchy(hasUnsavedChanges);

            if (!m_Selection.isEmpty)
            {
                m_ElementHierarchyView.SelectElement(m_Selection.selection.First());
                m_ElementHierarchyView.IncrementVersion(VersionChangeType.Styles);
            }

            m_SelectionMadeExternally = false;
        }

        public virtual void HierarchyChanged(VisualElement element, BuilderHierarchyChangeType changeType)
        {
            if (element == null ||
                changeType.HasFlag(BuilderHierarchyChangeType.ChildrenAdded) ||
                changeType.HasFlag(BuilderHierarchyChangeType.ChildrenRemoved) ||
                changeType.HasFlag(BuilderHierarchyChangeType.Name) ||
                changeType.HasFlag(BuilderHierarchyChangeType.ClassList))
            {
                UpdateHierarchyAndSelection(m_Selection.hasUnsavedChanges);
                m_ShouldRebuildHierarchyOnStyleChange = !m_Selection.hasUnsavedChanges;
            }
        }

        protected virtual bool IsSelectedItemValid(VisualElement element)
        {
            return true;
        }

        public virtual void SelectionChanged()
        {
            if (!m_Selection.selection.Any())
            {
                m_SelectionMadeExternally = true;
                m_ElementHierarchyView.ClearSelection();
                m_SelectionMadeExternally = false;
                return;
            }

            var element = m_Selection.selection.First();
            if (!IsSelectedItemValid(element))
            {
                m_SelectionMadeExternally = true;
                m_ElementHierarchyView.ClearSelection();
                m_SelectionMadeExternally = false;
                return;
            }

            m_SelectionMadeExternally = true;
            m_ElementHierarchyView.SelectElement(element);
            m_SelectionMadeExternally = false;
        }

        public virtual void StylingChanged(List<string> styles)
        {
            if (m_ShouldRebuildHierarchyOnStyleChange)
                UpdateHierarchyAndSelection(m_Selection.hasUnsavedChanges);
            m_ShouldRebuildHierarchyOnStyleChange = !m_Selection.hasUnsavedChanges;
        }
    }
}
