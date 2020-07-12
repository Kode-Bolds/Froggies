using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.StyleSheets;
using System.Linq;
using System.Collections.Generic;

namespace Unity.UI.Builder
{
    internal class BuilderCommandHandler
    {
        BuilderPaneWindow m_PaneWindow;
        BuilderToolbar m_Toolbar;
        BuilderSelection m_Selection;

        VisualElement m_CutElement;

        List<BuilderPaneContent> m_Panes = new List<BuilderPaneContent>();

        bool m_ControlWasPressed;
        IVisualElementScheduledItem m_ControlUnpressScheduleItem;

#if UNITY_2019_2
        // TODO: Hack. We need this because of a bug on Mac where we
        // get double command events.
        // Case: https://fogbugz.unity3d.com/f/cases/1180090/
        long m_LastFrameCount;
#endif

        public BuilderCommandHandler(
            BuilderPaneWindow paneWindow,
            BuilderSelection selection)
        {
            m_PaneWindow = paneWindow;
            m_Toolbar = null;
            m_Selection = selection;
        }

        public void OnEnable()
        {
            var root = m_PaneWindow.rootVisualElement;
            root.focusable = true; // We want commands to work anywhere in the builder.

            foreach (var pane in m_Panes)
            {
                pane.primaryFocusable.RegisterCallback<ValidateCommandEvent>(OnCommandValidate);
                pane.primaryFocusable.RegisterCallback<ExecuteCommandEvent>(OnCommandExecute);

                // Make sure Delete key works on Mac keyboards.
                pane.primaryFocusable.RegisterCallback<KeyDownEvent>(OnDelete);
            }

            // Ctrl+S to save.
            m_PaneWindow.rootVisualElement.RegisterCallback<KeyUpEvent>(OnSaveDocument);
            m_ControlUnpressScheduleItem = m_PaneWindow.rootVisualElement.schedule.Execute(UnsetControlFlag);

            // Undo/Redo
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        public void OnDisable()
        {
            foreach (var pane in m_Panes)
            {
                pane.primaryFocusable.UnregisterCallback<ValidateCommandEvent>(OnCommandValidate);
                pane.primaryFocusable.UnregisterCallback<ExecuteCommandEvent>(OnCommandExecute);

                pane.primaryFocusable.UnregisterCallback<KeyDownEvent>(OnDelete);
            }

            m_PaneWindow.rootVisualElement.UnregisterCallback<KeyUpEvent>(OnSaveDocument);

            // Undo/Redo
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        public void RegisterPane(BuilderPaneContent paneContent)
        {
            m_Panes.Add(paneContent);
        }

        public void RegisterToolbar(BuilderToolbar toolbar)
        {
            m_Toolbar = toolbar;
        }

        public void OnCommandValidate(ValidateCommandEvent evt)
        {
#if UNITY_2019_2
            // TODO: Hack. We need this because of a bug on Mac where we
            // get double command events.

            if (m_LastFrameCount == Time.frameCount)
                return;
            m_LastFrameCount = Time.frameCount;
#endif

            switch (evt.commandName)
            {
                case EventCommandNames.Cut: evt.StopPropagation(); return;
                case EventCommandNames.Copy: evt.StopPropagation(); return;
                case EventCommandNames.SoftDelete:
                case EventCommandNames.Delete: evt.StopPropagation(); return;
                case EventCommandNames.Duplicate: evt.StopPropagation(); return;
                case EventCommandNames.Paste: evt.StopPropagation(); return;
#if UNITY_2019_3_OR_NEWER
                case EventCommandNames.Rename: evt.StopPropagation(); return;
#endif
            }
        }

        public void OnCommandExecute(ExecuteCommandEvent evt)
        {
            switch (evt.commandName)
            {
                case EventCommandNames.Cut: PerformActionOnSelection(CutElement, ClearCopyBuffer, JustNotify); return;
                case EventCommandNames.Copy: PerformActionOnSelection(CopyElement, ClearCopyBuffer); return;
                case EventCommandNames.SoftDelete:
                case EventCommandNames.Delete: DeleteSelection(); return;
                case EventCommandNames.Duplicate: PerformActionOnSelection(DuplicateElement, ClearCopyBuffer, Paste); return;
                case EventCommandNames.Paste: Paste(); return;
#if UNITY_2019_3_OR_NEWER
                case EventCommandNames.Rename: PerformActionOnSelection(RenameElement); return;
#endif
            }
        }

        static void RenameElement(VisualElement element)
        {
            var explorerItemElement = element.GetProperty(BuilderConstants.ElementLinkedExplorerItemVEPropertyName) as BuilderExplorerItem;
            explorerItemElement?.ActivateRenameElementMode();
        }

        void OnUndoRedo()
        {
            m_PaneWindow.OnEnableAfterAllSerialization();
        }

        void UnsetControlFlag()
        {
            m_ControlWasPressed = false;
            m_ControlUnpressScheduleItem.Pause();
        }

        void OnSaveDocument(KeyUpEvent evt)
        {
            if (m_Toolbar == null)
                return;

            if (evt.keyCode == KeyCode.LeftCommand ||
                evt.keyCode == KeyCode.RightCommand ||
                evt.keyCode == KeyCode.LeftControl ||
                evt.keyCode == KeyCode.RightControl)
            {
                m_ControlUnpressScheduleItem.ExecuteLater(100);
                m_ControlWasPressed = true;
                return;
            }

            if (evt.keyCode != KeyCode.S)
                return;

            if (!evt.modifiers.HasFlag(EventModifiers.Control) &&
                !evt.modifiers.HasFlag(EventModifiers.Command) &&
                !m_ControlWasPressed)
                return;

            m_ControlWasPressed = false;

            m_Toolbar.SaveDocument(false);

            evt.StopPropagation();
        }

        void OnDelete(KeyDownEvent evt)
        {
            // HACK: This must be a bug. TextField leaks its key events to everyone!
            if (evt.leafTarget is ITextInputField)
                return;

            switch (evt.keyCode)
            {
                case KeyCode.Delete:
                case KeyCode.Backspace:
                    DeleteSelection();
                    evt.StopPropagation();
                    break;
                case KeyCode.Escape:
                    {
                        if (m_CutElement != null)
                        {
                            m_CutElement = null;
                            BuilderEditorUtility.SystemCopyBuffer = null;
                        }
                    }
                    break;
            }
        }

        public void DeleteSelection()
        {
            if (m_Selection.isEmpty)
                return;

            // Must save a copy of the selection here and then clear selection before
            // we delete the elements. Otherwise the selection clearing will fail
            // to remove the special selection objects because it won't be able
            // to query parent information of selected elements (they have already
            // been removed from the hierarchy).
            var selectionCopy = m_Selection.selection.ToList();
            m_Selection.ClearSelection(null, true);

            bool somethingWasDeleted = false;
            foreach (var element in selectionCopy)
                somethingWasDeleted |= DeleteElement(element);

            if (somethingWasDeleted)
                JustNotify();
        }

        public void PerformActionOnSelection(Action<VisualElement> preElementaction, Action preAction = null, Action postAction = null)
        {
            preAction?.Invoke();

            if (m_Selection.isEmpty)
                return;

            foreach (var element in m_Selection.selection)
                preElementaction(element);

            postAction?.Invoke();
        }

        public void DuplicateElement(VisualElement element)
        {
            CopyElement(element);
        }

        public void CutElement(VisualElement element)
        {
            CopyElement(element);
            m_CutElement = element;
        }

        public void CopyElement(VisualElement element)
        {
            var vea = element.GetVisualElementAsset();
            if (vea != null)
            {
                BuilderEditorUtility.SystemCopyBuffer =
                    VisualTreeAssetToUXML.GenerateUXML(m_PaneWindow.document.visualTreeAsset, null, vea);
                return;
            }

            var selector = element.GetStyleComplexSelector();
            if (selector != null)
            {
                var styleSheet = element.GetClosestStyleSheet();
                BuilderEditorUtility.SystemCopyBuffer =
                    StyleSheetToUss.ToUssString(styleSheet, selector);
                return;
            }
        }

        void PasteUXML(string copyBuffer)
        {
            var importer = new BuilderVisualTreeAssetImporter(); // Cannot be cached because the StyleBuilder never gets reset.
            importer.ImportXmlFromString(copyBuffer, out var pasteVta);

            VisualElementAsset parent = null;
            if (!m_Selection.isEmpty)
                parent = m_Selection.selection.First().parent?.GetVisualElementAsset();

            BuilderAssetUtilities.TransferAssetToAsset(m_PaneWindow.document, parent, pasteVta);
            m_PaneWindow.document.AddStyleSheetsToAllRootElements();

            var selectionParentId = parent?.id ?? m_PaneWindow.document.visualTreeAsset.GetRootUXMLElementId();
            VisualElementAsset newSelectedItem = pasteVta.templateAssets.FirstOrDefault(tpl => tpl.parentId == selectionParentId);
            if (newSelectedItem == null)
                newSelectedItem = pasteVta.visualElementAssets.FirstOrDefault(asset => asset.parentId == selectionParentId);

            m_Selection.ClearSelection(null);
            newSelectedItem.Select();

            ScriptableObject.DestroyImmediate(pasteVta);
        }

        void PasteUSS(string copyBuffer)
        {
            // Paste does nothing if document has no stylesheets.
            var mainStyleSheet = m_PaneWindow.document.activeStyleSheet;
            if (mainStyleSheet == null)
                return;

            var pasteStyleSheet = StyleSheetUtilities.CreateInstance();
            var importer = new BuilderStyleSheetImporter(); // Cannot be cached because the StyleBuilder never gets reset.
            importer.Import(pasteStyleSheet, copyBuffer);

            BuilderAssetUtilities.TransferAssetToAsset(m_PaneWindow.document, mainStyleSheet, pasteStyleSheet);

            m_Selection.ClearSelection(null);
            var scs = mainStyleSheet.complexSelectors.Last();
            BuilderAssetUtilities.AddStyleComplexSelectorToSelection(m_PaneWindow.document, mainStyleSheet, scs);

            ScriptableObject.DestroyImmediate(pasteStyleSheet);
        }

        public void Paste()
        {
            var copyBuffer = BuilderEditorUtility.SystemCopyBuffer;

            if (string.IsNullOrEmpty(copyBuffer))
                return;

            var trimmedBuffer = copyBuffer.Trim();
            if (trimmedBuffer.StartsWith("<") && trimmedBuffer.EndsWith(">"))
                PasteUXML(copyBuffer);
            else if (trimmedBuffer.EndsWith("}"))
                PasteUSS(copyBuffer);
            else // Unknown string.
                return;

            if (m_CutElement != null)
            {
                DeleteElement(m_CutElement);
                m_CutElement = null;
                BuilderEditorUtility.SystemCopyBuffer = null;
            }

            m_PaneWindow.OnEnableAfterAllSerialization();

            // TODO: ListView bug. Does not refresh selection pseudo states after a
            // call to Refresh().
            m_PaneWindow.rootVisualElement.schedule.Execute(() =>
            {
                var currentlySelectedItem = m_Selection.selection.FirstOrDefault();
                if(currentlySelectedItem != null)
                    m_Selection.Select(null, currentlySelectedItem);
            }).ExecuteLater(200);

            m_Selection.NotifyOfHierarchyChange();
        }

        bool DeleteElement(VisualElement element)
        {
            if (BuilderSharedStyles.IsSelectorsContainerElement(element) ||
                BuilderSharedStyles.IsStyleSheetElement(element) ||
                BuilderSharedStyles.IsDocumentElement(element) ||
                !element.IsLinkedToAsset())
                return false;

            if (BuilderSharedStyles.IsSelectorElement(element))
            {
                var styleSheet = element.GetClosestStyleSheet();
                Undo.RegisterCompleteObjectUndo(
                    styleSheet, BuilderConstants.DeleteSelectorUndoMessage);

                var selectorStr = BuilderSharedStyles.GetSelectorString(element);
                styleSheet.RemoveSelector(selectorStr);

                element.RemoveFromHierarchy();
                m_Selection.NotifyOfHierarchyChange();

                return true;
            }

            return DeleteElementFromVisualTreeAsset(element);
        }

        bool DeleteElementFromVisualTreeAsset(VisualElement element)
        {
            var vea = element.GetVisualElementAsset();
            if (vea == null)
                return false;

            // Before 2020.1, the only way to attach a StyleSheet to a UXML document was via a <Style>
            // tag as a child of an element tag. This meant that if there were no elements in the document,
            // there cannot be any StyleSheets attached to it. Therefore, we need to warn the user when
            // deleting the last element in the document that they will lose the list of attached StyleSheets
            // as well. This is something that can be undone via undo so it's not terrible if they say "Yes"
            // accidentally.
            //
            // 2020.1 adds support for the global <Style> tag so this limitation is lifted. However, the
            // UI Builder does not yet support the global <Style> tag. Plus, even when support to the
            // UI Builder is added, we still need to maintain support for 2019.3 via this logic.
            if (!(vea is TemplateAsset) &&
                m_PaneWindow.document.firstStyleSheet != null &&
                m_PaneWindow.document.visualTreeAsset.WillBeEmptyIfRemovingOne())
            {
                var continueDeletion = BuilderDialogsUtility.DisplayDialog(
                    BuilderConstants.DeleteLastElementDialogTitle,
                    BuilderConstants.DeleteLastElementDialogMessage,
                    "Yes", "Cancel");
                if (!continueDeletion)
                    return false;

                BuilderAssetUtilities.DeleteElementFromAsset(m_PaneWindow.document, element);

                m_PaneWindow.OnEnableAfterAllSerialization();
            }
            else
            {
                BuilderAssetUtilities.DeleteElementFromAsset(m_PaneWindow.document, element);

                element.RemoveFromHierarchy();
                m_Selection.NotifyOfHierarchyChange();
            }

            return true;
        }

        public void ClearCopyBuffer()
        {
            BuilderEditorUtility.SystemCopyBuffer = null;
        }

        public void ClearSelectionNotify()
        {
            m_Selection.ClearSelection(null);
            m_Selection.NotifyOfHierarchyChange(null);
            m_Selection.NotifyOfStylingChange(null);
        }

        public void JustNotify()
        {
            m_Selection.NotifyOfHierarchyChange(null);
            m_Selection.NotifyOfStylingChange(null);
        }
    }
}