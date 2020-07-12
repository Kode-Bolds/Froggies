using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class BuilderLibraryTreeItem : TreeViewItem<string>
    {
        public string Name => data;
        public Type Type { get; }
        public bool IsHeader { get; set; }
        public bool HasPreview { get; set; }
        public VisualTreeAsset SourceAsset { get; }
        public string SourceAssetPath { get; }
        public Func<VisualElement> MakeVisualElementCallback { get; }
        public Func<VisualTreeAsset, VisualElementAsset, VisualElementAsset> MakeElementAssetCallback { get; }
        public Texture2D Icon { get; private set; }
        public Texture2D LargeIcon { get; private set; }
        public bool IsEditorOnly { get; set; }
        public Texture2D DarkSkinIcon { get; private set; }
        public Texture2D LightSkinIcon { get; private set; }
        public Texture2D DarkSkinLargeIcon { get; private set; }
        public Texture2D LightSkinLargeIcon { get; private set; }

        public BuilderLibraryTreeItem(
            string name, string iconName, Type type, Func<VisualElement> makeVisualElementCallback,
            Func<VisualTreeAsset, VisualElementAsset, VisualElementAsset> makeElementAssetCallback = null,
            List<TreeViewItem<string>> children = null, VisualTreeAsset asset = null, int id = default)
            : base(GetItemId(name, type, asset, id) , name, children)
        {
            MakeVisualElementCallback = makeVisualElementCallback;
            MakeElementAssetCallback = makeElementAssetCallback;
            SourceAsset = asset;
            if (SourceAsset != null)
                SourceAssetPath = AssetDatabase.GetAssetPath(SourceAsset);

            Type = type;
            var @namespace = Type?.Namespace;
            if (@namespace != null)
            {
                if (@namespace.Contains(nameof(UnityEditor)) || @namespace.Contains("Unity.Editor"))
                    IsEditorOnly = true;
            }

            if (!string.IsNullOrEmpty(iconName))
            {
                AssignIcon(iconName);
                if (Icon == null)
                    AssignIcon("VisualElement");
            }
        }

        static int GetItemId(string name, Type type, VisualTreeAsset asset, int id)
        {
            if (id != default)
                return id;

            if (asset != null)
                return AssetDatabase.GetAssetPath(asset).GetHashCode();

            return (name + type?.FullName).GetHashCode();
        }

        void AssignIcon(string iconName)
        {
            var darkSkinResourceBasePath = $"{BuilderConstants.IconsResourcesPath}/Dark/Library/";
            var lightSkinResourceBasePath = $"{BuilderConstants.IconsResourcesPath}/Light/Library/";

            DarkSkinLargeIcon = LoadLargeIcon(darkSkinResourceBasePath, iconName);
            LightSkinLargeIcon = LoadLargeIcon(lightSkinResourceBasePath, iconName);

            DarkSkinIcon = LoadIcon(darkSkinResourceBasePath, iconName);
            LightSkinIcon = LoadIcon(lightSkinResourceBasePath, iconName);

            if (EditorGUIUtility.isProSkin)
            {
                Icon = DarkSkinIcon;
                LargeIcon = DarkSkinLargeIcon;
            }
            else
            {
                Icon = LightSkinIcon;
                LargeIcon = LightSkinLargeIcon;
            }
        }

        Texture2D LoadIcon(string resourceBasePath, string iconName)
        {
            return Resources.Load<Texture2D>(EditorGUIUtility.pixelsPerPoint > 1
                ? $"{resourceBasePath}{iconName}@2x"
                : $"{resourceBasePath}{iconName}");
        }

        Texture2D LoadLargeIcon(string resourceBasePath, string iconName)
        {
            return Resources.Load<Texture2D>(EditorGUIUtility.pixelsPerPoint > 1
                ? $"{resourceBasePath}{iconName}@8x"
                : $"{resourceBasePath}{iconName}@4x");
        }

        public void SetIcon(Texture2D icon)
        {
            Icon = icon;
            LargeIcon = icon;
            DarkSkinIcon = icon;
            LightSkinIcon = icon;
        }
    }
}
