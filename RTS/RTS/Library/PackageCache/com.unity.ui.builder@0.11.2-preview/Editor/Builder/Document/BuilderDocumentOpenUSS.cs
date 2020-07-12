using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using System;
using System.IO;

namespace Unity.UI.Builder
{
    [Serializable]
    class BuilderDocumentOpenUSS
    {
        [SerializeField]
        StyleSheet m_StyleSheet;

        // Used to restore in-memory StyleSheet asset if closing without saving.
        [SerializeField]
        StyleSheet m_Backup;

        // This is for automatic style path fixing after a uss file name change.
        [SerializeField]
        string m_OldPath;

        // Used during saving to reload USS asset from disk after AssetDatabase.Refresh().
        string m_NewPath;

        public StyleSheet Sheet
        {
            get => m_StyleSheet;
            set => m_StyleSheet = value;
        }

        public string AssetPath => AssetDatabase.GetAssetPath(m_StyleSheet);

        public string OldPath => m_OldPath;

        public void Set(StyleSheet styleSheet, string ussPath)
        {
            Clear();

            if (string.IsNullOrEmpty(ussPath))
                ussPath = AssetDatabase.GetAssetPath(styleSheet);

            m_StyleSheet = styleSheet;
            m_Backup = styleSheet.DeepCopy();
            m_OldPath = ussPath;
            m_NewPath = null;
        }

        public void Clear()
        {
            var path = AssetPath;

            ClearBackup();
            m_StyleSheet = null;

            // Restore from file system in case of unsaved changes.
            if (!string.IsNullOrEmpty(path))
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        }

        public void FixRuleReferences()
        {
            m_StyleSheet.FixRuleReferences();
        }

        public void SaveToDisk(VisualTreeAsset visualTreeAsset)
        {
            var newUSSPath = AssetPath;

            // There should not be a way to have an unsaved USS. The newUSSPath should always be non-empty.

            m_NewPath = newUSSPath;
            visualTreeAsset.ReplaceStyleSheetPaths(m_OldPath, newUSSPath);

            // Need to save a backup before the AssetDatabase.Refresh().
            if (m_Backup == null)
                m_Backup = m_StyleSheet.DeepCopy();
            else
                m_StyleSheet.DeepOverwrite(m_Backup);

            WriteUSSToFile(newUSSPath);
        }

        public bool PostSaveToDiskChecksAndFixes()
        {
            m_StyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(m_NewPath);
            bool needsFullRefresh = m_StyleSheet != m_Backup;

            // Get back selection markers from backup:
            if (needsFullRefresh)
                m_Backup.DeepOverwrite(m_StyleSheet);

            m_NewPath = null;
            return needsFullRefresh;
        }

        public bool CheckPostProcessAssetIfFileChanged(string assetPath)
        {
            if (assetPath != m_OldPath)
                return false;

            m_StyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(assetPath);
            return true;
        }

        public void RestoreFromBackup()
        {
            if (m_Backup == null || m_StyleSheet == null)
                return;

            m_Backup.DeepOverwrite(m_StyleSheet);
        }

        public void ClearBackup()
        {
            if (m_Backup == null)
                return;

            m_Backup.Destroy();
            m_Backup = null;
        }

        public void ClearUndo()
        {
            m_StyleSheet.ClearUndo();
        }

        void WriteUSSToFile(string ussPath)
        {
            var ussText = m_StyleSheet.GenerateUSS();

            // This will only be null (not empty) if the UXML is invalid in some way.
            if (ussText == null)
                return;

            // Make sure the folders exist.
            var ussFolder = Path.GetDirectoryName(ussPath);
            if (!Directory.Exists(ussFolder))
                Directory.CreateDirectory(ussFolder);

            File.WriteAllText(ussPath, ussText);
        }
    }
}
