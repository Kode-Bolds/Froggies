using System.Collections.Generic;
using System.Linq;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal static class StyleSheetExtensions
    {
        public static StyleSheet DeepCopy(this StyleSheet styleSheet)
        {
            if (styleSheet == null)
                return null;

            var newStyleSheet = StyleSheetUtilities.CreateInstance();

            styleSheet.DeepOverwrite(newStyleSheet);

            return newStyleSheet;
        }

        public static void DeepOverwrite(this StyleSheet styleSheet, StyleSheet other)
        {
            var json = JsonUtility.ToJson(styleSheet);
            JsonUtility.FromJsonOverwrite(json, other);

            other.FixRuleReferences();

            other.name = styleSheet.name;
        }

        public static void FixRuleReferences(this StyleSheet styleSheet)
        {
            // Force call to StyleSheet.SetupReferences().
            styleSheet.rules = styleSheet.rules;
        }

        internal static List<string> GetSelectorStrings(this StyleSheet styleSheet)
        {
            var list = new List<string>();

            foreach (var complexSelector in styleSheet.complexSelectors)
            {
                var str = StyleSheetToUss.ToUssSelector(complexSelector);
                list.Add(str);
            }

            return list;
        }

        internal static string GenerateUSS(this StyleSheet styleSheet)
        {
            string result = null;
            try
            {
                result = StyleSheetToUss.ToUssString(styleSheet);
            }
            catch (Exception ex)
            {
                if (!styleSheet.name.Contains(BuilderConstants.InvalidUXMLOrUSSAssetNameSuffix))
                {
                    var message = string.Format(BuilderConstants.InvalidUSSDialogMessage, styleSheet.name);
                    BuilderDialogsUtility.DisplayDialog(BuilderConstants.InvalidUSSDialogTitle, message);
                    styleSheet.name = styleSheet.name + BuilderConstants.InvalidUXMLOrUSSAssetNameSuffix;
                }
                else
                {
                    var name = styleSheet.name.Replace(BuilderConstants.InvalidUXMLOrUSSAssetNameSuffix, string.Empty);
                    var message = string.Format(BuilderConstants.InvalidUSSDialogMessage, name);
                    Builder.ShowWarning(message);
                }
                Debug.LogError(ex.Message + "\n" + ex.StackTrace);
            }
            return result;
        }

        internal static StyleComplexSelector FindSelector(this StyleSheet styleSheet, string selectorStr)
        {
            // Remove extra whitespace.
            var selectorSplit = selectorStr.Split(' ');
            selectorStr = String.Join(" ", selectorSplit);

            foreach (var complexSelector in styleSheet.complexSelectors)
            {
                var str = StyleSheetToUss.ToUssSelector(complexSelector);

                if (str == selectorStr)
                    return complexSelector;
            }

            return null;
        }

        internal static void RemoveSelector(
            this StyleSheet styleSheet, string selectorStr, string undoMessage = null)
        {
            var selector = styleSheet.FindSelector(selectorStr);
            if (selector == null)
                return;

            // Undo/Redo
            if (string.IsNullOrEmpty(undoMessage))
                undoMessage = "Delete UI Style Selector";
            Undo.RegisterCompleteObjectUndo(styleSheet, undoMessage);

            var selectorList = styleSheet.complexSelectors.ToList();
            selectorList.Remove(selector);
            styleSheet.complexSelectors = selectorList.ToArray();
        }

        public static int AddRule(this StyleSheet styleSheet)
        {
            var rule = new StyleRule { line = -1 };
            rule.properties = new StyleProperty[0];

            // Add rule to StyleSheet.
            var rulesList = styleSheet.rules.ToList();
            var index = rulesList.Count;
            rulesList.Add(rule);
            styleSheet.rules = rulesList.ToArray();

            return index;
        }

        public static StyleRule GetRule(this StyleSheet styleSheet, int index)
        {
            if (styleSheet.rules.Count() <= index)
                return null;

            return styleSheet.rules[index];
        }

        public static StyleComplexSelector AddSelector(
            this StyleSheet styleSheet, string complexSelectorStr, string undoMessage = null)
        {
            // Undo/Redo
            if (string.IsNullOrEmpty(undoMessage))
                undoMessage = "New UI Style Selector";
            Undo.RegisterCompleteObjectUndo(styleSheet, undoMessage);

            // Remove extra whitespace.
            var selectorSplit = complexSelectorStr.Split(' ');
            complexSelectorStr = String.Join(" ", selectorSplit);

            // Create rule.
            var rule = new StyleRule { line = -1 };
            rule.properties = new StyleProperty[0];

            // Create selector.
            var complexSelector = new StyleComplexSelector();
            complexSelector.rule = rule;
            var initResult = StyleComplexSelectorExtensions.InitializeSelector(complexSelector, complexSelectorStr);
            if (!initResult)
                return null;

            // Add rule to StyleSheet.
            var rulesList = styleSheet.rules.ToList();
            rulesList.Add(rule);
            styleSheet.rules = rulesList.ToArray();
            complexSelector.ruleIndex = styleSheet.rules.Length - 1;

            // Add complex selector to list in stylesheet.
            var complexSelectorsList = styleSheet.complexSelectors.ToList();
            complexSelectorsList.Add(complexSelector);
            styleSheet.complexSelectors = complexSelectorsList.ToArray();

            return complexSelector;
        }

        public static void TransferRulePropertiesToSelector(this StyleSheet toStyleSheet, StyleComplexSelector toSelector, StyleSheet fromStyleSheet, StyleRule fromRule)
        {
            foreach (var property in fromRule.properties)
            {
                var newProperty = toStyleSheet.AddProperty(toSelector, property.name);
                foreach (var value in property.values)
                {
                    switch (value.valueType)
                    {
                        case StyleValueType.Float: toStyleSheet.AddValue(newProperty, fromStyleSheet.GetFloat(value)); break;
#if UNITY_2019_3_OR_NEWER
                        case StyleValueType.Dimension: toStyleSheet.AddValue(newProperty, fromStyleSheet.GetDimension(value)); break;
#endif
                        case StyleValueType.Enum: toStyleSheet.AddValueAsEnum(newProperty, fromStyleSheet.GetEnum(value)); break;
                        case StyleValueType.String: toStyleSheet.AddValue(newProperty, fromStyleSheet.GetString(value)); break;
                        case StyleValueType.Color: toStyleSheet.AddValue(newProperty, fromStyleSheet.GetColor(value)); break;
                        case StyleValueType.AssetReference: toStyleSheet.AddValue(newProperty, fromStyleSheet.GetAsset(value)); break;
                        case StyleValueType.ResourcePath: toStyleSheet.AddValue(newProperty, fromStyleSheet.GetAsset(value)); break;
                    }
                }
            }
            foreach (var property in fromRule.properties)
            {
                fromStyleSheet.RemoveProperty(fromRule, property);
            }
        }

        public static bool IsSelected(this StyleSheet styleSheet)
        {
            var selector = styleSheet.FindSelector(BuilderConstants.SelectedStyleSheetSelectorName);
            return selector != null;
        }

        static void SwallowStyleRule(
            StyleSheet toStyleSheet, StyleComplexSelector toSelector,
            StyleSheet fromStyleSheet, StyleComplexSelector fromSelector)
        {
            var fromRule = fromSelector.rule;

            // Add property values to sheet.
            foreach (var fromProperty in fromRule.properties)
            {
                var toProperty = toStyleSheet.AddProperty(toSelector, fromProperty.name);
                for (int i = 0; i < fromProperty.values.Length; ++i)
                {
                    var fromValueHandle = fromProperty.values[i];
                    var toValueIndex = toStyleSheet.SwallowStyleValue(fromStyleSheet, fromValueHandle);
                    toStyleSheet.AddValueHandle(toProperty, toValueIndex, fromValueHandle.valueType);
                }
            }
        }

        public static void Swallow(this StyleSheet toStyleSheet, StyleSheet fromStyleSheet)
        {
            foreach (var fromSelector in fromStyleSheet.complexSelectors)
            {
                var toSelector = toStyleSheet.AddSelector(StyleSheetToUss.ToUssSelector(fromSelector));
                SwallowStyleRule(toStyleSheet, toSelector, fromStyleSheet, fromSelector);
            }
        }

        public static void ClearUndo(this StyleSheet styleSheet)
        {
            if (styleSheet == null)
                return;

            Undo.ClearUndo(styleSheet);
        }

        public static void Destroy(this StyleSheet styleSheet)
        {
            if (styleSheet == null)
                return;

            ScriptableObject.DestroyImmediate(styleSheet);
        }
    }
}