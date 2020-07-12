using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderHierarchyDragger : BuilderDragger
    {
        static readonly string s_DraggableStyleClassPillClassName = "unity-builder-class-pill--draggable";
        static readonly string s_DragPreviewElementClassName = "unity-builder-dragger__drag-preview";

        VisualElement m_DragPreviewLastParent;

        VisualElement m_OldParent;
        int m_OldIndex;

        VisualElement m_TargetElementToReparent;

        public BuilderHierarchyDragger(
            BuilderPaneWindow paneWindow,
            VisualElement root, BuilderSelection selection,
            BuilderViewport viewport, BuilderParentTracker parentTracker)
            : base(paneWindow, root, selection, viewport, parentTracker)
        {

        }

        protected override VisualElement CreateDraggedElement()
        {
            var classPillTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                BuilderConstants.UIBuilderPackagePath + "/BuilderClassPill.uxml");
            var pill = classPillTemplate.CloneTree();
            pill.AddToClassList(s_DraggableStyleClassPillClassName);
            return pill;
        }

        protected override bool StartDrag(VisualElement target, Vector2 mousePosition, VisualElement pill)
        {
            m_TargetElementToReparent = target.GetProperty(BuilderConstants.ExplorerItemElementLinkVEPropertyName) as VisualElement;
            if (!m_TargetElementToReparent.IsPartOfCurrentDocument())
                return false;

            var pillLabel = pill.Q<Label>();
            pillLabel.text = string.IsNullOrEmpty(m_TargetElementToReparent.name)
                ? m_TargetElementToReparent.GetType().Name
                : m_TargetElementToReparent.name;

            // TODO: Clean this by having different draggers for Hierarchy and StyleSheets panes.
            // For now, just remove the yellow text color from pill if dragger element corresponds to a VisualTreeAsset.
            if (m_TargetElementToReparent.GetVisualElementAsset() != null)
                pillLabel.RemoveFromClassList(BuilderConstants.ElementClassNameClassName);

            m_OldParent = m_TargetElementToReparent.parent;
            m_OldIndex = m_OldParent.IndexOf(m_TargetElementToReparent);

            return true;
        }

        protected override void PerformDrag(VisualElement target, VisualElement pickedElement, int index = -1)
        {
            if (pickedElement == null)
            {
                FailAction(target);
                return;
            }

            if (pickedElement == m_DragPreviewLastParent)
            {
                return;
            }
            else
            {
                ResetDragPreviewElement();

                m_DragPreviewLastParent = pickedElement;

                m_DragPreviewLastParent.HideMinSizeSpecialElement();

                FixElementSizeAndPosition(m_DragPreviewLastParent);
            }

            m_TargetElementToReparent.AddToClassList(s_DragPreviewElementClassName);

            Reparent(pickedElement, index);
        }

        void Reparent(VisualElement newParent, int index)
        {
            var elementToReparent = m_TargetElementToReparent;

            if (newParent == elementToReparent)
                return;

            if (index < 0)
                newParent.Add(elementToReparent);
            else
                newParent.Insert(index, elementToReparent);
        }

        protected override void PerformAction(VisualElement destination, DestinationPane pane, int index = -1)
        {
            m_TargetElementToReparent.RemoveFromClassList(s_DragPreviewElementClassName);

            // Remove temporary min-size element.
            destination.RemoveMinSizeSpecialElement();

            var elementToReparent = m_TargetElementToReparent;
            var newParent = destination;

            // We already have the correct index from the preview element that is
            // already inserted in the hierarchy. The index we get from the arguments
            // is actually incorrect (off by one) because it will count the
            // preview element.
            index = m_DragPreviewLastParent.IndexOf(m_TargetElementToReparent);

            BuilderAssetUtilities.ReparentElementInAsset(
                paneWindow.document, elementToReparent, newParent, index);

            selection.NotifyOfHierarchyChange(null);
        }

        protected override bool StopEventOnMouseDown()
        {
            return false;
        }

        protected override bool IsPickedElementValid(VisualElement element)
        {
            if (element == null)
                return true;

            if (element == m_TargetElementToReparent)
                return false;

            var hasAncestor = element.HasAncestor(m_TargetElementToReparent);

            return !hasAncestor;
        }

        protected override bool SupportsDragBetweenElements()
        {
            return true;
        }

        protected override void EndDrag()
        {
            ResetDragPreviewElement();
        }

        protected override void FailAction(VisualElement target)
        {
            ResetDragPreviewElement();
            Reparent(m_OldParent, m_OldIndex);
        }

        void ResetDragPreviewElement()
        {
            m_TargetElementToReparent.RemoveFromClassList(s_DragPreviewElementClassName);

            if (m_DragPreviewLastParent == null)
                return;

            UnfixElementSizeAndPosition(m_DragPreviewLastParent);
            m_DragPreviewLastParent.UnhideMinSizeSpecialElement();
            m_DragPreviewLastParent = null;
        }
    }
}