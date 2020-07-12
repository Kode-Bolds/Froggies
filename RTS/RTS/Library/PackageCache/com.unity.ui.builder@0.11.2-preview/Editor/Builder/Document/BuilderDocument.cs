using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEditor;
using System;
using System.IO;

namespace Unity.UI.Builder
{
    class BuilderDocument : ScriptableObject, IBuilderSelectionNotifier, ISerializationCallbackReceiver, IBuilderAssetPostprocessor
    {
        //
        // Types
        //

        public enum CanvasTheme
        {
            Default,
            Dark,
            Light,
            Runtime
        }

        //
        // Serialized Data
        //

        [SerializeField]
        CanvasTheme m_CurrentCanvasTheme;

        [SerializeField]
        bool m_CodePreviewVisible = true;

        [SerializeField]
        List<BuilderDocumentOpenUXML> m_OpenUXMLFiles = new List<BuilderDocumentOpenUXML>();

        [SerializeField]
        int m_ActiveOpenUXMLFileIndex = 0;

        //
        // Unserialized Data
        //

        readonly WeakReference<IBuilderViewportWindow> m_PrimaryViewportWindow = new WeakReference<IBuilderViewportWindow>(null);

        readonly List<BuilderPaneWindow> m_RegisteredWindows = new List<BuilderPaneWindow>();

        //
        // Getters
        //

        BuilderDocumentOpenUXML activeOpenUXMLFile
        {
            get
            {
                // We should always have one open UXML, even if unsaved.
                if (m_OpenUXMLFiles.Count == 0)
                {
                    m_OpenUXMLFiles.Add(new BuilderDocumentOpenUXML());
                    m_ActiveOpenUXMLFileIndex = 0;
                }

                return m_OpenUXMLFiles[m_ActiveOpenUXMLFileIndex];
            }
        }

        public StyleSheet activeStyleSheet => activeOpenUXMLFile.activeStyleSheet;
        public BuilderDocumentSettings settings => activeOpenUXMLFile.settings;
        public string uxmlFileName => activeOpenUXMLFile.uxmlFileName;
        public string uxmlOldPath => activeOpenUXMLFile.uxmlOldPath;
        public string uxmlPath => activeOpenUXMLFile.uxmlPath;
        public List<string> ussPaths => activeOpenUXMLFile.ussPaths;
        public VisualTreeAsset visualTreeAsset => activeOpenUXMLFile.visualTreeAsset;
        public StyleSheet firstStyleSheet => activeOpenUXMLFile.firstStyleSheet;
        public List<BuilderDocumentOpenUSS> openUSSFiles => activeOpenUXMLFile.openUSSFiles;

        //
        // Getter/Setters
        //

        public IBuilderViewportWindow primaryViewportWindow
        {
            get
            {
                if (m_PrimaryViewportWindow == null)
                    return null;

                IBuilderViewportWindow window;
                bool isReferenceValid = m_PrimaryViewportWindow.TryGetTarget(out window);
                if (!isReferenceValid)
                    return null;

                return window;
            }
            private set
            {
                m_PrimaryViewportWindow.SetTarget(value);
            }
        }

        public bool hasUnsavedChanges
        {
            get
            {
                bool hasUnsavedChanges = false;
                foreach (var openUXMLFile in m_OpenUXMLFiles)
                {
                    hasUnsavedChanges |= openUXMLFile.hasUnsavedChanges;
                    if (hasUnsavedChanges)
                        break;
                }
                return hasUnsavedChanges;
            }
            set
            {
                foreach (var openUXMLFile in m_OpenUXMLFiles)
                    openUXMLFile.hasUnsavedChanges = value;
            }
        }

        public CanvasTheme currentCanvasTheme => m_CurrentCanvasTheme;

        public void ChangeDocumentTheme(VisualElement documentElement, CanvasTheme canvasTheme)
        {
            m_CurrentCanvasTheme = canvasTheme;
            RefreshStyle(documentElement);
        }

        public bool codePreviewVisible
        {
            get { return m_CodePreviewVisible; }
            set { m_CodePreviewVisible = value; }
        }

        public float viewportZoomScale
        {
            get => activeOpenUXMLFile.viewportZoomScale;
            set { activeOpenUXMLFile.viewportZoomScale = value; }
        }

        public Vector2 viewportContentOffset
        {
            get => activeOpenUXMLFile.viewportContentOffset;
            set { activeOpenUXMLFile.viewportContentOffset = value; }
        }

        //
        // Initialize / Construct / Enable / Clear
        //

        bool UnityWantsToQuit() => CheckForUnsavedChanges();

        public BuilderDocument()
        {
            hasUnsavedChanges = false;
            EditorApplication.wantsToQuit += UnityWantsToQuit;
            activeOpenUXMLFile.Clear();
        }

        void OnEnable()
        {
            BuilderAssetPostprocessor.Register(this);
        }

        void OnDisable()
        {
            BuilderAssetPostprocessor.Unregister(this);
        }

        public static BuilderDocument CreateInstance()
        {
            var newDoc = ScriptableObject.CreateInstance<BuilderDocument>();
            newDoc.hideFlags = HideFlags.DontUnloadUnusedAsset | HideFlags.DontSaveInEditor;
            newDoc.name = "BuilderDocument";
            newDoc.LoadFromDisk();
            return newDoc;
        }

        //
        // Window Registrations
        //

        public void RegisterWindow(BuilderPaneWindow window)
        {
            if (window == null || m_RegisteredWindows.Contains(window))
                return;

            m_RegisteredWindows.Add(window);

            if (window is IBuilderViewportWindow)
            {
                primaryViewportWindow = window as IBuilderViewportWindow;
                BroadcastChange();
            }
        }

        public void UnregisterWindow(BuilderPaneWindow window)
        {
            if (window == null)
                return;

            var removed = m_RegisteredWindows.Remove(window);
            if (!removed)
                return;

            if (window is IBuilderViewportWindow && primaryViewportWindow == window as IBuilderViewportWindow)
            {
                primaryViewportWindow = null;
                BroadcastChange();
            }
        }

        public void BroadcastChange()
        {
            foreach (var window in m_RegisteredWindows)
                window.PrimaryViewportWindowChanged();
        }

        //
        // Styles
        //

        public void RefreshStyle(VisualElement documentElement)
            => activeOpenUXMLFile.RefreshStyle(documentElement);

        public void MarkStyleSheetsDirty()
            => activeOpenUXMLFile.MarkStyleSheetsDirty();

        public void AddStyleSheetToDocument(StyleSheet styleSheet, string ussPath)
            => activeOpenUXMLFile.AddStyleSheetToDocument(styleSheet, ussPath);

        public void RemoveStyleSheetFromDocument(int ussIndex)
            => activeOpenUXMLFile.RemoveStyleSheetFromDocument(ussIndex);

        public void AddStyleSheetsToAllRootElements(string newUssPath = null, int newUssIndex = 0)
            => activeOpenUXMLFile.AddStyleSheetsToAllRootElements(newUssPath, newUssIndex);

        public void UpdateActiveStyleSheet(BuilderSelection selection, StyleSheet styleSheet, IBuilderSelectionNotifier source)
            => activeOpenUXMLFile.UpdateActiveStyleSheet(selection, styleSheet, source);

        public bool UpdateActiveStyleSheetFromSelection(BuilderSelection selection)
            => activeOpenUXMLFile.UpdateActiveStyleSheetFromSelection(selection);

        //
        // Save / Load
        //

        public bool SaveUnsavedChanges(string manualUxmlPath = null, bool isSaveAs = false)
            => activeOpenUXMLFile.SaveNewDocument(null, isSaveAs, out var needsFullRefresh, manualUxmlPath);

        public bool SaveNewDocument(
            VisualElement documentRootElement, bool isSaveAs,
            out bool needsFullRefresh,
            string manualUxmlPath = null)
        {
            var result = activeOpenUXMLFile.SaveNewDocument(
                documentRootElement, isSaveAs,
                out needsFullRefresh,
                manualUxmlPath);

            SaveToDisk();

            return result;
        }

        public bool CheckForUnsavedChanges(bool assetModifiedExternally = false)
            => activeOpenUXMLFile.CheckForUnsavedChanges(assetModifiedExternally);

        public void NewDocument(VisualElement documentRootElement)
        {
            activeOpenUXMLFile.NewDocument(documentRootElement);
            SaveToDisk();
        }

        public void LoadDocument(VisualTreeAsset visualTreeAsset, VisualElement documentElement)
        {
            activeOpenUXMLFile.LoadDocument(visualTreeAsset, documentElement);
            SaveToDisk();
        }

        //
        // Asset Change Detection
        //

        public void OnPostProcessAsset(string assetPath)
            => activeOpenUXMLFile.OnPostProcessAsset(assetPath);

        //
        // Selection
        //

        public void SelectionChanged()
        {
            // Selection changes don't affect the document.
        }

        public void HierarchyChanged(VisualElement element, BuilderHierarchyChangeType changeType)
            => activeOpenUXMLFile.HierarchyChanged(element);

        public void StylingChanged(List<string> styles)
            => activeOpenUXMLFile.StylingChanged();

        //
        // Serialization
        //

        public void OnAfterBuilderDeserialize(VisualElement documentElement)
            => activeOpenUXMLFile.OnAfterBuilderDeserialize(documentElement);

        public void OnBeforeSerialize()
        {
            // Do nothing.
        }

        public void OnAfterDeserialize()
            => activeOpenUXMLFile.OnAfterDeserialize();

        void LoadFromDisk()
        {
            var path = BuilderConstants.BuilderDocumentDiskJsonFileAbsolutePath;

            if (!File.Exists(path))
                return;

            var json = File.ReadAllText(path);
            EditorJsonUtility.FromJsonOverwrite(json, this);

            // Very important we convert asset references to paths here after a restore.
            foreach (var openUXMLFile in m_OpenUXMLFiles)
                openUXMLFile.OnAfterLoadFromDisk();
        }

        public void SaveToDisk()
        {
            var json = EditorJsonUtility.ToJson(this, true);

            var folderPath = BuilderConstants.BuilderDocumentDiskJsonFolderAbsolutePath;
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            File.WriteAllText(BuilderConstants.BuilderDocumentDiskJsonFileAbsolutePath, json);
        }

        public void SaveSettingsToDisk() => activeOpenUXMLFile.settings.SaveSettingsToDisk();
    }
}
