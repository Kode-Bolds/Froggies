using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderDragger
    {
        protected enum DestinationPane
        {
            Hierarchy,
            Viewport
        };

        static readonly string s_DraggerPreviewClassName = "unity-builder-dragger-preview";
        static readonly string s_DraggedPreviewClassName = "unity-builder-dragger-preview--dragged";

        static readonly string s_TreeItemHoverHoverClassName = "unity-builder-explorer__item--dragger-hover";
        static readonly string s_TreeItemHoverWithDragBetweenElementsSupportClassName = "unity-builder-explorer__between-element-item--dragger-hover";
        static readonly string s_TreeViewItemName = "unity-tree-view__item";
        static readonly int s_DistanceToActivation = 5;

        Vector2 m_Start;
        bool m_Active;
        bool m_WeStartedTheDrag;

        BuilderPaneWindow m_PaneWindow;
        VisualElement m_Root;
        VisualElement m_Canvas;
        BuilderSelection m_Selection;

        VisualElement m_DraggedElement;
        VisualElement m_LastHoverElement;
        VisualElement m_LastRowHoverElement;

        BuilderParentTracker m_ParentTracker;

        public VisualElement builderHierarchyRoot { get; set; }

        protected BuilderPaneWindow paneWindow { get { return m_PaneWindow; } }
        protected BuilderSelection selection { get { return m_Selection; } }

        protected BuilderViewport Viewport { get; set; }

        List<ManipulatorActivationFilter> activators { get; set; }
        ManipulatorActivationFilter m_CurrentActivator;

        public BuilderDragger(
            BuilderPaneWindow paneWindow,
            VisualElement root, BuilderSelection selection,
            BuilderViewport viewport, BuilderParentTracker parentTracker)
        {
            m_PaneWindow = paneWindow;
            m_Root = root;
            Viewport = viewport;
            m_Canvas = viewport.documentElement;
            m_Selection = selection;
            m_ParentTracker = parentTracker;

            activators = new List<ManipulatorActivationFilter>();
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });

            m_Active = false;
            m_WeStartedTheDrag = false;

            m_DraggedElement = CreateDraggedElement();
            m_DraggedElement.AddToClassList(s_DraggerPreviewClassName);
            m_Root.Add(m_DraggedElement);
        }

        protected virtual VisualElement CreateDraggedElement()
        {
            return new VisualElement();
        }

        protected virtual bool StartDrag(VisualElement target, Vector2 mousePosition, VisualElement pill)
        {
            return true;
        }

        protected virtual void PerformDrag(VisualElement target, VisualElement pickedElement, int index = -1)
        {

        }

        protected virtual void PerformAction(VisualElement destination, DestinationPane pane, int index = -1)
        {

        }

        protected virtual void FailAction(VisualElement target)
        {

        }

        protected virtual void EndDrag()
        {

        }

        protected virtual bool StopEventOnMouseDown()
        {
            return true;
        }

        protected virtual bool IsPickedElementValid(VisualElement element)
        {
            return true;
        }

        protected virtual bool SupportsDragBetweenElements()
        {
            return false;
        }

        protected void FixElementSizeAndPosition(VisualElement target)
        {
            target.style.minWidth = target.resolvedStyle.width;
            target.style.minHeight = target.resolvedStyle.height;
        }

        protected void UnfixElementSizeAndPosition(VisualElement target)
        {
            target.style.minWidth = StyleKeyword.Null;
            target.style.minHeight = StyleKeyword.Null;
        }

        public void RegisterCallbacksOnTarget(VisualElement target)
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<KeyUpEvent>(OnEsc);

            target.RegisterCallback<DetachFromPanelEvent>(UnregisterCallbacksFromTarget);
        }

        void UnregisterCallbacksFromTarget(DetachFromPanelEvent evt)
        {
            var target = evt.target as VisualElement;

            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<KeyUpEvent>(OnEsc);

            target.UnregisterCallback<DetachFromPanelEvent>(UnregisterCallbacksFromTarget);
        }

        bool StartDrag(VisualElement target, Vector2 mousePosition)
        {
            var startSuccess = StartDrag(target, mousePosition, m_DraggedElement);
            if (!startSuccess)
                return startSuccess;

            m_DraggedElement.BringToFront();
            m_DraggedElement.AddToClassList(s_DraggedPreviewClassName);

            // So we don't have a flashing element at the top left corner
            // at the very start of the drag.
            PerformDragInner(target, mousePosition);

            return startSuccess;
        }

        bool TryToPickInCanvas(Vector2 mousePosition)
        {
            var localMouse = m_Canvas.WorldToLocal(mousePosition);
            if (!m_Canvas.ContainsPoint(localMouse))
            {
                m_ParentTracker.Deactivate();
                return false;
            }

            var pickedElement = Panel.PickAllWithoutValidatingLayout(m_Canvas, mousePosition);

            // Don't allow selection of elements inside template instances.
            pickedElement = pickedElement.GetClosestElementPartOfCurrentDocument();

            // Get Closest valid element.
            pickedElement = pickedElement.GetClosestElementThatIsValid(IsPickedElementValid);

            if (pickedElement == null)
            {
                m_ParentTracker.Deactivate();
                return false;
            }

            m_LastHoverElement = pickedElement;

            m_ParentTracker.Activate(pickedElement);

            return true;
        }

        bool IsElementTheScrollView(VisualElement pickedElement)
        {
            if (pickedElement == null)
                return false;

            if (pickedElement is ScrollView)
                return true;

            if (pickedElement.ClassListContains(ScrollView.viewportUssClassName))
                return true;

            return false;
        }

        bool TryToPickInHierarchy(Vector2 mousePosition)
        {
            if (builderHierarchyRoot == null)
                return false;

            var localMouse = builderHierarchyRoot.WorldToLocal(mousePosition);
            if (!builderHierarchyRoot.ContainsPoint(localMouse))
            {
                return false;
            }

            var supportsDragBetweenElements = SupportsDragBetweenElements();

            var pickedElement = Panel.PickAllWithoutValidatingLayout(builderHierarchyRoot, mousePosition);
            if (!IsElementTheScrollView(pickedElement))
            {
                while (true)
                {
                    if (pickedElement == null)
                        break;

                    if (pickedElement.GetProperty(BuilderConstants.ExplorerItemElementLinkVEPropertyName) != null)
                        break;

                    if (supportsDragBetweenElements && pickedElement.ClassListContains(BuilderConstants.ExplorerItemReorderZoneClassName))
                        break;

                    pickedElement = pickedElement.parent;
                }
            }

            // Don't allow selection of elements inside template instances.
            VisualElement linkedCanvasPickedElement = null;
            if (pickedElement != null && pickedElement.ClassListContains(BuilderConstants.ExplorerItemReorderZoneClassName))
            {
                linkedCanvasPickedElement = GetLinkedElementFromReorderZone(pickedElement);
            }
            else if (pickedElement != null)
            {
                linkedCanvasPickedElement = pickedElement.GetProperty(BuilderConstants.ExplorerItemElementLinkVEPropertyName) as VisualElement;
            }
            if (pickedElement != null &&
                !IsElementTheScrollView(pickedElement) &&
                linkedCanvasPickedElement.GetVisualElementAsset() == null)
                pickedElement = null;

            // Validate element with implementation.
            if (pickedElement != null && !IsPickedElementValid(linkedCanvasPickedElement))
                pickedElement = null;

            m_LastHoverElement = pickedElement;
            if (pickedElement == null)
            {
                m_LastRowHoverElement = null;
                return false;
            }

            // The hover style class may not be applied to the hover element itself. We need
            // to find the correct parent.
            m_LastRowHoverElement = m_LastHoverElement;
            if (!IsElementTheScrollView(pickedElement))
            {
                while (m_LastRowHoverElement != null && m_LastRowHoverElement.name != s_TreeViewItemName)
                    m_LastRowHoverElement = m_LastRowHoverElement.parent;
            }

            m_LastRowHoverElement.AddToClassList(s_TreeItemHoverHoverClassName);

            if (supportsDragBetweenElements)
                m_LastRowHoverElement.AddToClassList(s_TreeItemHoverWithDragBetweenElementsSupportClassName);

            return true;
        }

        void PerformDragInner(VisualElement target, Vector2 mousePosition)
        {
            // Move dragged element.
            m_DraggedElement.style.left = mousePosition.x;
            m_DraggedElement.style.top = mousePosition.y;

            m_LastRowHoverElement?.RemoveFromClassList(s_TreeItemHoverHoverClassName);
            m_LastRowHoverElement?.RemoveFromClassList(s_TreeItemHoverWithDragBetweenElementsSupportClassName);

            var validHover = TryToPickInCanvas(mousePosition);
            if (validHover)
            {
                PerformDrag(target, m_LastHoverElement);
                return;
            }

            validHover = TryToPickInHierarchy(mousePosition);
            if (validHover)
            {
                VisualElement pickedElement;
                int index;
                GetPickedElementFromHoverElement(out pickedElement, out index);

                PerformDrag(target, pickedElement, index);
                return;
            }

            PerformDrag(target, null);
        }

        void EndDragInner()
        {
            EndDrag();

            m_LastRowHoverElement?.RemoveFromClassList(s_TreeItemHoverHoverClassName);
            m_LastRowHoverElement?.RemoveFromClassList(s_TreeItemHoverWithDragBetweenElementsSupportClassName);
            m_DraggedElement.RemoveFromClassList(s_DraggedPreviewClassName);
            m_ParentTracker.Deactivate();
        }

        void OnMouseDown(MouseDownEvent evt)
        {
            var target = evt.currentTarget as VisualElement;
            if (m_WeStartedTheDrag && target.HasMouseCapture())
            {
                evt.StopImmediatePropagation();
                evt.PreventDefault();
                return;
            }
            
            if (!CanStartManipulation(evt))
                return;
            
            var stopEvent = StopEventOnMouseDown();
            if (stopEvent)
                evt.StopImmediatePropagation();

            if (target.HasMouseCapture())
            {
                if (!stopEvent)
                    evt.StopImmediatePropagation();

                return;
            }

            m_Start = evt.mousePosition;
            m_WeStartedTheDrag = true;
            target.CaptureMouse();
        }

        void OnMouseMove(MouseMoveEvent evt)
        {
            var target = evt.currentTarget as VisualElement;

            if (!target.HasMouseCapture() || !m_WeStartedTheDrag)
                return;

            if (!m_Active)
            {
                if (Mathf.Abs(m_Start.x - evt.mousePosition.x) > s_DistanceToActivation ||
                    Mathf.Abs(m_Start.y - evt.mousePosition.y) > s_DistanceToActivation)
                {
                    var startSuccess = StartDrag(target, evt.mousePosition);

                    if (startSuccess)
                    {
                        evt.StopImmediatePropagation();
                        evt.StopPropagation();
                        m_Active = true;
                    }
                    else
                    {
                        target.ReleaseMouse();
                    }
                }

                return;
            }

            PerformDragInner(target, evt.mousePosition);

            evt.StopPropagation();
        }

        VisualElement GetLinkedElementFromReorderZone(VisualElement hoverZone)
        {
            var reorderZone = hoverZone;
            var explorerItem = reorderZone.userData as BuilderExplorerItem;
            var sibling = explorerItem.GetProperty(BuilderConstants.ExplorerItemElementLinkVEPropertyName) as VisualElement;
            return sibling;
        }

        void OnMouseUp(MouseUpEvent evt)
        {
            if (evt.button != (int) MouseButton.LeftMouse)
            {
                evt.StopPropagation();
                evt.PreventDefault();

                return;
            }

            var target = evt.currentTarget as VisualElement;

            if (!CanStopManipulation(evt))
                return;

            target.ReleaseMouse();

            if (!m_Active)
                return;

            if (m_Active)
            {
                var currentMouse = evt.mousePosition;
                if (m_LastHoverElement != null)
                {
                    var localCanvasMouse = m_Canvas.WorldToLocal(currentMouse);
                    var localHierarchyMouse = builderHierarchyRoot.WorldToLocal(currentMouse);

                    if (m_Canvas.ContainsPoint(localCanvasMouse))
                    {
                        PerformAction(m_LastHoverElement, DestinationPane.Viewport);
                    }
                    else if (builderHierarchyRoot.ContainsPoint(localHierarchyMouse))
                    {
                        VisualElement newParent;
                        int index;
                        GetPickedElementFromHoverElement(out newParent, out index);

                        PerformAction(newParent, DestinationPane.Hierarchy, index);
                    }
                }

                evt.StopPropagation();
                m_Active = false;
            }
            else
            {
                FailAction(target);
            }

            EndDragInner();
        }

        void GetPickedElementFromHoverElement(out VisualElement pickedElement, out int index)
        {
            index = -1;
            if (IsElementTheScrollView(m_LastRowHoverElement))
                pickedElement = m_Canvas;
            else if (m_LastHoverElement.ClassListContains(BuilderConstants.ExplorerItemReorderZoneClassName))
            {
                var reorderZone = m_LastHoverElement;
                var sibling = GetLinkedElementFromReorderZone(reorderZone);

                pickedElement = sibling.parent;

                var siblingIndex = pickedElement.IndexOf(sibling);
                index = pickedElement.childCount;

                if (reorderZone.ClassListContains(BuilderConstants.ExplorerItemReorderZoneAboveClassName))
                {
                    index = siblingIndex;
                }
                else if (reorderZone.ClassListContains(BuilderConstants.ExplorerItemReorderZoneBelowClassName))
                {
                    index = siblingIndex + 1;
                }
            }
            else
                pickedElement = m_LastHoverElement.GetProperty(BuilderConstants.ExplorerItemElementLinkVEPropertyName) as VisualElement;
        }

        void OnEsc(KeyUpEvent evt)
        {
            if (evt.keyCode != KeyCode.Escape)
                return;

            var target = evt.currentTarget as VisualElement;

            if (!m_Active)
                return;

            m_Active = false;

            if (!target.HasMouseCapture())
                return;

            target.ReleaseMouse();
            evt.StopPropagation();
            EndDragInner();

            FailAction(target);
        }

        bool CanStartManipulation(IMouseEvent evt)
        {
            foreach (var activator in activators)
            {
                if (activator.Matches(evt))
                {
                    m_CurrentActivator = activator;
                    return true;
                }
            }

            return false;
        }

        bool CanStopManipulation(IMouseEvent evt)
        {
            if (evt == null)
            {
                return false;
            }

            return ((MouseButton)evt.button == m_CurrentActivator.button);
        }
    }
}