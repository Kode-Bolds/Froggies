using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class BuilderLibrary : BuilderPaneContent
    {
        public enum BuilderLibraryTab
        {
            Controls,
            Project
        }

        public enum LibraryViewMode
        {
            IconTile,
            TreeView
        }

        const string k_UssClassName = "unity-builder-library";
        const string k_ContentContainerName = "content";

        readonly BuilderPaneWindow m_PaneWindow;
        readonly VisualElement m_DocumentElement;
        readonly BuilderSelection m_Selection;
        readonly BuilderLibraryDragger m_Dragger;
        readonly BuilderTooltipPreview m_TooltipPreview;

        readonly ToggleButtonStrip m_HeaderButtonStrip;
        readonly VisualElement m_LibraryContentContainer;

        BuilderLibraryTreeView m_ProjectTreeView;
        BuilderLibraryPlainView m_ControlsPlainView;
        BuilderLibraryTreeView m_ControlsTreeView;

        [SerializeField] bool m_ShowPackageTemplates;
        [SerializeField] LibraryViewMode m_ViewMode = LibraryViewMode.IconTile;
        [SerializeField] BuilderLibraryTab m_ActiveTab = BuilderLibraryTab.Controls;

        public BuilderLibrary(
            BuilderPaneWindow paneWindow, BuilderViewport viewport,
            BuilderSelection selection, BuilderLibraryDragger dragger,
            BuilderTooltipPreview tooltipPreview)
        {
            m_PaneWindow = paneWindow;
            m_DocumentElement = viewport.documentElement;
            m_Selection = selection;
            m_Dragger = dragger;
            m_TooltipPreview = tooltipPreview;

            viewDataKey = "unity-ui-builder-library";

            // Load styles.
            AddToClassList(k_UssClassName);
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(BuilderConstants.LibraryUssPathNoExt + ".uss"));
            styleSheets.Add(EditorGUIUtility.isProSkin
                ? AssetDatabase.LoadAssetAtPath<StyleSheet>(BuilderConstants.LibraryUssPathNoExt + "Dark.uss")
                : AssetDatabase.LoadAssetAtPath<StyleSheet>(BuilderConstants.LibraryUssPathNoExt + "Light.uss"));

            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BuilderConstants.LibraryUssPathNoExt + ".uxml");
            template.CloneTree(this);

            m_LibraryContentContainer = this.Q<VisualElement>(k_ContentContainerName);

            m_HeaderButtonStrip = this.Q<ToggleButtonStrip>();
            m_HeaderButtonStrip.choices = Enum.GetNames(typeof(BuilderLibraryTab));
            m_HeaderButtonStrip.labels = new List<string> { BuilderConstants.LibraryStandardControlsTabName, BuilderConstants.LibraryProjectTabName };
            m_HeaderButtonStrip.RegisterValueChangedCallback(e =>
            {
                m_ActiveTab = (BuilderLibraryTab) Enum.Parse(typeof(BuilderLibraryTab), e.newValue);
                SaveViewData();
                RefreshView();
            });

            AddFocusable(m_HeaderButtonStrip);
            BuilderLibraryContent.OnLibraryContentUpdated += RebuildView;
        }

        protected override void InitEllipsisMenu()
        {
            base.InitEllipsisMenu();

            if (pane == null)
                return;

            pane.AppendActionToEllipsisMenu(BuilderConstants.LibraryShowPackageFiles,
                a => TogglePackageFilesVisibility(),
                a => m_ShowPackageTemplates
                    ? DropdownMenuAction.Status.Checked
                    : DropdownMenuAction.Status.Normal);

            pane.AppendActionToEllipsisMenu(BuilderConstants.LibraryViewModeToggle,
                a => SwitchControlsViewMode(),
                a => m_ViewMode == LibraryViewMode.TreeView
                    ? DropdownMenuAction.Status.Checked
                    : DropdownMenuAction.Status.Normal);
        }

        internal override void OnViewDataReady()
        {
            base.OnViewDataReady();
            OverwriteFromViewData(this, viewDataKey);
            RefreshView();
        }

        public void OnAfterBuilderDeserialize()
        {
            RebuildView();
        }

        void TogglePackageFilesVisibility()
        {
            m_ShowPackageTemplates = !m_ShowPackageTemplates;
            SaveViewData();
            RebuildView();
        }

        internal void SetViewMode(LibraryViewMode viewMode)
        {
            if (m_ViewMode == viewMode)
                return;

            m_ViewMode = viewMode;
            SaveViewData();
            RefreshView();
        }

        void SwitchControlsViewMode()
        {
            SetViewMode(m_ViewMode == LibraryViewMode.IconTile
                ? LibraryViewMode.TreeView
                : LibraryViewMode.IconTile);
        }

        BuilderLibraryTreeView ControlsTreeView
        {
            get
            {
                if (m_ControlsTreeView != null)
                    return m_ControlsTreeView;

                m_ControlsTreeView = new BuilderLibraryTreeView(BuilderLibraryContent.StandardControlsTree);
                m_ControlsTreeView.viewDataKey = "unity-ui-builder-library-controls-tree";
                SetUpLibraryView(m_ControlsTreeView);

                return m_ControlsTreeView;
            }
        }

        BuilderLibraryTreeView ProjectTreeView
        {
            get
            {
                if (m_ProjectTreeView != null)
                    return m_ProjectTreeView;

                var projectContentTree = m_ShowPackageTemplates
                    ? BuilderLibraryContent.ProjectContentTree
                    : BuilderLibraryContent.ProjectContentTreeNoPackages;

                m_ProjectTreeView = new BuilderLibraryTreeView(projectContentTree);
                m_ProjectTreeView.viewDataKey = "unity-ui-builder-library-project-view";
                SetUpLibraryView(m_ProjectTreeView);

                return m_ProjectTreeView;
            }
        }

        BuilderLibraryPlainView ControlsPlainView
        {
            get
            {
                if (m_ControlsPlainView != null)
                    return m_ControlsPlainView;

                m_ControlsPlainView = new BuilderLibraryPlainView(BuilderLibraryContent.StandardControlsTree);
                m_ControlsPlainView.viewDataKey = "unity-ui-builder-library-controls-plane";
                SetUpLibraryView(m_ControlsPlainView);

                return m_ControlsPlainView;
            }
        }

        void SetUpLibraryView(BuilderLibraryView builderLibraryView)
        {
            builderLibraryView.SetupView(m_Dragger, m_TooltipPreview,
                this, m_PaneWindow,
                m_DocumentElement, m_Selection);
        }

        void RebuildView()
        {
            m_LibraryContentContainer.Clear();
            m_ProjectTreeView = null;
            m_ControlsPlainView = null;
            m_ControlsTreeView = null;

            RefreshView();
        }

        void RefreshView()
        {
            m_LibraryContentContainer.Clear();
            m_HeaderButtonStrip.SetValueWithoutNotify(m_ActiveTab.ToString());
            switch (m_ActiveTab)
            {
                case BuilderLibraryTab.Controls:
                    if (m_ViewMode == LibraryViewMode.TreeView)
                        SetActiveView(ControlsTreeView);
                    else
                        SetActiveView(ControlsPlainView);
                    break;

                case BuilderLibraryTab.Project:
                    SetActiveView(ProjectTreeView);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void SetActiveView(BuilderLibraryView builderLibraryView)
        {
            m_LibraryContentContainer.Add(builderLibraryView);
            builderLibraryView.Refresh();
            primaryFocusable = builderLibraryView.PrimaryFocusable;
        }

        public void ResetCurrentlyLoadedUxmlStyles()
        {
            RefreshView();
        }
    }
}
