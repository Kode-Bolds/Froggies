using UnityEditor.StyleSheets;
using UnityEngine.UIElements;
using UnityEngine;
using System.Reflection;

namespace Unity.UI.Builder
{
    internal static class StyleSheetUtilities
    {
        public static readonly PropertyInfo[] ComputedStylesFieldInfos =
            typeof(ComputedStyle).GetProperties(BindingFlags.Instance | BindingFlags.Public);

        readonly static StyleSheetImporterImpl s_StyleSheetImporter = new StyleSheetImporterImpl();

        public static StyleSheet CreateInstance()
        {
            var newStyleSheet = ScriptableObject.CreateInstance<StyleSheet>();
            newStyleSheet.hideFlags = HideFlags.DontUnloadUnusedAsset | HideFlags.DontSaveInEditor;

            // Initialize all defaults.
            s_StyleSheetImporter.Import(newStyleSheet, "");

            return newStyleSheet;
        }

        public static StyleValueKeyword ConvertStyleKeyword(StyleKeyword keyword)
        {
            switch (keyword)
            {
                case StyleKeyword.Auto:
                    return StyleValueKeyword.Auto;
                case StyleKeyword.None:
                    return StyleValueKeyword.None;
                case StyleKeyword.Initial:
                    return StyleValueKeyword.Initial;
            }

            return StyleValueKeyword.Auto;
        }
    }
}