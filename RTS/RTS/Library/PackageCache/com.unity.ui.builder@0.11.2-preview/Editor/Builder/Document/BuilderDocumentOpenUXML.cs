using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEditor;
using System;
using System.IO;
using System.Linq;

namespace Unity.UI.Builder
{
    [Serializable]
    [ExtensionOfNativeClass]
    class BuilderDocumentOpenUXML
    {
        //
        // Serialized Data
        //

        [SerializeField]
        List<BuilderDocumentOpenUSS> m_OpenUSSFiles = new List<BuilderDocumentOpenUSS>();

        [SerializeField]
        VisualTreeAsset m_VisualTreeAssetBackup;

        [SerializeField]
        string m_OpenendVisualTreeAssetOldPath;

        [SerializeField]
        VisualTreeAsset m_VisualTreeAsset;

        [SerializeField]
        StyleSheet m_ActiveStyleSheet;

        [SerializeField]
        BuilderDocumentSettings m_Settings;

        //
        // Unserialized Data
        //

        bool m_HasUnsavedChanges = false;
        bool m_DocumentBeingSavedExplicitly = false;

        //
        // Getters
        //

        public StyleSheet activeStyleSheet
        {
            get
            {
                if (m_ActiveStyleSheet == null)
                    m_ActiveStyleSheet = firstStyleSheet;

                return m_ActiveStyleSheet;
            }
        }

        public BuilderDocumentSettings settings
        {
            get
            {
                m_Settings = BuilderDocumentSettings.CreateOrLoadSettingsObject(m_Settings, uxmlPath);
                return m_Settings;
            }
        }

        public string uxmlFileName
        {
            get
            {
                var path = uxmlPath;
                if (path == null)
                    return null;

                var fileName = Path.GetFileName(path);
                return fileName;
            }
        }

        public string uxmlOldPath
        {
            get { return m_OpenendVisualTreeAssetOldPath; }
        }

        public string uxmlPath
        {
            get { return AssetDatabase.GetAssetPath(m_VisualTreeAsset); }
        }

        public List<string> ussPaths
        {
            get
            {
                var paths = new List<string>();
                for (int i = 0; i < m_OpenUSSFiles.Count; ++i)
                    paths.Add(m_OpenUSSFiles[i].AssetPath);
                return paths;
            }
        }

        public VisualTreeAsset visualTreeAsset
        {
            get
            {
                if (m_VisualTreeAsset == null)
                    m_VisualTreeAsset = VisualTreeAssetUtilities.CreateInstance();

                return m_VisualTreeAsset;
            }
        }

        public StyleSheet firstStyleSheet
        {
            get { return m_OpenUSSFiles.Count > 0 ? m_OpenUSSFiles[0].Sheet : null; }
        }

        public List<BuilderDocumentOpenUSS> openUSSFiles => m_OpenUSSFiles;

        //
        // Getter/Setters
        //

        public bool hasUnsavedChanges
        {
            get { return m_HasUnsavedChanges; }
            set
            {
                if (value == m_HasUnsavedChanges)
                    return;

                m_HasUnsavedChanges = value;
            }
        }

        public float viewportZoomScale
        {
            get
            {
                return settings.ZoomScale;
            }
            set
            {
                settings.ZoomScale = value;
                settings.SaveSettingsToDisk();
            }
        }

        public Vector2 viewportContentOffset
        {
            get
            {
                return settings.PanOffset;
            }
            set
            {
                settings.PanOffset = value;
                settings.SaveSettingsToDisk();
            }
        }

        //
        // Initialize / Construct / Enable / Clear
        //

        public void Clear()
        {
            ClearUndo();

            RestoreAssetsFromBackup();

            ClearBackups();
            m_OpenendVisualTreeAssetOldPath = string.Empty;
            m_ActiveStyleSheet = null;

            if (m_VisualTreeAsset != null)
            {
                if (!AssetDatabase.Contains(m_VisualTreeAsset))
                    m_VisualTreeAsset.Destroy();

                m_VisualTreeAsset = null;
            }

            m_OpenUSSFiles.Clear();

            m_Settings = null;
        }

        //
        // Styles
        //

        public void RefreshStyle(VisualElement documentElement)
        {
            foreach (var openUSS in m_OpenUSSFiles)
            {
                var sheet = openUSS.Sheet;
                if (sheet == null)
                    continue;

                if (!documentElement.styleSheets.Contains(sheet))
                {
                    documentElement.styleSheets.Clear();

                    foreach (var innerOpenUSS in m_OpenUSSFiles)
                        if (innerOpenUSS.Sheet != null)
                            documentElement.styleSheets.Add(innerOpenUSS.Sheet);

                    break;
                }
            }

            StyleCache.ClearStyleCache();
            UnityEngine.UIElements.StyleSheets.StyleSheetCache.ClearCaches();
            foreach (var openUSS in m_OpenUSSFiles)
                openUSS.FixRuleReferences();
            documentElement.IncrementVersion((VersionChangeType)(-1));
        }

        public void MarkStyleSheetsDirty()
        {
            foreach (var openUSS in m_OpenUSSFiles)
                EditorUtility.SetDirty(openUSS.Sheet);
        }

        public void AddStyleSheetToDocument(StyleSheet styleSheet, string ussPath)
        {
            var newOpenUssFile = new BuilderDocumentOpenUSS();
            newOpenUssFile.Set(styleSheet, ussPath);
            m_OpenUSSFiles.Add(newOpenUssFile);

            AddStyleSheetsToAllRootElements();

            hasUnsavedChanges = true;
        }

        public void RemoveStyleSheetFromDocument(int ussIndex)
        {
            RemoveStyleSheetFromLists(ussIndex);

            AddStyleSheetsToAllRootElements();

            hasUnsavedChanges = true;
        }

        void AddStyleSheetsToRootAsset(VisualElementAsset rootAsset, string newUssPath = null, int newUssIndex = 0)
        {
            if (rootAsset.fullTypeName == BuilderConstants.SelectedVisualTreeAssetSpecialElementTypeName)
                return;

            rootAsset.ClearStyleSheets();

            for (int i = 0; i < m_OpenUSSFiles.Count; ++i)
            {
                var localUssPath = m_OpenUSSFiles[i].AssetPath;

                if (!string.IsNullOrEmpty(newUssPath) && i == newUssIndex)
                    localUssPath = newUssPath;

                if (string.IsNullOrEmpty(localUssPath))
                    continue;

#if UNITY_2019_3_OR_NEWER
                rootAsset.AddStyleSheet(m_OpenUSSFiles[i].Sheet);
#endif
                rootAsset.AddStyleSheetPath(localUssPath);
            }
        }

        public void AddStyleSheetsToAllRootElements(string newUssPath = null, int newUssIndex = 0)
        {
            foreach (var asset in visualTreeAsset.visualElementAssets)
            {
                if (!visualTreeAsset.IsRootElement(asset))
                    continue; // Not a root asset.

                AddStyleSheetsToRootAsset(asset, newUssPath, newUssIndex);
            }
        }

        void AddStyleSheetToRootIfNeeded(VisualElement element)
        {
            var rootElement = BuilderSharedStyles.GetDocumentRootLevelElement(element);
            if (rootElement == null)
                return;

            var rootAsset = rootElement.GetVisualElementAsset();
            if (rootAsset == null)
                return;

            AddStyleSheetsToRootAsset(rootAsset);
        }

        void RemoveStyleSheetFromLists(int ussIndex)
        {
            var openUSSFile = m_OpenUSSFiles[ussIndex];
            m_OpenUSSFiles.RemoveAt(ussIndex);
            openUSSFile.Clear();
        }

        public void UpdateActiveStyleSheet(BuilderSelection selection, StyleSheet styleSheet, IBuilderSelectionNotifier source)
        {
            if (m_ActiveStyleSheet == styleSheet)
                return;

            m_ActiveStyleSheet = styleSheet;
            selection.ForceReselection(source);
        }

        public bool UpdateActiveStyleSheetFromSelection(BuilderSelection selection)
        {
            var originalActiveStyleSheet = m_ActiveStyleSheet;
            if (m_ActiveStyleSheet == null)
            {
                m_ActiveStyleSheet = firstStyleSheet;
            }
            else
            {
                var selectedElement = selection.isEmpty ? null : selection.selection.First();
                if (selectedElement != null)
                {
                    if (BuilderSharedStyles.IsStyleSheetElement(selectedElement))
                        m_ActiveStyleSheet = selectedElement.GetStyleSheet();
                    else if (BuilderSharedStyles.IsSelectorElement(selectedElement))
                        m_ActiveStyleSheet = selectedElement.GetClosestStyleSheet();
                }
            }
            return originalActiveStyleSheet != m_ActiveStyleSheet;
        }

        //
        // Save / Load
        //

        public bool SaveUnsavedChanges(string manualUxmlPath = null, bool isSaveAs = false)
        {
            return SaveNewDocument(null, isSaveAs, out var needsFullRefresh, manualUxmlPath);
        }

        public bool SaveNewDocument(
            VisualElement documentRootElement, bool isSaveAs,
            out bool needsFullRefresh,
            string manualUxmlPath = null)
        {
            needsFullRefresh = false;

            ClearUndo();

            // Re-use or ask the user for the UXML path.
            var newUxmlPath = uxmlPath;
            if (string.IsNullOrEmpty(newUxmlPath) || isSaveAs)
            {
                if (!string.IsNullOrEmpty(manualUxmlPath))
                {
                    newUxmlPath = manualUxmlPath;
                }
                else
                {
                    newUxmlPath = BuilderDialogsUtility.DisplaySaveFileDialog("Save UXML", null, null, "uxml");
                    if (newUxmlPath == null) // User cancelled the save dialog.
                        return false;
                }
            }

            // Save USS files.
            foreach (var openUSSFile in m_OpenUSSFiles)
                openUSSFile.SaveToDisk(visualTreeAsset);

            { // Save UXML file.
                // Need to save a backup before the AssetDatabase.Refresh().
                if (m_VisualTreeAssetBackup == null)
                    m_VisualTreeAssetBackup = m_VisualTreeAsset.DeepCopy();
                else
                    m_VisualTreeAsset.DeepOverwrite(m_VisualTreeAssetBackup);
                WriteUXMLToFile(newUxmlPath);
            }

            // Once we wrote all the files to disk, we refresh the DB and reload
            // the files from the AssetDatabase.
            m_DocumentBeingSavedExplicitly = true;
            try
            {
                AssetDatabase.Refresh();
            }
            finally
            {
                m_DocumentBeingSavedExplicitly = false;
            }

            // Check if any USS assets have changed reload them.
            foreach (var openUSSFile in m_OpenUSSFiles)
                needsFullRefresh |= openUSSFile.PostSaveToDiskChecksAndFixes();
            { // Check if the UXML asset has changed and reload it.
                m_VisualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(newUxmlPath);
                needsFullRefresh |= m_VisualTreeAsset != m_VisualTreeAssetBackup;
            }

            if (needsFullRefresh)
            {
                // Copy previous document settings.
                if (m_Settings != null)
                {
                    m_Settings.UxmlGuid = AssetDatabase.AssetPathToGUID(newUxmlPath);
                    m_Settings.UxmlPath = newUxmlPath;
                    m_Settings.SaveSettingsToDisk();
                }

                { // Fix up UXML asset.
                    // To get all the selection markers into the new assets.
                    m_VisualTreeAssetBackup.DeepOverwrite(m_VisualTreeAsset);

                    // Reset asset name.
                    m_VisualTreeAsset.name = Path.GetFileNameWithoutExtension(newUxmlPath);

                    m_VisualTreeAsset.ConvertAllAssetReferencesToPaths();
                    m_OpenendVisualTreeAssetOldPath = newUxmlPath;
                }

                if (documentRootElement != null)
                    ReloadDocumentToCanvas(documentRootElement);
            }

            hasUnsavedChanges = false;

            return true;
        }

        public bool CheckForUnsavedChanges(bool assetModifiedExternally = false)
        {
            if (!hasUnsavedChanges)
                return true;

            if (assetModifiedExternally)
            {
                // TODO: Nothing can be done here yet, other than telling the user
                // what just happened. Adding the ability to save unsaved changes
                // after a file has been modified externally will require some
                // major changes to the document flow.
                BuilderDialogsUtility.DisplayDialog(
                    BuilderConstants.SaveDialogExternalChangesPromptTitle,
                    BuilderConstants.SaveDialogExternalChangesPromptMessage);

                return true;
            }
            else
            {
                var option = BuilderDialogsUtility.DisplayDialogComplex(
                    BuilderConstants.SaveDialogSaveChangesPromptTitle,
                    BuilderConstants.SaveDialogSaveChangesPromptMessage,
                    BuilderConstants.DialogSaveActionOption,
                    BuilderConstants.DialogCancelOption,
                    BuilderConstants.DialogDontSaveActionOption);

                switch (option)
                {
                    // Save
                    case 0:
                        return SaveUnsavedChanges();
                    // Cancel
                    case 1:
                        return false;
                    // Don't Save
                    case 2:
                        RestoreAssetsFromBackup();
                        return true;
                }
            }

            return true;
        }

        public void NewDocument(VisualElement documentRootElement)
        {
            Clear();

            // Re-run initializations and setup, even though there's nothing to clone.
            ReloadDocumentToCanvas(documentRootElement);

            hasUnsavedChanges = false;
        }

        public void LoadDocument(VisualTreeAsset visualTreeAsset, VisualElement documentElement)
        {
            NewDocument(documentElement);

            if (visualTreeAsset == null)
                return;

            m_VisualTreeAssetBackup = visualTreeAsset.DeepCopy();
            m_VisualTreeAsset = visualTreeAsset;
            m_VisualTreeAsset.ConvertAllAssetReferencesToPaths();

            // Load styles.
            var styleSheetsUsed = m_VisualTreeAsset.GetAllReferencedStyleSheets();
            for (int i = 0; i < styleSheetsUsed.Count; ++i)
                AddStyleSheetToDocument(styleSheetsUsed[i], null);

            m_OpenendVisualTreeAssetOldPath = AssetDatabase.GetAssetPath(m_VisualTreeAsset);

            hasUnsavedChanges = false;

            m_Settings = BuilderDocumentSettings.CreateOrLoadSettingsObject(m_Settings, uxmlPath);

            ReloadDocumentToCanvas(documentElement);
        }

        //
        // Asset Change Detection
        //

        public void OnPostProcessAsset(string assetPath)
        {
            if (m_DocumentBeingSavedExplicitly)
                return;

            var newVisualTreeAsset = m_VisualTreeAsset;
            if (assetPath == uxmlOldPath)
            {
                newVisualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(assetPath);
            }
            else
            {
                bool found = false;
                foreach (var openUSSFile in m_OpenUSSFiles)
                    if (found = openUSSFile.CheckPostProcessAssetIfFileChanged(assetPath))
                        break;

                if (!found)
                    return;
            }

            var builderWindow = Builder.ActiveWindow;
            if (builderWindow == null)
                builderWindow = Builder.ShowWindow();

            // LoadDocument() will call Clear() which will try to restore from Backup().
            // If we don't clear the Backups here, they will overwrite the newly post-processed
            // and re-imported asset we detected here.
            ClearBackups();

            builderWindow.toolbar.LoadDocument(newVisualTreeAsset, true);
        }

        //
        // Selection
        //

        public void HierarchyChanged(VisualElement element)
        {
            hasUnsavedChanges = true;

            if (element != null) // Add StyleSheet to this element's root element.
                AddStyleSheetToRootIfNeeded(element);
            else // Add StyleSheet to all root elements since one might match this new selector.
                AddStyleSheetsToAllRootElements();
        }

        public void StylingChanged()
        {
            hasUnsavedChanges = true;

            // Make sure active stylesheet is still in the document.
            foreach (var openUSSFile in m_OpenUSSFiles)
            {
                if (m_ActiveStyleSheet == openUSSFile.Sheet)
                    continue;

                m_ActiveStyleSheet = firstStyleSheet;
                break;
            }
        }

        //
        // Serialization
        //

        public void OnAfterBuilderDeserialize(VisualElement documentElement)
        {
            // Refresh StyleSheets.
            m_ActiveStyleSheet = null;
            var styleSheetsUsed = m_VisualTreeAsset.GetAllReferencedStyleSheets();
            while (m_OpenUSSFiles.Count < styleSheetsUsed.Count)
                m_OpenUSSFiles.Add(new BuilderDocumentOpenUSS());
            for (int i = 0; i < styleSheetsUsed.Count; ++i)
            {
                if (m_OpenUSSFiles[i].Sheet == styleSheetsUsed[i])
                    continue;

                m_OpenUSSFiles[i].Set(styleSheetsUsed[i], null);
            }
            while (m_OpenUSSFiles.Count > styleSheetsUsed.Count)
            {
                var lastIndex = m_OpenUSSFiles.Count - 1;
                RemoveStyleSheetFromLists(lastIndex);
            }

            // Fix unserialized rule references in Selectors in StyleSheets.
            // VTA.inlineSheet only has Rules so it does not need this fix.
            foreach (var openUSSFile in m_OpenUSSFiles)
                openUSSFile.FixRuleReferences();

            ReloadDocumentToCanvas(documentElement);
        }

        public void OnAfterDeserialize()
        {
            // Fix unserialized rule references in Selectors in StyleSheets.
            // VTA.inlineSheet only has Rules so it does not need this fix.
            foreach (var openUSSFile in m_OpenUSSFiles)
                openUSSFile.FixRuleReferences();
        }

        public void OnAfterLoadFromDisk()
        {
            // Very important we convert asset references to paths here after a restore.
            if (m_VisualTreeAsset != null)
                m_VisualTreeAsset.ConvertAllAssetReferencesToPaths();
        }

        //
        // Private Utilities
        //

        void RestoreAssetsFromBackup()
        {
            if (m_VisualTreeAsset != null && m_VisualTreeAssetBackup != null)
                m_VisualTreeAssetBackup.DeepOverwrite(m_VisualTreeAsset);

            foreach (var openUSSFile in m_OpenUSSFiles)
                openUSSFile.RestoreFromBackup();
        }

        void ClearBackups()
        {
            m_VisualTreeAssetBackup.Destroy();
            m_VisualTreeAssetBackup = null;

            foreach (var openUSSFile in m_OpenUSSFiles)
                openUSSFile.ClearBackup();
        }

        void ClearUndo()
        {
            m_VisualTreeAsset.ClearUndo();

            foreach (var openUSSFile in m_OpenUSSFiles)
                openUSSFile.ClearUndo();
        }

        void WriteUXMLToFile(string uxmlPath)
        {
            var uxmlText = visualTreeAsset.GenerateUXML(uxmlPath, true);

            // This will only be null (not empty) if the UXML is invalid in some way.
            if (uxmlText == null)
                return;

            // Make sure the folders exist.
            var uxmlFolder = Path.GetDirectoryName(uxmlPath);
            if (!Directory.Exists(uxmlFolder))
                Directory.CreateDirectory(uxmlFolder);

            File.WriteAllText(uxmlPath, uxmlText);
        }

        void ReloadDocumentToCanvas(VisualElement documentRootElement)
        {
            if (documentRootElement == null)
                return;

            // Load the asset.
            documentRootElement.Clear();
            try
            {
                visualTreeAsset.LinkedCloneTree(documentRootElement);
            }
            catch (Exception e)
            {
                Debug.LogError("Invalid UXML or USS: " + e.ToString());
                Clear();
            }
            documentRootElement.SetProperty(
                BuilderConstants.ElementLinkedVisualTreeAssetVEPropertyName, visualTreeAsset);

            // TODO: For now, don't allow stylesheets in root elements.
            foreach (var rootElement in documentRootElement.Children())
                rootElement.styleSheets.Clear();

            // Refresh styles.
            RefreshStyle(documentRootElement);

            // Add shared styles.
            BuilderSharedStyles.AddSelectorElementsFromStyleSheet(documentRootElement, m_OpenUSSFiles);
        }
    }
}
