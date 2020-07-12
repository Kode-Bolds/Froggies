using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    internal enum BuilderSelectionType
    {
        Nothing,
        Element,
        StyleSheet,
        StyleSelector,
        ElementInTemplateInstance,
        VisualTreeAsset
    }

    [Flags]
    internal enum BuilderHierarchyChangeType
    {
        ChildrenAdded = 1 << 0,
        ChildrenRemoved = 1 << 1,
        Name = 1 << 2,
        ClassList = 1 << 3,
        InlineStyle = 1 << 4,

        All = ~0
    }

    internal interface IBuilderSelectionNotifier
    {
        void SelectionChanged();
        void HierarchyChanged(VisualElement element, BuilderHierarchyChangeType changeType);
        void StylingChanged(List<string> styles);
    }

    internal class BuilderSelection
    {
#if UNITY_2019_3_OR_NEWER
        static readonly StylePropertyReader s_StylePropertyReader = new StylePropertyReader();
#endif
        List<IBuilderSelectionNotifier> m_Notifiers;

        IBuilderSelectionNotifier m_CurrentNotifier;
        List<string> m_CurrentStyleList;
        Action m_NextPostStylingAction;

        VisualElement m_Root;
        BuilderPaneWindow m_PaneWindow;
        VisualElement m_DocumentElement;
        VisualElement m_DummyElementForStyleChangeNotifications;

        public BuilderSelectionType selectionType
        {
            get
            {
                if (m_Selection.Count == 0)
                    return BuilderSelectionType.Nothing;

                var selectedElement = m_Selection[0];

                if (BuilderSharedStyles.IsDocumentElement(selectedElement))
                    return BuilderSelectionType.VisualTreeAsset;
                if (BuilderSharedStyles.IsSelectorElement(selectedElement))
                    return BuilderSelectionType.StyleSelector;
                if (BuilderSharedStyles.IsStyleSheetElement(selectedElement))
                    return BuilderSelectionType.StyleSheet;
                if (selectedElement.GetVisualElementAsset() != null)
                    return BuilderSelectionType.Element;

                // If we got here, it means we selected element inside
                // a child Template Instance in the main doc.
                return BuilderSelectionType.ElementInTemplateInstance;
            }
        }

        List<VisualElement> m_Selection;
        public IEnumerable<VisualElement> selection
        {
            get { return m_Selection; }
        }

        public VisualElement documentElement
        {
            get { return m_DocumentElement; }
            set { m_DocumentElement = value; }
        }

        public bool isEmpty { get { return m_Selection.Count == 0; } }

        public BuilderSelection(VisualElement root, BuilderPaneWindow paneWindow)
        {
            m_Notifiers = new List<IBuilderSelectionNotifier>();
            m_Selection = new List<VisualElement>();

            m_Root = root;
            m_PaneWindow = paneWindow;

            m_DummyElementForStyleChangeNotifications = new VisualElement();
            m_DummyElementForStyleChangeNotifications.name = "unity-dummy-element-for-style-change-notifications";
            m_DummyElementForStyleChangeNotifications.style.position = Position.Absolute;
            m_DummyElementForStyleChangeNotifications.style.top = -1000;
            m_DummyElementForStyleChangeNotifications.style.left = -1000;
            m_DummyElementForStyleChangeNotifications.style.width = 1;
            m_DummyElementForStyleChangeNotifications.RegisterCallback<GeometryChangedEvent>(AfterPanelUpdaterChange);
            m_Root.Add(m_DummyElementForStyleChangeNotifications);
        }

        public void AssignNotifiers(IEnumerable<IBuilderSelectionNotifier> notifiers)
        {
            m_Notifiers.Clear();
            foreach (var notifier in notifiers)
                m_Notifiers.Add(notifier);
        }

        public void AddNotifier(IBuilderSelectionNotifier notifier)
        {
            if (!m_Notifiers.Contains(notifier))
                m_Notifiers.Add(notifier);
        }

        public void RemoveNotifier(IBuilderSelectionNotifier notifier)
        {
            m_Notifiers.Remove(notifier);
        }

        public void ForceReselection(IBuilderSelectionNotifier source = null)
        {
            NotifyOfSelectionChange(source);
        }

        public void Select(IBuilderSelectionNotifier source, VisualElement ve)
        {
            if (ve == null)
                return;

            foreach (var sel in m_Selection)
            {
                if (sel == null)
                    continue;

                BuilderAssetUtilities.RemoveElementFromSelectionInAsset(m_PaneWindow.document, sel);
            }

            m_Selection.Clear();

            m_Selection.Add(ve);
            BuilderAssetUtilities.AddElementToSelectionInAsset(m_PaneWindow.document, ve);

            NotifyOfSelectionChange(source);
        }

        public void AddToSelection(IBuilderSelectionNotifier source, VisualElement ve, bool undo = true)
        {
            if (ve == null)
                return;

            m_Selection.Add(ve);

            if (undo)
                BuilderAssetUtilities.AddElementToSelectionInAsset(m_PaneWindow.document, ve);

            NotifyOfSelectionChange(source);
        }

        public void RemoveFromSelection(IBuilderSelectionNotifier source, VisualElement ve)
        {
            m_Selection.Remove(ve);
            BuilderAssetUtilities.RemoveElementFromSelectionInAsset(m_PaneWindow.document, ve);

            NotifyOfSelectionChange(source);
        }

        public void ClearSelection(IBuilderSelectionNotifier source, bool undo = true)
        {
            if (isEmpty)
                return;

            if (undo)
                foreach (var sel in m_Selection)
                    BuilderAssetUtilities.RemoveElementFromSelectionInAsset(m_PaneWindow.document, sel);

            m_Selection.Clear();

            NotifyOfSelectionChange(source);
        }

        public void RestoreSelectionFromDocument(VisualElement sharedStylesAndDocumentElement)
        {
            ClearSelection(null, false);

            var selectedElements = sharedStylesAndDocumentElement.FindSelectedElements();
            foreach (var selectedElement in selectedElements)
                AddToSelection(null, selectedElement, false);
        }

        public void NotifyOfHierarchyChange(
            IBuilderSelectionNotifier source = null,
            VisualElement element = null,
            BuilderHierarchyChangeType changeType = BuilderHierarchyChangeType.All)
        {
            if (m_Notifiers == null || m_Notifiers.Count == 0)
                return;

            VisualElementAsset vea = element?.GetVisualElementAsset();
            if (vea != null && vea.ruleIndex >= 0 && changeType.HasFlag(BuilderHierarchyChangeType.InlineStyle))
            {
                var vta = m_PaneWindow.document.visualTreeAsset;
                var rule = vta.GetOrCreateInlineStyleRule(vea);

#if UNITY_2020_1_OR_NEWER
                element.SetInlineRule(vta.inlineSheet, rule);

                // Need to enforce this specific style is updated.
                element.IncrementVersion(VersionChangeType.Opacity | VersionChangeType.Overflow);

#elif UNITY_2019_3_OR_NEWER
                var stylesData = new UnityEngine.UIElements.StyleSheets.VisualElementStylesData(false);
                element.m_Style = stylesData;
                
                s_StylePropertyReader.SetInlineContext(vta.inlineSheet, rule, vea.ruleIndex);
                stylesData.ApplyProperties(s_StylePropertyReader, null);

                // Need to enforce this specific style is updated.
                element.IncrementVersion(VersionChangeType.Opacity | VersionChangeType.Overflow);
#else
                var stylesData = new UnityEngine.UIElements.StyleSheets.VisualElementStylesData(false);
                element.m_Style = stylesData;

                var propIds = UnityEngine.UIElements.StyleSheets.StyleSheetCache.GetPropertyIDs(vta.inlineSheet, vea.ruleIndex);
                element.specifiedStyle.ApplyRule(vta.inlineSheet, Int32.MaxValue, rule, propIds);

                // Need to enforce this specific style is updated.
                element.IncrementVersion(VersionChangeType.Overflow);
#endif
            }

            if (m_DocumentElement != null)
                m_PaneWindow.document.RefreshStyle(m_DocumentElement);

            // This is so anyone interested can refresh their use of this UXML with
            // the latest (unsaved to disk) changes.
            EditorUtility.SetDirty(m_PaneWindow.document.visualTreeAsset);

            foreach (var notifier in m_Notifiers)
                if (notifier != source)
                    notifier.HierarchyChanged(element, changeType);
        }

        public void NotifyOfStylingChange(IBuilderSelectionNotifier source = null, List<string> styles = null)
        {
            if (m_Notifiers == null || m_Notifiers.Count == 0)
                return;

            if (m_DocumentElement != null)
                m_PaneWindow.document.RefreshStyle(m_DocumentElement);

            m_CurrentNotifier = source;
            m_CurrentStyleList = styles;
            QueueUpPostPanelUpdaterChangeAction(NotifyOfStylingChangePostStylingUpdate);
        }

        void NotifyOfSelectionChange(IBuilderSelectionNotifier source)
        {
            if (m_Notifiers == null || m_Notifiers.Count == 0)
                return;

            if (m_DocumentElement != null)
                m_PaneWindow.document.RefreshStyle(m_DocumentElement);

            foreach (var notifier in m_Notifiers)
                if (notifier != source)
                    notifier.SelectionChanged();
        }

        public void NotifyOfStylingChangePostStylingUpdate()
        {
            // This is so anyone interested can refresh their use of this USS with
            // the latest (unsaved to disk) changes.
            //RetainedMode.FlagStyleSheetChange(); // Works but TOO SLOW.
            m_PaneWindow.document.MarkStyleSheetsDirty();

            foreach (var notifier in m_Notifiers)
                if (notifier != m_CurrentNotifier)
                    notifier.StylingChanged(m_CurrentStyleList);

            m_CurrentNotifier = null;
            m_CurrentStyleList = null;
        }

        void QueueUpPostPanelUpdaterChangeAction(Action action)
        {
            m_NextPostStylingAction = action;
            if (m_DummyElementForStyleChangeNotifications.resolvedStyle.width > 0)
                m_DummyElementForStyleChangeNotifications.style.width = -1;
            else
                m_DummyElementForStyleChangeNotifications.style.width = 1;
        }

        void AfterPanelUpdaterChange(GeometryChangedEvent evt)
        {
            if (m_NextPostStylingAction == null)
                return;

            m_NextPostStylingAction();

            m_NextPostStylingAction = null;
        }

        public bool hasUnsavedChanges
        {
            get { return m_PaneWindow.document.hasUnsavedChanges; }
            private set { m_PaneWindow.document.hasUnsavedChanges = value; }
        }
        
        public void ResetUnsavedChanges()
        {
            hasUnsavedChanges = false;
        }
    }
}
