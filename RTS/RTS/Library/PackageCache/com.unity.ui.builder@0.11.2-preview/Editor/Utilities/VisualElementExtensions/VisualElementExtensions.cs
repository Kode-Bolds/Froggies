using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal static class VisualElementExtensions
    {
        static readonly List<string> s_SkippedAttributeNames = new List<string>()
        {
            "content-container",
            "class",
            "style",
            "template",
        };

        public static List<UxmlAttributeDescription> GetAttributeDescriptions(this VisualElement ve)
        {
            var attributeList = new List<UxmlAttributeDescription>();
            var uxmlQualifiedName = GetUxmlQualifiedName(ve);

            if (!VisualElementFactoryRegistry.TryGetValue(uxmlQualifiedName, out var factoryList))
                return attributeList;

            foreach (IUxmlFactory f in factoryList)
            {
                foreach (var a in f.uxmlAttributesDescription)
                {
                    if (s_SkippedAttributeNames.Contains(a.name))
                        continue;

                    attributeList.Add(a);
                }
            }

            return attributeList;
        }

        static string GetUxmlQualifiedName(VisualElement ve)
        {
            var uxmlQualifiedName = ve.GetType().FullName;

            // Try get uxmlQualifiedName from the UxmlFactory.
            var factoryTypeName = $"{ve.GetType().FullName}+UxmlFactory";
            var asm = ve.GetType().Assembly;
            var factoryType = asm.GetType(factoryTypeName);
            if (factoryType != null)
            {
                var factoryTypeInstance = (IUxmlFactory) Activator.CreateInstance(factoryType);
                if (factoryTypeInstance != null)
                {
                    uxmlQualifiedName = factoryTypeInstance.uxmlQualifiedName;
                }
            }

            return uxmlQualifiedName;
        }

        public static Dictionary<string, string> GetOverriddenAttributes(this VisualElement ve)
        {
            var attributeList = ve.GetAttributeDescriptions();
            var overriddenAttributes = new Dictionary<string, string>();

            foreach (var attribute in attributeList)
            {
                if (attribute == null || attribute.name == null)
                    continue;

                var veType = ve.GetType();
                var camel = BuilderNameUtilities.ConvertDashToCamel(attribute.name);
                var fieldInfo = veType.GetProperty(camel);
                if (fieldInfo != null)
                {
                    var veValueAbstract = fieldInfo.GetValue(ve);
                    if (veValueAbstract == null)
                        continue;

                    var veValueStr = veValueAbstract.ToString();
                    if (veValueStr == "False")
                        veValueStr = "false";
                    else if (veValueStr == "True")
                        veValueStr = "true";

                    var attributeValueStr = attribute.defaultValueAsString;
                    if (veValueStr == attributeValueStr)
                        continue;

                    overriddenAttributes.Add(attribute.name, veValueAbstract.ToString());
                }
            }

            return overriddenAttributes;
        }

        public static VisualTreeAsset GetVisualTreeAsset(this VisualElement element)
        {
            if (element == null)
                return null;

            var obj = element.GetProperty(BuilderConstants.ElementLinkedVisualTreeAssetVEPropertyName);
            if (obj == null)
                return null;

            var vta = obj as VisualTreeAsset;
            return vta;
        }

        public static VisualElementAsset GetVisualElementAsset(this VisualElement element)
        {
            if (element == null)
                return null;

            var obj = element.GetProperty(BuilderConstants.ElementLinkedVisualElementAssetVEPropertyName);
            if (obj == null)
                return null;

            var vea = obj as VisualElementAsset;
            return vea;
        }

        public static StyleSheet GetStyleSheet(this VisualElement element)
        {
            if (element == null)
                return null;

            var obj = element.GetProperty(BuilderConstants.ElementLinkedStyleSheetVEPropertyName);
            if (obj == null)
                return null;

            var styleSheet = obj as StyleSheet;
            return styleSheet;
        }

        public static StyleComplexSelector GetStyleComplexSelector(this VisualElement element)
        {
            if (element == null)
                return null;

            var obj = element.GetProperty(BuilderConstants.ElementLinkedStyleSelectorVEPropertyName);
            if (obj == null)
                return null;

            var scs = obj as StyleComplexSelector;
            return scs;
        }

        public static bool IsLinkedToAsset(this VisualElement element)
        {
            var vta = element.GetVisualTreeAsset();
            if (vta != null)
                return true;

            var vea = element.GetVisualElementAsset();
            if (vea != null)
                return true;

            var styleSheet = element.GetStyleSheet();
            if (styleSheet != null)
                return true;

            var scs = element.GetStyleComplexSelector();
            if (scs != null)
                return true;

            return false;
        }

        public static bool IsSelected(this VisualElement element)
        {
            var vta = element.GetVisualTreeAsset();
            if (vta != null)
                return vta.IsSelected();

            var vea = element.GetVisualElementAsset();
            if (vea != null)
                return vea.IsSelected();

            var styleSheet = element.GetStyleSheet();
            if (styleSheet != null)
                return styleSheet.IsSelected();

            var scs = element.GetStyleComplexSelector();
            if (scs != null)
                return scs.IsSelected();

            return false;
        }

        public static bool IsPartOfCurrentDocument(this VisualElement element)
        {
            return element.GetVisualElementAsset() != null || element.GetVisualTreeAsset() != null || BuilderSharedStyles.IsDocumentElement(element);
        }

        public static StyleSheet GetClosestStyleSheet(this VisualElement element)
        {
            if (element == null)
                return null;

            var ss = element.GetStyleSheet();
            if (ss != null)
                return ss;

            return element.parent.GetClosestStyleSheet();
        }

        public static VisualElement GetClosestElementPartOfCurrentDocument(this VisualElement element)
        {
            if (element == null)
                return null;

            if (element.IsPartOfCurrentDocument())
                return element;

            return element.parent.GetClosestElementPartOfCurrentDocument();
        }

        public static VisualElement GetClosestElementThatIsValid(this VisualElement element, Func<VisualElement, bool> test)
        {
            if (element == null)
                return null;

            if (test(element))
                return element;

            return element.parent.GetClosestElementPartOfCurrentDocument();
        }

        static void FindSelectedElementsRecursive(VisualElement parent, List<VisualElement> selected)
        {
            if (parent.IsSelected())
                selected.Add(parent);

            foreach (var child in parent.Children())
                FindSelectedElementsRecursive(child, selected);
        }

        public static List<VisualElement> FindSelectedElements(this VisualElement element)
        {
            var selected = new List<VisualElement>();

            FindSelectedElementsRecursive(element, selected);

            return selected;
        }

        public static bool IsFocused(this VisualElement element)
        {
            if (element.focusController == null)
                return false;

            return element.focusController.focusedElement == element;
        }

        public static bool HasAncestor(this VisualElement element, VisualElement ancestor)
        {
            if (ancestor == null || element == null)
                return false;

            if (element == ancestor)
                return true;

            return element.parent.HasAncestor(ancestor);
        }

        public static VisualElement GetMinSizeSpecialElement(this VisualElement element)
        {
            foreach (var child in element.Children())
                if (child.name == BuilderConstants.SpecialVisualElementInitialMinSizeName)
                    return child;

            return null;
        }

        public static void HideMinSizeSpecialElement(this VisualElement element)
        {
            var minSizeSpecialElement = element.GetMinSizeSpecialElement();
            if (minSizeSpecialElement == null)
                return;

            minSizeSpecialElement.style.display = DisplayStyle.None;
        }

        public static void UnhideMinSizeSpecialElement(this VisualElement element)
        {
            var minSizeSpecialElement = element.GetMinSizeSpecialElement();
            if (minSizeSpecialElement == null)
                return;

            minSizeSpecialElement.style.display = DisplayStyle.Flex;
        }

        public static void RemoveMinSizeSpecialElement(this VisualElement element)
        {
            var minSizeSpecialElement = element.GetMinSizeSpecialElement();
            if (minSizeSpecialElement == null)
                return;

            minSizeSpecialElement.RemoveFromHierarchy();
        }

        public static VisualElement GetFirstAncestorWithClass(this VisualElement element, string className)
        {
            if (element == null)
                return null;

            if (element.ClassListContains(className))
                return element;

            return element.parent.GetFirstAncestorWithClass(className);
        }

        static CustomStyleProperty<string> s_BuilderElementStyleProperty = new CustomStyleProperty<string>("--builder-style");

        public static void RegisterCustomBuilderStyleChangeEvent(this VisualElement element, Action<BuilderElementStyle> onElementStyleChanged)
        {
            element.RegisterCallback<CustomStyleResolvedEvent>(e =>
            {
                if (e.customStyle.TryGetValue(s_BuilderElementStyleProperty, out var value))
                {
                    if (Enum.TryParse<BuilderElementStyle>(value, true, out var elementStyle))
                        onElementStyleChanged.Invoke(elementStyle);
                    else
                        throw new NotSupportedException($"The `{value}` value is not supported for {s_BuilderElementStyleProperty.name} property.");
                }
            });
        }
    }

    enum BuilderElementStyle
    {
        Default,
        Highlighted
    }
}
