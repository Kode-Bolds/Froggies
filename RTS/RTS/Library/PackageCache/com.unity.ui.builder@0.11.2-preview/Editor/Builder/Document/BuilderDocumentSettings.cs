using UnityEngine;
using System;
using System.IO;
using UnityEditor;

namespace Unity.UI.Builder
{
    [Serializable]
    class BuilderDocumentSettings
    {
        public string UxmlGuid;
        public string UxmlPath;
        public int CanvasX;
        public int CanvasY;
        public int CanvasWidth = (int)BuilderConstants.CanvasInitialWidth;
        public int CanvasHeight = (int)BuilderConstants.CanvasInitialHeight;

        public float ZoomScale = BuilderConstants.ViewportInitialZoom;
        public Vector2 PanOffset = BuilderConstants.ViewportInitialContentOffset;

        public float CanvasBackgroundOpacity = 1.0f;
        public BuilderCanvasBackgroundMode CanvasBackgroundMode = BuilderCanvasBackgroundMode.None;
        public Color CanvasBackgroundColor = new Color(0, 0, 0, 255);
        public Texture2D CanvasBackgroundImage;
        public ScaleMode CanvasBackgroundImageScaleMode = ScaleMode.ScaleAndCrop;
        public string CanvasBackgroundCameraName;

        public static BuilderDocumentSettings CreateOrLoadSettingsObject(
            BuilderDocumentSettings settings,
            string uxmlPath)
        {
            if (settings != null)
                return settings;

            settings = new BuilderDocumentSettings();

            var diskDataFound = settings.LoadSettingsFromDisk(uxmlPath);
            if (diskDataFound)
                return settings;

            settings.UxmlGuid = AssetDatabase.AssetPathToGUID(uxmlPath);
            settings.UxmlPath = uxmlPath;

            return settings;
        }

        public bool LoadSettingsFromDisk(string uxmlPath)
        {
            var guid = AssetDatabase.AssetPathToGUID(uxmlPath);
            if (string.IsNullOrEmpty(guid))
                return false;

            var folderPath = BuilderConstants.BuilderDocumentDiskSettingsJsonFolderAbsolutePath;
            var fileName = guid + ".json";
            var path = folderPath + "/" + fileName;

            if (!File.Exists(path))
                return false;

            var json = File.ReadAllText(path);
            EditorJsonUtility.FromJsonOverwrite(json, this);

            return true;
        }

        public void SaveSettingsToDisk()
        {
            if (string.IsNullOrEmpty(UxmlGuid) || string.IsNullOrEmpty(UxmlPath))
                return;

            var json = EditorJsonUtility.ToJson(this, true);

            var folderPath = BuilderConstants.BuilderDocumentDiskSettingsJsonFolderAbsolutePath;
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var fileName = UxmlGuid + ".json";
            var filePath = folderPath + "/" + fileName;
            File.WriteAllText(filePath, json);
        }
    }
}