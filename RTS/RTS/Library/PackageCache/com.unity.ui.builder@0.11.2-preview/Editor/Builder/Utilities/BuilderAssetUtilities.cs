using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UI.Builder
{
    internal static class BuilderAssetUtilities
    {
        public static string GetResourcesPathForAsset(Object asset)
        {
            var assetPath = AssetDatabase.GetAssetPath(asset);
            return GetResourcesPathForAsset(assetPath);
        }

        public static string GetResourcesPathForAsset(string assetPath)
        {
            var resourcesFolder = "Resources/";
            if (string.IsNullOrEmpty(assetPath) || !assetPath.Contains(resourcesFolder))
                return null;

            var lastResourcesSubstring = assetPath.LastIndexOf(resourcesFolder) + resourcesFolder.Length;
            assetPath = assetPath.Substring(lastResourcesSubstring);
            var lastExtDot = assetPath.LastIndexOf(".");

            if (lastExtDot == -1)
            {
                return null;
            }

            assetPath = assetPath.Substring(0, lastExtDot);

            return assetPath;
        }

        public static bool IsBuiltinPath(string assetPath)
        {
            return assetPath == "Resources/unity_builtin_extra";
        }

        public static void AddStyleSheetToAsset(
            BuilderDocument document, string ussPath)
        {
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
            if (styleSheet == null)
            {
                BuilderDialogsUtility.DisplayDialog("Invalid Asset Type", @"Asset at path {ussPath} is not a StyleSheet.");
                return;
            }

            Undo.RegisterCompleteObjectUndo(
                document.visualTreeAsset, "Add StyleSheet to UXML");

            document.AddStyleSheetToDocument(styleSheet, ussPath);
        }

        public static void RemoveStyleSheetFromAsset(
            BuilderDocument document, int ussIndex)
        {
            Undo.RegisterCompleteObjectUndo(
                document.visualTreeAsset, "Remove StyleSheet from UXML");

            document.RemoveStyleSheetFromDocument(ussIndex);
        }

        public static VisualElementAsset AddElementToAsset(
            BuilderDocument document, VisualElement ve, int index = -1)
        {
            Undo.RegisterCompleteObjectUndo(
                document.visualTreeAsset, BuilderConstants.CreateUIElementUndoMessage);

            var veParent = ve.parent;
            VisualElementAsset veaParent = null;
            if (veParent != null)
                veaParent = veParent.GetVisualElementAsset();

#if UNITY_2020_1_OR_NEWER
            if (veaParent == null)
                veaParent = document.visualTreeAsset.GetRootUXMLElement(); // UXML Root Element
#endif

            var vea = document.visualTreeAsset.AddElement(veaParent, ve);

            if (index >= 0)
                document.visualTreeAsset.ReparentElement(vea, veaParent, index);

            return vea;
        }

        public static VisualElementAsset AddElementToAsset(
            BuilderDocument document, VisualElement ve,
            Func<VisualTreeAsset, VisualElementAsset, VisualElementAsset> makeVisualElementAsset,
            int index = -1)
        {
            Undo.RegisterCompleteObjectUndo(
                document.visualTreeAsset, BuilderConstants.CreateUIElementUndoMessage);

            var veParent = ve.parent;
            var veaParent = veParent == null ? null : veParent.GetVisualElementAsset();

#if UNITY_2020_1_OR_NEWER
            if (veaParent == null)
                veaParent = document.visualTreeAsset.GetRootUXMLElement(); // UXML Root Element
#endif

            var vea = makeVisualElementAsset(document.visualTreeAsset, veaParent);
            ve.SetProperty(BuilderConstants.ElementLinkedVisualElementAssetVEPropertyName, vea);

            if (index >= 0)
                document.visualTreeAsset.ReparentElement(vea, veaParent, index);

            return vea;
        }

        public static void ReparentElementInAsset(
            BuilderDocument document, VisualElement veToReparent, VisualElement newParent, int index = -1)
        {
            var veaToReparent = veToReparent.GetVisualElementAsset();
            if (veaToReparent == null)
                return;

            Undo.RegisterCompleteObjectUndo(
                document.visualTreeAsset, BuilderConstants.ReparentUIElementUndoMessage);

            VisualElementAsset veaNewParent = null;
            if (newParent != null)
                veaNewParent = newParent.GetVisualElementAsset();

#if UNITY_2020_1_OR_NEWER
            if (veaNewParent == null)
                veaNewParent = document.visualTreeAsset.GetRootUXMLElement(); // UXML Root Element
#endif

            document.visualTreeAsset.ReparentElement(veaToReparent, veaNewParent, index);
        }

        public static void DeleteElementFromAsset(BuilderDocument document, VisualElement ve)
        {
            var vea = ve.GetVisualElementAsset();
            if (vea == null)
                return;

            Undo.RegisterCompleteObjectUndo(
                document.visualTreeAsset, BuilderConstants.DeleteUIElementUndoMessage);

            document.visualTreeAsset.RemoveElement(vea);
        }

        public static void TransferAssetToAsset(
            BuilderDocument document, VisualElementAsset parent, VisualTreeAsset otherVta)
        {
            Undo.RegisterCompleteObjectUndo(
                document.visualTreeAsset, BuilderConstants.CreateUIElementUndoMessage);

            document.visualTreeAsset.Swallow(parent, otherVta);
        }

        public static void TransferAssetToAsset(
            BuilderDocument document, StyleSheet styleSheet, StyleSheet otherStyleSheet)
        {
            Undo.RegisterCompleteObjectUndo(
                styleSheet, BuilderConstants.AddNewSelectorUndoMessage);

            styleSheet.Swallow(otherStyleSheet);
        }

        public static void AddStyleClassToElementInAsset(BuilderDocument document, VisualElement ve, string className)
        {
            Undo.RegisterCompleteObjectUndo(
                document.visualTreeAsset, BuilderConstants.AddStyleClassUndoMessage);

            var vea = ve.GetVisualElementAsset();
            vea.AddStyleClass(className);
        }

        public static void RemoveStyleClassToElementInAsset(BuilderDocument document, VisualElement ve, string className)
        {
            Undo.RegisterCompleteObjectUndo(
                document.visualTreeAsset, BuilderConstants.RemoveStyleClassUndoMessage);

            var vea = ve.GetVisualElementAsset();
            vea.RemoveStyleClass(className);
        }

        public static void AddStyleComplexSelectorToSelection(BuilderDocument document, StyleSheet styleSheet, StyleComplexSelector scs)
        {
            var selectionProp = styleSheet.AddProperty(
                scs,
                BuilderConstants.SelectedStyleRulePropertyName,
                BuilderConstants.ChangeSelectionUndoMessage);

            // Need to add at least one dummy value because lots of code will die
            // if it encounters a style property with no values.
            styleSheet.AddValue(
                selectionProp, 42.0f, BuilderConstants.ChangeSelectionUndoMessage);
        }

        public static void AddElementToSelectionInAsset(BuilderDocument document, VisualElement ve)
        {
            if (BuilderSharedStyles.IsStyleSheetElement(ve))
            {
                var styleSheet = ve.GetStyleSheet();
                styleSheet.AddSelector(
                    BuilderConstants.SelectedStyleSheetSelectorName,
                    BuilderConstants.ChangeSelectionUndoMessage);
            }
            else if (BuilderSharedStyles.IsSelectorElement(ve))
            {
                var styleSheet = ve.GetClosestStyleSheet();
                var scs = ve.GetStyleComplexSelector();
                AddStyleComplexSelectorToSelection(document, styleSheet, scs);
            }
            else if (BuilderSharedStyles.IsDocumentElement(ve))
            {
                Undo.RegisterCompleteObjectUndo(
                    document.visualTreeAsset, BuilderConstants.ChangeSelectionUndoMessage);

                var vta = ve.GetVisualTreeAsset();
                vta.AddElement(null, BuilderConstants.SelectedVisualTreeAssetSpecialElementTypeName);
            }
            else if (ve.GetVisualElementAsset() != null)
            {
                Undo.RegisterCompleteObjectUndo(
                    document.visualTreeAsset, BuilderConstants.ChangeSelectionUndoMessage);

                var vea = ve.GetVisualElementAsset();
                vea.Select();
            }
        }

        public static void RemoveElementFromSelectionInAsset(BuilderDocument document, VisualElement ve)
        {
            if (BuilderSharedStyles.IsStyleSheetElement(ve))
            {
                var styleSheet = ve.GetStyleSheet();
                styleSheet.RemoveSelector(
                    BuilderConstants.SelectedStyleSheetSelectorName,
                    BuilderConstants.ChangeSelectionUndoMessage);
            }
            else if (BuilderSharedStyles.IsSelectorElement(ve))
            {
                var styleSheet = ve.GetClosestStyleSheet();
                var scs = ve.GetStyleComplexSelector();
                styleSheet.RemoveProperty(
                    scs,
                    BuilderConstants.SelectedStyleRulePropertyName,
                    BuilderConstants.ChangeSelectionUndoMessage);
            }
            else if (BuilderSharedStyles.IsDocumentElement(ve))
            {
                Undo.RegisterCompleteObjectUndo(
                    document.visualTreeAsset, BuilderConstants.ChangeSelectionUndoMessage);

                var vta = ve.GetVisualTreeAsset();
                var selectedElement = vta.FindElementByType(BuilderConstants.SelectedVisualTreeAssetSpecialElementTypeName);
                vta.RemoveElement(selectedElement);
            }
            else if (ve.GetVisualElementAsset() != null)
            {
                Undo.RegisterCompleteObjectUndo(
                    document.visualTreeAsset, BuilderConstants.ChangeSelectionUndoMessage);

                var vea = ve.GetVisualElementAsset();
                vea.Deselect();
            }
        }

        public static string GetVisualTreeAssetAssetName(VisualTreeAsset visualTreeAsset, bool hasUnsavedChanges) =>
            GetAssetName(visualTreeAsset, BuilderConstants.UxmlExtension, hasUnsavedChanges);

        public static string GetStyleSheetAssetName(StyleSheet styleSheet, bool hasUnsavedChanges) =>
            GetAssetName(styleSheet, BuilderConstants.UssExtension, hasUnsavedChanges);

        public static string GetAssetName(ScriptableObject asset, string extension, bool hasUnsavedChanges)
        {
            if (asset == null)
            {
                if (extension == BuilderConstants.UxmlExtension)
                    return BuilderConstants.ToolbarUnsavedFileDisplayMessage + extension;
                else
                    return string.Empty;
            }

            var assetPath = AssetDatabase.GetAssetPath(asset);
            if(string.IsNullOrEmpty(assetPath))
                return BuilderConstants.ToolbarUnsavedFileDisplayMessage + extension;
           
            return Path.GetFileName(assetPath) + (hasUnsavedChanges ? BuilderConstants.ToolbarUnsavedFileSuffix : "");
        }
    }
}