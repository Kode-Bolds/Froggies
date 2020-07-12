using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderElementContextMenu
    {
        readonly BuilderPaneWindow m_PaneWindow;
        readonly BuilderSelection m_Selection;

        bool m_WeStartedTheDrag;

        List<ManipulatorActivationFilter> activators { get; }
        ManipulatorActivationFilter m_CurrentActivator;

        protected BuilderDocument document => m_PaneWindow.document;
        protected BuilderPaneWindow paneWindow => m_PaneWindow;

        public BuilderElementContextMenu(BuilderPaneWindow paneWindow, BuilderSelection selection)
        {
            m_PaneWindow = paneWindow;
            m_Selection = selection;

            m_WeStartedTheDrag = false;

            activators = new List<ManipulatorActivationFilter>();
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse });
            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Control });
            }
        }

        public void RegisterCallbacksOnTarget(VisualElement target)
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<ContextualMenuPopulateEvent>(a => BuildElementContextualMenu(a, target));
            target.RegisterCallback<DetachFromPanelEvent>(UnregisterCallbacksFromTarget);
        }

        void UnregisterCallbacksFromTarget(DetachFromPanelEvent evt)
        {
            var target = evt.target as VisualElement;

            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<ContextualMenuPopulateEvent>(a => BuildElementContextualMenu(a, target));
            target.UnregisterCallback<DetachFromPanelEvent>(UnregisterCallbacksFromTarget);
        }

        void OnMouseDown(MouseDownEvent evt)
        {
            if (!CanStartManipulation(evt))
                return;

            var target = evt.currentTarget as VisualElement;
            target.CaptureMouse();
            m_WeStartedTheDrag = true;
            evt.StopPropagation();
        }

        void OnMouseUp(MouseUpEvent evt)
        {
            var target = evt.currentTarget as VisualElement;

            if (!target.HasMouseCapture() || !m_WeStartedTheDrag)
                return;

            if (!CanStopManipulation(evt))
                return;
            
            DisplayContextMenu(evt, target);

            target.ReleaseMouse();
            m_WeStartedTheDrag = false;
            evt.StopPropagation();
        }

        public void DisplayContextMenu(EventBase triggerEvent, VisualElement target)
        {
            if (target.elementPanel?.contextualMenuManager != null)
            {
                target.elementPanel.contextualMenuManager.DisplayMenu(triggerEvent, target);
                triggerEvent.PreventDefault();
            }
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

        public virtual void BuildElementContextualMenu(ContextualMenuPopulateEvent evt, VisualElement target)
        {
            var documentElement = target.GetProperty(BuilderConstants.ElementLinkedDocumentVisualElementVEPropertyName) as VisualElement;
            
            var isValidTarget = documentElement != null && (documentElement.IsPartOfCurrentDocument() || documentElement.GetStyleComplexSelector() != null);
            if (isValidTarget)
                evt.StopImmediatePropagation();

            evt.menu.AppendAction(
                "Copy",
                a =>
                {
                    m_Selection.Select(null, documentElement);
                    if (documentElement.IsPartOfCurrentDocument() || documentElement.GetStyleComplexSelector() != null)
                        m_PaneWindow.commandHandler.PerformActionOnSelection(
                            m_PaneWindow.commandHandler.CopyElement,
                            m_PaneWindow.commandHandler.ClearCopyBuffer);
                },
                isValidTarget
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction(
                "Paste",
                a =>
                {
                    m_Selection.Select(null, documentElement);
                    m_PaneWindow.commandHandler.Paste();
                },
                string.IsNullOrEmpty(BuilderEditorUtility.SystemCopyBuffer)
                    ? DropdownMenuAction.Status.Disabled
                    : DropdownMenuAction.Status.Normal);

            evt.menu.AppendSeparator();

            evt.menu.AppendAction(
                "Rename",
                a =>
                {
                    m_Selection.Select(null, documentElement);
                    var explorerItemElement = documentElement.GetProperty(BuilderConstants.ElementLinkedExplorerItemVEPropertyName) as BuilderExplorerItem;
                    if (explorerItemElement == null)
                        return;

                    explorerItemElement.ActivateRenameElementMode();

                },
                documentElement != null && documentElement.IsPartOfCurrentDocument()
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction(
                "Duplicate",
                a =>
                {
                    m_Selection.Select(null, documentElement);
                    if (documentElement.IsPartOfCurrentDocument() || documentElement.GetStyleComplexSelector() != null)
                        m_PaneWindow.commandHandler.PerformActionOnSelection(
                            m_PaneWindow.commandHandler.DuplicateElement,
                            m_PaneWindow.commandHandler.ClearCopyBuffer,
                            m_PaneWindow.commandHandler.Paste);
                },
                isValidTarget
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendSeparator();

            evt.menu.AppendAction(
                "Delete",
                a =>
                {
                    m_Selection.Select(null, documentElement);
                    m_PaneWindow.commandHandler.DeleteSelection();
                },
                isValidTarget
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled);
        }
    }
}