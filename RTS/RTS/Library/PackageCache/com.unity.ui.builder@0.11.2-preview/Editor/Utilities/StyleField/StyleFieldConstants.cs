using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets.Syntax;
#if UNITY_2019_3_OR_NEWER
using UnityEngine.UIElements.StyleSheets;
#endif

namespace Unity.UI.Builder
{
    internal static class StyleFieldConstants
    {
        // Units
        public static readonly string UnitPixel = "px";
        public static readonly string UnitPercent = "%";

#if UNITY_2019_3_OR_NEWER
        public static readonly Dictionary<string, Dimension.Unit> StringToDimensionUnitMap = new Dictionary<string, Dimension.Unit>()
        {
            { UnitPixel, Dimension.Unit.Pixel },
            { UnitPercent, Dimension.Unit.Percent }
        };

        public static readonly Dictionary<Dimension.Unit, string> DimensionUnitToStringMap = new Dictionary<Dimension.Unit, string>()
        {
            { Dimension.Unit.Pixel, UnitPixel },
            { Dimension.Unit.Percent, UnitPercent }
        };
#endif

        // Keywords
        public static readonly string KeywordInitial = "initial";
        public static readonly string KeywordAuto = "auto";
        public static readonly string KeywordNone = "none";

        public static readonly Dictionary<string, StyleValueKeyword> StringToStyleValueKeywordMap = new Dictionary<string, StyleValueKeyword>()
        {
            { "initial", StyleValueKeyword.Initial },
            { "auto", StyleValueKeyword.Auto },
            { "none", StyleValueKeyword.None }
        };

        public static readonly Dictionary<StyleValueKeyword, string> StyleValueKeywordToStringMap = new Dictionary<StyleValueKeyword, string>()
        {
            { StyleValueKeyword.Initial, "initial" },
            { StyleValueKeyword.Auto, "auto" },
            { StyleValueKeyword.None, "none" }
        };

        // Keyword Lists
        public static readonly List<string> KLDefault = new List<string>() { KeywordInitial };
        public static readonly List<string> KLAuto = new List<string>() { KeywordAuto, KeywordInitial };
        public static readonly List<string> KLNone = new List<string>() { KeywordNone, KeywordInitial };

        public static List<string> GetStyleKeywords(string binding)
        {
            if (string.IsNullOrEmpty(binding))
                return StyleFieldConstants.KLDefault;

            var syntaxParser = new StyleSyntaxParser();
#if UNITY_2019_3_OR_NEWER
            var syntaxFound = StylePropertyCache.TryGetSyntax(binding, out var syntax);
#else
            var syntaxFound = StyleFieldConstants.StylePropertySyntaxCache.TryGetValue(binding, out var syntax);
#endif
            if (!syntaxFound)
                return StyleFieldConstants.KLDefault;

            var expression = syntaxParser.Parse(syntax);
            if (expression == null)
                return StyleFieldConstants.KLDefault;

            var hasAuto = FindKeywordInExpression(expression, StyleFieldConstants.KeywordAuto);
            var hasNone = FindKeywordInExpression(expression, StyleFieldConstants.KeywordNone);

            if (hasAuto)
                return StyleFieldConstants.KLAuto;
            else if (hasNone)
                return StyleFieldConstants.KLNone;

            return StyleFieldConstants.KLDefault;
        }

        static bool FindKeywordInExpression(Expression expression, string keyword)
        {
            if (expression.type == ExpressionType.Keyword && expression.keyword == keyword)
                return true;

            if (expression.subExpressions == null)
                return false;

            foreach (var subExp in expression.subExpressions)
                if (FindKeywordInExpression(subExp, keyword))
                    return true;

            return false;
        }


#if UNITY_2019_2
        public static readonly Dictionary<string, string> StylePropertySyntaxCache = new Dictionary<string, string>()
        {
            {"align-content", "flex-start | flex-end | center | stretch | auto"},
            {"align-items", "flex-start | flex-end | center | stretch | auto"},
            {"align-self", "flex-start | flex-end | center | stretch | auto"},
            {"background-color", "<color>"},
            {"background-image", "<resource> | <url> | none"},
            {"border-bottom-color", "<color>"},
            {"border-bottom-left-radius", "<length>"},
            {"border-bottom-right-radius", "<length>"},
            {"border-bottom-width", "<length>"},
            {"border-color", "<color>{1,4}"},
            {"border-left-color", "<color>"},
            {"border-left-width", "<length>"},
            {"border-radius", "[ <length> ]{1,4}"},
            {"border-right-color", "<color>"},
            {"border-right-width", "<length>"},
            {"border-top-color", "<color>"},
            {"border-top-left-radius", "<length>"},
            {"border-top-right-radius", "<length>"},
            {"border-top-width", "<length>"},
            {"border-width", "<length>{1,4}"},
            {"bottom", "<length> | auto"},
            {"color", "<color>"},
            {"cursor", "[ [ <resource> | <url> ] [ <integer> <integer> ]? ] | [ arrow | text | resize-vertical | resize-horizontal | link | slide-arrow | resize-up-right | resize-up-left | move-arrow | rotate-arrow | scale-arrow | arrow-plus | arrow-minus | pan | orbit | zoom | fps | split-resize-up-down | split-resize-left-right ]"},
            {"display", "flex | none"},
            {"flex", "none | [ <'flex-grow'> <'flex-shrink'>? || <'flex-basis'> ]"},
            {"flex-basis", "<length> | auto"},
            {"flex-direction", "column | row | column-reverse | row-reverse"},
            {"flex-grow", "<number>"},
            {"flex-shrink", "<number>"},
            {"flex-wrap", "nowrap | wrap | wrap-reverse"},
            {"font-size", "<length>"},
            {"height", "<length> | auto"},
            {"justify-content", "flex-start | flex-end | center | space-between | space-around"},
            {"left", "<length> | auto"},
            {"margin", "[ <length> | auto ]{1,4}"},
            {"margin-bottom", "<length> | auto"},
            {"margin-left", "<length> | auto"},
            {"margin-right", "<length> | auto"},
            {"margin-top", "<length> | auto"},
            {"max-height", "<length> | none"},
            {"max-width", "<length> | none"},
            {"min-height", "<length> | auto"},
            {"min-width", "<length> | auto"},
            {"opacity", "<number>"},
            {"overflow", "visible | hidden | scroll"},
            {"padding", "[ <length> ]{1,4}"},
            {"padding-bottom", "<length>"},
            {"padding-left", "<length>"},
            {"padding-right", "<length>"},
            {"padding-top", "<length>"},
            {"position", "relative | absolute"},
            {"right", "<length> | auto"},
            {"top", "<length> | auto"},
            {"-unity-background-image-tint-color", "<color>"},
            {"-unity-background-scale-mode", "stretch-to-fill | scale-and-crop | scale-to-fit"},
            {"-unity-font", "<resource> | <url>"},
            {"-unity-font-style", "normal | italic | bold | bold-and-italic"},
            {"-unity-overflow-clip-box", "padding-box | content-box"},
            {"-unity-slice-bottom", "<integer>"},
            {"-unity-slice-left", "<integer>"},
            {"-unity-slice-right", "<integer>"},
            {"-unity-slice-top", "<integer>"},
            {"-unity-text-align", "upper-left | middle-left | lower-left | upper-center | middle-center | lower-center | upper-right | middle-right | lower-right"},
            {"visibility", "visible | hidden"},
            {"white-space", "normal | nowrap"},
            {"width", "<length> | auto"}
        };
#endif
    }
}
