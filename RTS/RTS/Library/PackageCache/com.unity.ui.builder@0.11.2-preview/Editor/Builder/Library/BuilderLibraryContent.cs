using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Toolbar = UnityEditor.UIElements.Toolbar;

namespace Unity.UI.Builder
{
    static class BuilderLibraryContent
    {
        class AssetModificationProcessor : IBuilderAssetModificationProcessor
        {
            readonly Action m_OnAssetChange;

            public AssetModificationProcessor(Action onAssetChange)
            {
                m_OnAssetChange = onAssetChange;
            }

            public void OnAssetChange()
            {
                // AssetDatabase.FindAllAssets(filter) will return outdated assets if
                // we refresh immediately.

                // Note: This used to rely on Builder.ActiveWindow.rootVisualElement.schedule
                // when not in batch mode to do the delay call but this caused problems with
                // tests that waited on the LibraryContent to refresh based on asset changes
                // before the any UI Builder window was open.
                EditorApplication.delayCall += m_OnAssetChange.Invoke;
            }

            public AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath)
                => AssetMoveResult.DidNotMove;

            public AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions option)
                => AssetDeleteResult.DidNotDelete;
        }

        static readonly Dictionary<Type, BuilderLibraryTreeItem> s_ControlsTypeCache = new Dictionary<Type, BuilderLibraryTreeItem>();
        static readonly BuilderLibraryProjectScanner s_ProjectAssetsScanner  = new BuilderLibraryProjectScanner();
        static int s_ProjectUxmlPathsHash;

        public static event Action OnLibraryContentUpdated;
        public static List<ITreeViewItem> StandardControlsTree { get; private set; }
        public static List<ITreeViewItem> ProjectContentTree { get; private set; }
        public static List<ITreeViewItem> ProjectContentTreeNoPackages { get;  private set; }

        static BuilderLibraryContent()
        {
            RegenerateLibraryContent();
            BuilderAssetModificationProcessor.Register(new AssetModificationProcessor(() =>
            {
                if (s_ProjectUxmlPathsHash != s_ProjectAssetsScanner.GetAllProjectUxmlFilePathsHash())
                    RegenerateLibraryContent();
            }));
        }

        static void RegenerateLibraryContent()
        {
            GenerateControlsItemsTree();
            GenerateProjectContentTrees();

            UpdateControlsTypeCache(ProjectContentTree);
            UpdateControlsTypeCache(StandardControlsTree);
            s_ProjectUxmlPathsHash = s_ProjectAssetsScanner.GetAllProjectUxmlFilePathsHash();

            OnLibraryContentUpdated?.Invoke();
        }

        internal static void ResetProjectUxmlPathsHash()
        {
            s_ProjectUxmlPathsHash = default;
        }

        public static Texture2D GetTypeLibraryIcon(Type type)
        {
            if (s_ControlsTypeCache.TryGetValue(type, out var builderLibraryTreeItem))
                return builderLibraryTreeItem.Icon;

            // Just in case to avoid infinity loop.
            if (type != typeof(VisualElement))
                return GetTypeLibraryIcon(typeof(VisualElement));

            return null;
        }

        internal static BuilderLibraryTreeItem GetLibraryItemForType(Type type)
        {
            return s_ControlsTypeCache.TryGetValue(type, out var builderLibraryTreeItem)
                ? builderLibraryTreeItem
                : null;
        }

        public static Texture2D GetTypeDarkSkinLibraryIcon(Type type)
        {
            if (s_ControlsTypeCache.TryGetValue(type, out var builderLibraryTreeItem))
                return builderLibraryTreeItem.DarkSkinIcon;

            return null;
        }

        public static Texture2D GetUXMLAssetIcon(string uxmlAssetPath)
        {
            return GetUXMLAssetIcon(ProjectContentTree, uxmlAssetPath);
        }

        static Texture2D GetUXMLAssetIcon(IEnumerable<ITreeViewItem> items, string uxmlAssetPath)
        {
            foreach (var item in items)
            {
                if (item is BuilderLibraryTreeItem builderLibraryTreeItem)
                {
                    if (!string.IsNullOrEmpty(builderLibraryTreeItem.SourceAssetPath) && builderLibraryTreeItem.SourceAssetPath.Equals(uxmlAssetPath))
                    {
                        return builderLibraryTreeItem.Icon;
                    }

                    if (item.hasChildren)
                    {
                        var icon =  GetUXMLAssetIcon(item.children, uxmlAssetPath);
                        if (icon != null)
                            return icon;
                    }
                }
            }

            return (Texture2D)EditorGUIUtility.IconContent("UxmlScript Icon").image;
        }

        static void GenerateProjectContentTrees()
        {
            ProjectContentTree = new List<ITreeViewItem>();
            ProjectContentTreeNoPackages = new List<ITreeViewItem>();

            var fromProjectCategory = new BuilderLibraryTreeItem(BuilderConstants.LibraryAssetsSectionHeaderName, null, null, null) { IsHeader = true };
            s_ProjectAssetsScanner.ImportUxmlFromProject(fromProjectCategory, true);
            ProjectContentTree.Add(fromProjectCategory);

            var fromProjectCategoryNoPackages = new BuilderLibraryTreeItem(BuilderConstants.LibraryAssetsSectionHeaderName, null, null, null) { IsHeader = true };
            s_ProjectAssetsScanner.ImportUxmlFromProject(fromProjectCategoryNoPackages, false);
            ProjectContentTreeNoPackages.Add(fromProjectCategoryNoPackages);

            var customControlsCategory = new BuilderLibraryTreeItem(BuilderConstants.LibraryCustomControlsSectionHeaderName, null,null, null) { IsHeader = true };
            s_ProjectAssetsScanner.ImportFactoriesFromSource(customControlsCategory);
            if (customControlsCategory.hasChildren)
            {
                ProjectContentTree.Add(customControlsCategory);
                ProjectContentTreeNoPackages.Add(customControlsCategory);
            }
        }

        static void GenerateControlsItemsTree()
        {
            StandardControlsTree = new List<ITreeViewItem>();
            var containersItem = new BuilderLibraryTreeItem(BuilderConstants.LibraryContainersSectionHeaderName, null, null, null) { IsHeader = true };
            IList<ITreeViewItem> containersItemList = new List<ITreeViewItem>
            {
                new BuilderLibraryTreeItem("VisualElement", nameof(VisualElement), typeof(VisualElement), () =>
                    {
                        var ve = new VisualElement();
                        var veMinSizeChild = new VisualElement();
                        veMinSizeChild.name = BuilderConstants.SpecialVisualElementInitialMinSizeName;
                        veMinSizeChild.AddToClassList(BuilderConstants.SpecialVisualElementInitialMinSizeClassName);
                        ve.Add(veMinSizeChild);
                        return ve;
                    },
                    (inVta, inParent) =>
                    {
                        var vea = new VisualElementAsset(typeof(VisualElement).ToString());
                        VisualTreeAssetUtilities.InitializeElement(vea);
                        inVta.AddElement(inParent, vea);
                        return vea;
                    }),
                new BuilderLibraryTreeItem("ScrollView", nameof(ScrollView), typeof(ScrollView),() => new ScrollView()),
                new BuilderLibraryTreeItem("ListView", nameof(ListView), typeof(ListView),() => new ListView()),
                new BuilderLibraryTreeItem("IMGUI Container", "VisualElement", typeof(IMGUIContainer),() => new IMGUIContainer()),
            };
            containersItem.AddChildren(containersItemList);
            StandardControlsTree.Add(containersItem);

            var controlsItem = new BuilderLibraryTreeItem(BuilderConstants.LibraryControlsSectionHeaderName, null, null, null) { IsHeader = true };
            IList<ITreeViewItem> controlsItemList = new List<ITreeViewItem>
            {
                new BuilderLibraryTreeItem("Label", nameof(Label), typeof(Label),() => new Label("Label")),
                new BuilderLibraryTreeItem("Button", nameof(Button), typeof(Button), () => new Button { text = "Button" }),
                new BuilderLibraryTreeItem("Toggle", nameof(Toggle), typeof(Toggle), () => new Toggle("Toggle")),
                new BuilderLibraryTreeItem("Scroller", nameof(Scroller), typeof(Scroller), () => new Scroller(0, 100, (v) => { }, SliderDirection.Horizontal) { value = 42 }),
                new BuilderLibraryTreeItem("Text Field", nameof(TextField), typeof(TextField), () => new TextField("Text Field") { value = "filler text" }),
                new BuilderLibraryTreeItem("Object Field", nameof(ObjectField), typeof(ObjectField), () => new ObjectField("Object Field") { value = new Texture2D(10, 10) { name = "new_texture" } }),
                new BuilderLibraryTreeItem("Foldout", nameof(Foldout), typeof(Foldout), () => new Foldout { text = "Foldout"}),
                new BuilderLibraryTreeItem("Numeric Fields", null, null, null, null, new List<TreeViewItem<string>>()
                {
                    new BuilderLibraryTreeItem("Integer", nameof(IntegerField), typeof(IntegerField), () => new IntegerField("Int Field") { value = 42 }),
                    new BuilderLibraryTreeItem("Float", nameof(FloatField), typeof(FloatField), () => new FloatField("Float Field") { value = 42.2f }),
                    new BuilderLibraryTreeItem("Long", nameof(LongField), typeof(LongField), () => new LongField("Long Field") { value = 42 }),
                    new BuilderLibraryTreeItem("Min-Max Slider", nameof(MinMaxSlider), typeof(MinMaxSlider), () => new MinMaxSlider("Min/Max Slider", 0, 20, -10, 40) { value = new Vector2(10, 12) }),
                    new BuilderLibraryTreeItem("Slider", nameof(Slider), typeof(Slider), () => new Slider("Slider", 0, 100) { value = 42 }),
                    new BuilderLibraryTreeItem("Progress Bar", nameof(ProgressBar), typeof(ProgressBar), () => new ProgressBar() { title = "my-progress", value = 22 } ),
                    new BuilderLibraryTreeItem("Vector2", nameof(Vector2Field), typeof(Vector2Field), () => new Vector2Field("Vec2 Field")),
                    new BuilderLibraryTreeItem("Vector3", nameof(Vector3Field), typeof(Vector3Field), () => new Vector3Field("Vec3 Field")),
                    new BuilderLibraryTreeItem("Vector4", nameof(Vector4Field), typeof(Vector4Field), () => new Vector4Field("Vec4 Field")),
                    new BuilderLibraryTreeItem("Rect", nameof(RectField), typeof(RectField), () => new RectField("Rect")),
                    new BuilderLibraryTreeItem("Bounds", nameof(BoundsField), typeof(BoundsField), () => new BoundsField("Bounds")),
                    new BuilderLibraryTreeItem("Slider (Int)", nameof(SliderInt), typeof(SliderInt), () => new SliderInt("SliderInt", 0, 100) { value = 42 }),
                    new BuilderLibraryTreeItem("Vector2 (Int)", nameof(Vector2IntField), typeof(Vector2IntField), () => new Vector2IntField("Vector2Int")),
                    new BuilderLibraryTreeItem("Vector3 (Int)", nameof(Vector3IntField), typeof(Vector3IntField), () => new Vector3IntField("Vector3Int")),
                    new BuilderLibraryTreeItem("Rect (Int)", nameof(RectIntField), typeof(RectIntField), () => new RectIntField("RectInt")),
                    new BuilderLibraryTreeItem("Bounds (Int)", nameof(BoundsIntField), typeof(BoundsIntField), () => new BoundsIntField("BoundsInt"))
                }),
                new BuilderLibraryTreeItem("Value Fields", null ,null, null, null, new List<TreeViewItem<string>>()
                {
                    new BuilderLibraryTreeItem("Color", nameof(ColorField), typeof(ColorField), () => new ColorField("Color") { value = Color.cyan }),
                    new BuilderLibraryTreeItem("Curve", nameof(CurveField), typeof(CurveField), () => new CurveField("Curve") {
                        value = new AnimationCurve(new Keyframe(0, 0), new Keyframe(5, 8), new Keyframe(10, 4))
                    }),
                    new BuilderLibraryTreeItem("Gradient", nameof(GradientField), typeof(GradientField), () => new GradientField("Gradient") {
                        value = new Gradient()
                        {
                            colorKeys = new[]
                            {
                                new GradientColorKey(Color.red, 0),
                                new GradientColorKey(Color.blue, 10),
                                new GradientColorKey(Color.green, 20)
                            }
                        }
                    })
                }),
                new BuilderLibraryTreeItem("Choice Fields", null ,null, null, null, new List<TreeViewItem<string>>
                {
                    new BuilderLibraryTreeItem("Enum", nameof(EnumField), typeof(EnumField), () => new EnumField("Enum", TextAlignment.Center)),

                    // No UXML support for PopupField.
                    //new LibraryTreeItem("Popup", () => new PopupField<string>("Normal Field", choices, 0)),

                    new BuilderLibraryTreeItem("Tag", nameof(TagField), typeof(TagField), () => new TagField("Tag", "Player")),
                    new BuilderLibraryTreeItem("Mask", nameof(MaskField), typeof(MaskField), () => new MaskField("Mask")),
                    new BuilderLibraryTreeItem("Layer", nameof(LayerField), typeof(LayerField), () => new LayerField("Layer")),
                    new BuilderLibraryTreeItem("LayerMask", nameof(LayerMaskField), typeof(LayerMaskField), () => new LayerMaskField("LayerMask"))
                }),
                new BuilderLibraryTreeItem("Toolbar", null, null, null, null, new List<TreeViewItem<string>>()
                {
                    new BuilderLibraryTreeItem("Toolbar", "ToolbarElement", typeof(Toolbar), () => new Toolbar()),
                    new BuilderLibraryTreeItem("Toolbar Menu", "ToolbarElement", typeof(ToolbarMenu), () => new ToolbarMenu()),
                    new BuilderLibraryTreeItem("Toolbar Button", "ToolbarElement", typeof(ToolbarButton), () => new ToolbarButton { text = "Button" }),
                    new BuilderLibraryTreeItem("Toolbar Spacer", "ToolbarElement", typeof(ToolbarSpacer), () => new ToolbarSpacer()),
                    new BuilderLibraryTreeItem("Toolbar Toggle", "ToolbarElement", typeof(ToolbarToggle), () => new ToolbarToggle { label = "Toggle" }),
#if UNITY_2019_3_OR_NEWER
                    new BuilderLibraryTreeItem("Toolbar Breadcrumbs", "ToolbarElement", typeof(ToolbarBreadcrumbs), () => new ToolbarBreadcrumbs()),
#endif
                    new BuilderLibraryTreeItem("Toolbar Search Field", "ToolbarElement", typeof(ToolbarSearchField), () => new ToolbarSearchField ()),
                    new BuilderLibraryTreeItem("Toolbar Popup Search Field", "ToolbarElement", typeof(ToolbarPopupSearchField), () => new ToolbarPopupSearchField ()),
                }),
                new BuilderLibraryTreeItem("Inspectors", null ,null, null, null, new List<TreeViewItem<string>>()
                {
                    new BuilderLibraryTreeItem("PropertyField", nameof(PropertyField), typeof(PropertyField), () => new PropertyField())
                }),
            };

            controlsItem.AddChildren(controlsItemList);
            StandardControlsTree.Add(controlsItem);
        }

        static void UpdateControlsTypeCache(IEnumerable<ITreeViewItem> items)
        {
            foreach (var item in items)
            {
                if (item is BuilderLibraryTreeItem builderLibraryTreeItem)
                {
                    if (builderLibraryTreeItem.Type != null)
                        s_ControlsTypeCache[builderLibraryTreeItem.Type] = builderLibraryTreeItem;

                    if (item.hasChildren)
                        UpdateControlsTypeCache(item.children);
                }
            }
        }
    }
}
