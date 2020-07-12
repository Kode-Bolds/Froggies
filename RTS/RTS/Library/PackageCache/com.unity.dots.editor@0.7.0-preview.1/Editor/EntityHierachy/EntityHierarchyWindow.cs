using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Entities.Editor
{
    interface IEntityHierarchy
    {
        IEntityHierarchyGroupingStrategy Strategy { get; }

        EntityQueryDesc QueryDesc { get; }

        World World { get; }

        void OnStructuralChangeDetected();
    }

    class EntityHierarchyWindow : DOTSEditorWindow, IEntityHierarchy
    {
        static readonly string k_WindowName = L10n.Tr("Entities");

        // Matches SceneHierarchy's min size
        static readonly Vector2 k_MinWindowSize = new Vector2(200, 200);

        EntityHierarchyTreeView m_TreeView;

        [MenuItem(Constants.MenuItems.EntityHierarchyWindow, false, Constants.MenuItems.WindowPriority)]
        static void OpenWindow() => GetWindow<EntityHierarchyWindow>().Show();

        public IEntityHierarchyGroupingStrategy Strategy { get; private set; }

        public EntityQueryDesc QueryDesc { get; private set; }

        public World World { get; private set; }

        void OnEnable()
        {
            titleContent = new GUIContent(k_WindowName, EditorIcons.EntityGroup);
            minSize = k_MinWindowSize;

            Resources.Templates.CommonResources.AddStyles(rootVisualElement);
            Resources.Templates.DotsEditorCommon.AddStyles(rootVisualElement);
            rootVisualElement.AddToClassList(UssClasses.Resources.EntityHierarchy);

            CreateToolbar();
            CreateTreeView();
            RefreshTreeView();
        }

        void OnDisable()
        {
            m_TreeView.Dispose();
            if (Strategy != null)
            {
                EntityHierarchyDiffSystem.Unregister(this);
                Strategy.Dispose();
            }
        }

        void CreateToolbar()
        {
            Resources.Templates.EntityHierarchyToolbar.Clone(rootVisualElement);
            var leftSide = rootVisualElement.Q<VisualElement>(className: UssClasses.EntityHierarchyWindow.Toolbar.LeftSide);
            var rightSide = rootVisualElement.Q<VisualElement>(className: UssClasses.EntityHierarchyWindow.Toolbar.RightSide);

            leftSide.Add(CreateWorldSelector());

            AddSearchIcon(rightSide, UssClasses.DotsEditorCommon.SearchIcon);
            AddSearchFieldContainer(rootVisualElement, UssClasses.DotsEditorCommon.SearchFieldContainer);
        }

        void CreateTreeView()
        {
            m_TreeView = new EntityHierarchyTreeView();
            rootVisualElement.Add(m_TreeView);
        }

        void RefreshTreeView() => m_TreeView?.Refresh(this);

        void IEntityHierarchy.OnStructuralChangeDetected() => m_TreeView?.UpdateStructure();

        protected override void OnWorldSelected(World world)
        {
            if (world == World)
                return;

            // Maybe keep the previous strategy to keep its state
            // and reuse it when switching back to it.
            if (Strategy != null)
            {
                EntityHierarchyDiffSystem.Unregister(this);
                Strategy.Dispose();
            }

            World = world;
            Strategy = new EntityHierarchyDefaultGroupingStrategy(world);
            EntityHierarchyDiffSystem.Register(this);

            RefreshTreeView();
        }

        protected override void OnFilterChanged(string filter) {}

        protected override void OnUpdate() => m_TreeView.OnUpdate();
    }
}
