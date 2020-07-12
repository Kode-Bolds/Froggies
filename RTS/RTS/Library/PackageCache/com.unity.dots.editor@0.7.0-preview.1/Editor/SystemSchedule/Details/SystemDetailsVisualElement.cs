using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Entities.Editor
{
    class SystemDetailsVisualElement : VisualElement
    {
        static readonly string k_QueriesTitle = L10n.Tr("Queries");
        static readonly string k_QueriesMatchTitle = L10n.Tr("Matches");
        static readonly string k_SchedulingTitle = L10n.Tr("Scheduling");
        static readonly string k_ShowDependencies = L10n.Tr("Show Dependencies");
        static readonly string k_ShowLess = L10n.Tr("Show less");

        EntityQuery[] m_Query;
        string m_SearchFilter;
        Toolbar m_SystemDetailToolbar;
        VisualElement m_HeaderRightSide;
        VisualElement m_SystemIcon;
        Label m_SystemNameLabel;
        VisualElement m_ScriptIcon;
        VisualElement m_AllQueryResultContainer;
        bool m_ShowMoreBool = true;
        VisualElement m_ContentSectionRoot;

        public VisualElement Parent { get; set; }
        public static event Action<string> OnAddFilter;
        public static event Action<string> OnRemoveFilter;

        SystemTreeViewItem m_Target;
        public SystemTreeViewItem Target
        {
            get => m_Target;

            set
            {
                if (m_Target == value)
                    return;

                m_Target = value;

                UpdateContent();
            }
        }

        SystemTreeViewItem m_LastSelectedItem;
        public SystemTreeViewItem LastSelectedItem
        {
            get => m_LastSelectedItem;

            set { m_LastSelectedItem = value; }
        }

        public string SearchFilter
        {
            get => m_SearchFilter;
            set
            {
                if (m_SearchFilter == value)
                    return;

                m_SearchFilter = value;

                m_Query = null;

                UpdateQueryResults();
                UpdateDependencyToggle();
            }
        }

        public SystemDetailsVisualElement()
        {
            Resources.Templates.CommonResources.AddStyles(this);
            Resources.Templates.SystemScheduleDetailContent.Clone(this);

            m_ContentSectionRoot = this.Q(className: UssClasses.SystemScheduleWindow.Detail.Content);
            m_ContentSectionRoot.style.display = DisplayStyle.Flex;

            CreateToolBarForDetailSection();
            CreateQueryResultSection();
            CreateScheduleFilterSection();
        }

        void UpdateContent()
        {
            if (null == Target)
                return;

            if (Target.System != null && Target.System.World == null)
                return;

            switch (Target.System)
            {
                case null:
                {
                    if ((null != Parent) && Parent.Contains(this))
                        Parent.Remove(this);

                    return;
                }
            }

            UpdateSystemIconName();
            UpdateQueryResults();
            UpdateDependencyToggle();
        }

        string m_LastIconStyle = string.Empty;

        void UpdateSystemIconName()
        {
            var currentIconStyle = GetDetailSystemClass(Target.System);
            if (string.Compare(currentIconStyle, m_LastIconStyle) != 0)
            {
                m_SystemIcon.RemoveFromClassList(m_LastIconStyle);
                m_SystemIcon.AddToClassList(GetDetailSystemClass(Target.System));
                m_LastIconStyle = currentIconStyle;
            }

            var systemName = Target.System.GetType().Name;
            m_SystemNameLabel.text = systemName;

            var scriptFound = SearchForScript(systemName);
            if (scriptFound)
            {
                if (!m_SystemDetailToolbar.Contains(m_HeaderRightSide))
                    m_SystemDetailToolbar.Add(m_HeaderRightSide);

                m_ScriptIcon.RegisterCallback<MouseUpEvent, UnityEngine.Object>((evt, payload) =>
                {
                    AssetDatabase.OpenAsset(payload);
                    evt.StopPropagation();
                }, scriptFound);
            }
            else
            {
                if (m_SystemDetailToolbar.Contains(m_HeaderRightSide))
                    m_SystemDetailToolbar.Remove(m_HeaderRightSide);
            }
        }

        void UpdateDependencyToggle()
        {
            if (Target?.System == null)
                return;

            var schedulingToggle = this.Q<ToolbarToggle>(className: UssClasses.SystemScheduleWindow.Detail.SchedulingToggle);
            schedulingToggle.text = k_ShowDependencies;
            schedulingToggle.value = SearchUtility.CheckIfStringContainsGivenTokenAndName(m_SearchFilter, Constants.SystemSchedule.k_SystemDependencyToken,  Target.System.GetType().Name);

            schedulingToggle.RegisterValueChangedCallback(evt =>
            {
                var systemTypeName = Target.System.GetType().Name;
                var searchString = Constants.SystemSchedule.k_SystemDependencyToken + systemTypeName;

                if (schedulingToggle.value)
                {
                    OnAddFilter?.Invoke(searchString);
                }
                else
                {
                    if (m_SearchFilter.Contains(Constants.SystemSchedule.k_SystemDependencyToken + " " + systemTypeName))
                        searchString = Constants.SystemSchedule.k_SystemDependencyToken + " " + systemTypeName;

                    OnRemoveFilter?.Invoke(searchString);
                }
            });
        }

        void UpdateQueryResults()
        {
            if (Target?.System == null)
                return;

            var currentQueries = Target.System.EntityQueries;
            if (m_Query == currentQueries)
                return;

            m_AllQueryResultContainer.Clear();
            m_Query = currentQueries;

            var index = 0;
            var toAddList = new List<VisualElement>();

            // Query result for each row.
            foreach (var query in m_Query)
            {
                var eachRowContainer = new VisualElement();
                Resources.Templates.CommonResources.AddStyles(eachRowContainer);
                Resources.Templates.SystemScheduleDetailQuery.Clone(eachRowContainer);

                // Sort the components by their access mode, readonly, readwrite, etc.
                using (var queryTypePooledList = query.GetQueryTypes().ToPooledList())
                using (var readWriteQueryTypePooledList = query.GetReadAndWriteTypes().ToPooledList())
                {
                    var queryTypeList = queryTypePooledList.List;
                    var readWriteTypeList = readWriteQueryTypePooledList.List;
                    queryTypeList.Sort(EntityQueryUtility.CompareTypes);

                    // Icon container
                    var queryIcon = eachRowContainer.Q(className: UssClasses.SystemScheduleWindow.Detail.QueryIconName);
                    queryIcon.style.flexShrink = 1;
                    queryIcon.AddToClassList(UssClasses.SystemScheduleWindow.Detail.QueryIcon);

                    var allComponentContainer = eachRowContainer.Q(className: UssClasses.SystemScheduleWindow.Detail.AllComponentContainer);
                    foreach (var queryType in queryTypeList)
                    {
                        var componentManagedType = queryType.GetManagedType();
                        var componentTypeName = EntityQueryUtility.SpecifiedTypeName(componentManagedType);

                        // Component toggle container.
                        var componentTypeNameToggleContainer = new ComponentToggleWithAccessMode(GetAccessModeStyle(queryType, readWriteTypeList));
                        var componentTypeNameToggle = componentTypeNameToggleContainer.ComponentTypeNameToggle;

                        componentTypeNameToggle.text = componentTypeName;
                        componentTypeNameToggle.value = SearchUtility.CheckIfStringContainsGivenTokenAndName(m_SearchFilter, Constants.SystemSchedule.k_ComponentToken, componentTypeName);
                        componentTypeNameToggle.RegisterValueChangedCallback(evt =>
                        {
                            HandleComponentsAddRemoveEvents(evt, componentTypeNameToggle, componentTypeName);
                        });
                        allComponentContainer.Add(componentTypeNameToggleContainer);
                    }
                }

                // Entity match label
                var matchCountContainer = eachRowContainer.Q(className: UssClasses.SystemScheduleWindow.Detail.EntityMatchCountContainer);
                var matchCountLabel = new EntityMatchCountVisualElement { Query = query, CurrentWorld = Target.System.World};
                matchCountContainer.Add(matchCountLabel);

                // Show more to unfold the results or less to fold.
                if (index < Constants.SystemSchedule.k_ShowMinimumQueryCount)
                {
                    m_AllQueryResultContainer.Add(eachRowContainer);
                }
                else
                {
                    toAddList.Add(eachRowContainer);
                }

                index++;
            }

            var queryHideCount = m_Query.Length - Constants.SystemSchedule.k_ShowMinimumQueryCount;
            if (toAddList.Any())
                FoldPartOfResults(m_AllQueryResultContainer, toAddList, queryHideCount);
        }

        static ComponentType.AccessMode GetAccessModeStyle(ComponentType queryType, IReadOnlyCollection<ComponentType> readWriteTypeList)
        {
            if (null == readWriteTypeList)
                return queryType.AccessModeType;

            if (queryType.AccessModeType == ComponentType.AccessMode.Exclude)
            {
                return ComponentType.AccessMode.Exclude;
            }

            foreach (var readWriteType in readWriteTypeList)
            {
                if (readWriteType.TypeIndex == queryType.TypeIndex)
                {
                    return readWriteType.AccessModeType;
                }
            }

            return ComponentType.AccessMode.ReadWrite;
        }

        void FoldPartOfResults(VisualElement allQueryResultContainer, IReadOnlyCollection<VisualElement> toAddList, int queryHideCount)
        {
            var showMoreLessLabel = new CustomLabelWithUnderline();
            showMoreLessLabel.AddToClassList(UssClasses.SystemScheduleWindow.Detail.ShowMoreLessLabel);

            var showMoreText = queryHideCount > 1
                ? $"Show {queryHideCount.ToString()} more queries"
                : $"Show {queryHideCount.ToString()} more query";

            allQueryResultContainer.Add(showMoreLessLabel);

            ShowMoreOrLess(showMoreLessLabel, showMoreText, toAddList, allQueryResultContainer);

            showMoreLessLabel.RegisterCallback<MouseUpEvent>(evt =>
            {
                m_ShowMoreBool = !m_ShowMoreBool;

                ShowMoreOrLess(showMoreLessLabel, showMoreText, toAddList, allQueryResultContainer);
            });
        }

        void ShowMoreOrLess(CustomLabelWithUnderline showMoreLessLabel, string showMoreText,
            IReadOnlyCollection<VisualElement> toAddList, VisualElement allQueryResultContainer)
        {
            if (m_ShowMoreBool)
            {
                showMoreLessLabel.text = showMoreText;
                foreach (var eachRow in toAddList)
                {
                    if (allQueryResultContainer.Contains(eachRow))
                        allQueryResultContainer.Remove(eachRow);
                }
            }
            else
            {
                showMoreLessLabel.text = k_ShowLess;
                var index = allQueryResultContainer.IndexOf(showMoreLessLabel);
                foreach (var eachRow in toAddList)
                {
                    if (!allQueryResultContainer.Contains(eachRow))
                        allQueryResultContainer.Insert(index - 1, eachRow);
                }
            }
        }

        void HandleComponentsAddRemoveEvents(ChangeEvent<bool> evt, CustomToolbarToggle componentTypeNameToggle, string componentTypeName)
        {
            componentTypeNameToggle.value = evt.newValue;

            var searchString = Constants.SystemSchedule.k_ComponentToken + componentTypeName;
            if (componentTypeNameToggle.value)
            {
                OnAddFilter?.Invoke(searchString);
            }
            else
            {
                if (m_SearchFilter.Contains(Constants.SystemSchedule.k_ComponentToken + " " + componentTypeName))
                    searchString = Constants.SystemSchedule.k_ComponentToken + " " + componentTypeName;

                OnRemoveFilter?.Invoke(searchString);
            }
        }

        static string GetDetailSystemClass(ComponentSystemBase system)
        {
            switch (system)
            {
                case null:
                    return "";
                case EntityCommandBufferSystem _:
                    return UssClasses.SystemScheduleWindow.Detail.CommandBufferIcon;
                case ComponentSystemGroup _:
                    return UssClasses.SystemScheduleWindow.Detail.GroupIcon;
                case ComponentSystemBase _:
                    return UssClasses.SystemScheduleWindow.Detail.SystemIcon;
            }
        }

        void CreateToolBarForDetailSection()
        {
            m_SystemDetailToolbar = new Toolbar();

            Resources.Templates.CommonResources.AddStyles(m_SystemDetailToolbar);
            Resources.Templates.SystemScheduleDetailHeader.Clone(m_SystemDetailToolbar);

            m_SystemDetailToolbar.style.justifyContent = Justify.SpaceBetween;
            m_SystemDetailToolbar.RegisterCallback<MouseUpEvent>(evt =>
            {
                if (UIElementHelper.IsVisible(m_ContentSectionRoot))
                {
                    UIElementHelper.Hide(m_ContentSectionRoot);
                }
                else
                {
                    UIElementHelper.Show(m_ContentSectionRoot);
                }
            });

            // Left side
            m_SystemIcon = m_SystemDetailToolbar.Q(className: UssClasses.SystemScheduleWindow.Detail.SystemIconName);
            m_SystemNameLabel = m_SystemDetailToolbar.Q<Label>(className: UssClasses.SystemScheduleWindow.Detail.SystemNameLabel);

            // Middle section
            var middleSection = m_SystemDetailToolbar.Q(className: UssClasses.SystemScheduleWindow.Detail.MiddleSection);
            var resizeBar = new Label();
            resizeBar.AddToClassList(UssClasses.SystemScheduleWindow.Detail.ResizeBar);
            middleSection.Add(resizeBar);

            // Right side
            m_HeaderRightSide = m_SystemDetailToolbar.Q(className: "system-schedule-detail__header-right");
            m_ScriptIcon = m_SystemDetailToolbar.Q(className: UssClasses.SystemScheduleWindow.Detail.ScriptsIconName);
            m_ScriptIcon.AddToClassList(UssClasses.SystemScheduleWindow.Detail.ScriptsIcon);

            this.Insert(0, m_SystemDetailToolbar);
        }

        static UnityEngine.Object SearchForScript(string systemName)
        {
            var assets = AssetDatabase.FindAssets(systemName + Constants.SystemSchedule.k_ScriptType);
            return assets.Select(asset => AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(asset))).FirstOrDefault(a => a.name == systemName);
        }

        void CreateQueryResultSection()
        {
            var queryTitleLabel = this.Q<Label>(className: UssClasses.SystemScheduleWindow.Detail.QueryTitleLabel);
            queryTitleLabel.text = k_QueriesTitle;

            var matchTitleLabel = this.Q<Label>(className: UssClasses.SystemScheduleWindow.Detail.MatchTitleLabel);
            matchTitleLabel.text = k_QueriesMatchTitle;

            m_AllQueryResultContainer = this.Q(className: UssClasses.SystemScheduleWindow.Detail.QueryRow2);
        }

        void CreateScheduleFilterSection()
        {
            var schedulingTitle = this.Q<Label>(className: UssClasses.SystemScheduleWindow.Detail.SchedulingTitle);
            schedulingTitle.text = k_SchedulingTitle;
        }
    }
}
