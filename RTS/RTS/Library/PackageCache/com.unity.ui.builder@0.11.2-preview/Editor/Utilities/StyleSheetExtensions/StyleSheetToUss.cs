using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class UssExportOptions
    {
        public UssExportOptions()
        {
            propertyIndent = "    ";
            exportDefaultValues = true;
        }

        public UssExportOptions(UssExportOptions opts)
            : base()
        {
            propertyIndent = opts.propertyIndent;
            exportDefaultValues = opts.exportDefaultValues;
        }

        public string propertyIndent { get; set; }
        public bool useColorCode { get; set; }
        public bool exportDefaultValues { get; set; }
    }

    internal class StyleSheetToUss
    {
        static int ColorComponent(float component)
        {
            return (int)Math.Round(component * byte.MaxValue, 0, MidpointRounding.AwayFromZero);
        }

        public static string ToUssString(Color color, bool useColorCode = false)
        {
            string str;
            string alpha = color.a.ToString("0.##", CultureInfo.InvariantCulture.NumberFormat);
            if (alpha != "1")
            {
                str = UnityString.Format("rgba({0}, {1}, {2}, {3:F2})", ColorComponent(color.r),
                    ColorComponent(color.g),
                    ColorComponent(color.b),
                    alpha);
            }
            else if (!useColorCode)
            {
                str = UnityString.Format("rgb({0}, {1}, {2})",
                    ColorComponent(color.r),
                    ColorComponent(color.g),
                    ColorComponent(color.b));
            }
            else
            {
                str = UnityString.Format("#{0}", ColorUtility.ToHtmlStringRGB(color));
            }
            return str;
        }

        public static string ValueHandleToUssString(StyleSheet sheet, UssExportOptions options, string propertyName, StyleValueHandle handle)
        {
            string str = "";
            switch (handle.valueType)
            {
                case StyleValueType.Keyword:
                    str = sheet.ReadKeyword(handle).ToString().ToLower();
                    break;
                case StyleValueType.Float:
                    {
                        var num = sheet.ReadFloat(handle);
                        if (num == 0)
                        {
                            str = "0";
                        }
                        else
                        {
                            str = num.ToString(CultureInfo.InvariantCulture.NumberFormat);
                            if (IsLength(propertyName))
                                str += "px";
                        }
                    }
                    break;
#if UNITY_2019_3_OR_NEWER
                case StyleValueType.Dimension:
                    var dim = sheet.ReadDimension(handle);
                    if (dim.value == 0)
                        str = "0";
                    else
                        str = dim.ToString();
                    break;
#endif
                case StyleValueType.Color:
                    UnityEngine.Color color = sheet.ReadColor(handle);
                    str = ToUssString(color, options.useColorCode);
                    break;
                case StyleValueType.ResourcePath:
                    str = $"resource('{sheet.ReadResourcePath(handle)}')";
                    break;
                case StyleValueType.Enum:
                    str = sheet.ReadEnum(handle);
                    break;
                case StyleValueType.String:
                    str = $"\"{sheet.ReadString(handle)}\"";
                    break;
#if UNITY_2020_2_OR_NEWER
                case StyleValueType.MissingAssetReference:
                    str = $"url('{sheet.ReadMissingAssetReferenceUrl(handle)}')";
                    break;
#endif
                case StyleValueType.AssetReference:
                    var assetRef = sheet.ReadAssetReference(handle);
                    var assetPath = AssetDatabase.GetAssetPath(assetRef);
                    if (assetPath.StartsWith("Assets") || assetPath.StartsWith("Packages"))
                        assetPath = "/" + assetPath;
                    str = assetRef == null ? "none" : $"url('{assetPath}')";
                    break;
                default:
                    throw new ArgumentException("Unhandled type " + handle.valueType);
            }
            return str;
        }

        public static void ValueHandlesToUssString(StringBuilder sb, StyleSheet sheet, UssExportOptions options, string propertyName, StyleValueHandle[] values, ref int valueIndex, int valueCount = -1)
        {
            for (; valueIndex < values.Length && valueCount != 0; --valueCount)
            {
                var propertyValue = values[valueIndex++];
                switch (propertyValue.valueType)
                {
                    case StyleValueType.Function:
                        // First param: function name
                        sb.Append(sheet.ReadFunctionName(propertyValue));
                        sb.Append("(");

                        // Second param: number of arguments
                        var nbParams = (int)sheet.ReadFloat(values[valueIndex++]);
                        ValueHandlesToUssString(sb, sheet, options, propertyName, values, ref valueIndex, nbParams);
                        sb.Append(")");

                        break;
                    case StyleValueType.FunctionSeparator:
                        sb.Append(",");
                        break;
                    default:
                        {
                            var propertyValueStr = ValueHandleToUssString(sheet, options, propertyName, propertyValue);
                            sb.Append(propertyValueStr);
                            break;
                        }
                }

                if (valueIndex < values.Length && values[valueIndex].valueType != StyleValueType.FunctionSeparator && valueCount != 1)
                {
                    sb.Append(" ");
                }
            }
        }

        static bool IsLength(string name)
        {
            if (BuilderConstants.SpecialSnowflakeLengthSytles.Contains(name))
                return true;

#if UNITY_2019_3_OR_NEWER
            return false;
#else
            foreach (System.Reflection.PropertyInfo field in StyleSheetUtilities.ComputedStylesFieldInfos)
            {
                var styleName = BuilderNameUtilities.ConverStyleCSharpNameToUssName(field.Name);
                if (styleName != name)
                    continue;

                var dummyElement = new VisualElement();
                var val = field.GetValue(dummyElement.computedStyle, null);
                if (val is StyleLength)
                    return true;
            }
            return false;
#endif
        }

        public static void ToUssString(StyleSheet sheet, UssExportOptions options, StyleRule rule, StringBuilder sb)
        {
            foreach (var property in rule.properties)
            {
                if (property.name == BuilderConstants.SelectedStyleRulePropertyName)
                    continue;

                sb.Append(options.propertyIndent);
                sb.Append(property.name);
                sb.Append(":");
                if (property.name == "cursor" && property.values.Length > 1)
                {
                    int i;
                    string propertyValueStr;
                    for (i = 0; i < property.values.Length - 1; i++)
                    {
                        propertyValueStr = ValueHandleToUssString(sheet, options, property.name, property.values[i]);
                        sb.Append(" ");
                        sb.Append(propertyValueStr);
                    }
                    sb.Append(", ");
                    propertyValueStr = ValueHandleToUssString(sheet, options, property.name, property.values[i]);
                    sb.Append(propertyValueStr);
                }
                else
                {
                    var valueIndex = 0;
                    sb.Append(" ");
                    ValueHandlesToUssString(sb, sheet, options, property.name, property.values, ref valueIndex);
                }

                sb.Append(";");
                sb.Append(BuilderConstants.NewlineCharFromEditorSettings);
            }
        }

        public static void ToUssString(StyleSelectorRelationship previousRelationship, StyleSelectorPart[] parts, StringBuilder sb)
        {
            if (previousRelationship != StyleSelectorRelationship.None)
                sb.Append(previousRelationship == StyleSelectorRelationship.Child ? " > " : " ");
            foreach (var selectorPart in parts)
            {
                switch (selectorPart.type)
                {
                    case StyleSelectorType.Wildcard:
                        sb.Append('*');
                        break;
                    case StyleSelectorType.Type:
                        sb.Append(selectorPart.value);
                        break;
                    case StyleSelectorType.Class:
                        sb.Append('.');
                        sb.Append(selectorPart.value);
                        break;
                    case StyleSelectorType.PseudoClass:
                        sb.Append(':');
                        sb.Append(selectorPart.value);
                        break;
                    case StyleSelectorType.ID:
                        sb.Append('#');
                        sb.Append(selectorPart.value);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public static string ToUssSelector(StyleComplexSelector complexSelector)
        {
            var sb = new StringBuilder();
            foreach (var selector in complexSelector.selectors)
            {
                ToUssString(selector.previousRelationship, selector.parts, sb);
            }
            return sb.ToString();
        }

        public static string ToUssString(StyleSheet sheet, StyleComplexSelector complexSelector)
        {
            var inlineBuilder = new StringBuilder();

            ToUssString(sheet, new UssExportOptions(), complexSelector, inlineBuilder);

            var result = inlineBuilder.ToString();
            return result;
        }

        public static void ToUssString(StyleSheet sheet, UssExportOptions options, StyleComplexSelector complexSelector, StringBuilder sb)
        {
            foreach (var selector in complexSelector.selectors)
                ToUssString(selector.previousRelationship, selector.parts, sb);

            sb.Append(" {");
            sb.Append(BuilderConstants.NewlineCharFromEditorSettings);

            ToUssString(sheet, options, complexSelector.rule, sb);

            sb.Append("}");
            sb.Append(BuilderConstants.NewlineCharFromEditorSettings);
        }

        public static string ToUssString(StyleSheet sheet, UssExportOptions options = null)
        {
            if (options == null)
                options = new UssExportOptions();

            var sb = new StringBuilder();
            if (sheet.complexSelectors != null)
            {
                for (var complexSelectorIndex = 0; complexSelectorIndex < sheet.complexSelectors.Length; ++complexSelectorIndex)
                {
                    var complexSelector = sheet.complexSelectors[complexSelectorIndex];

                    // Omit special selection rule.
                    if (complexSelector.selectors.Length > 0 &&
                        complexSelector.selectors[0].parts.Length > 0 &&
                        complexSelector.selectors[0].parts[0].value == BuilderConstants.SelectedStyleSheetSelectorName)
                        continue;

                    ToUssString(sheet, options, complexSelector, sb);
                    if (complexSelectorIndex != sheet.complexSelectors.Length - 1)
                    {
                        sb.Append(BuilderConstants.NewlineCharFromEditorSettings);
                    }
                }
            }

            return sb.ToString();
        }

        public static void WriteStyleSheet(StyleSheet sheet, string path, UssExportOptions options = null)
        {
            File.WriteAllText(path, ToUssString(sheet, options));
        }
    }
}
