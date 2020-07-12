using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Editor.Bridge;
using UnityEngine.UIElements;

namespace Unity.Entities.Editor
{
    class SystemScheduleTreeView : VisualElement
    {
        readonly TreeView SystemTreeView;
        readonly IList<ITreeViewItem> m_TreeRootItems = new List<ITreeViewItem>();
        SystemDetailsVisualElement m_SystemDetailsVisualElement;
        SystemTreeViewItem m_LastSelectedItem;
        int m_LastSelectedItemId;
        World m_World;
        List<Type> m_SystemDependencyList = new List<Type>();

        public string SearchFilter { get; set; }

        /// <summary>
        /// Constructor of the tree view.
        /// </summary>
        public SystemScheduleTreeView()
        {
            SystemTreeView = new TreeView(m_TreeRootItems, 20, MakeItem, BindItem);
            SystemTreeView.viewDataKey = Constants.State.ViewDataKeyPrefix + nameof(SystemScheduleTreeView);
            SystemTreeView.style.flexGrow = 1;
            Add(SystemTreeView);
            CreateSystemDetailsSection();
        }

        void CreateSystemDetailsSection()
        {
            m_SystemDetailsVisualElement = new SystemDetailsVisualElement();
            SystemTreeView.onSelectionChange += (selectedItems) =>
            {
                var item = selectedItems.OfType<SystemTreeViewItem>().FirstOrDefault();
                if (null == item)
                    return;

                switch (item.System)
                {
                    case null:
                    {
                        if (this.Contains(m_SystemDetailsVisualElement))
                            Remove(m_SystemDetailsVisualElement);

                        return;
                    }
                }

                // Remember last selected item id so that query information can be properly updated.
                m_LastSelectedItemId = item.id;
                m_LastSelectedItem = item;

                // Start fresh.
                if (this.Contains(m_SystemDetailsVisualElement))
                    Remove(m_SystemDetailsVisualElement);

                m_SystemDetailsVisualElement.Target = item;
                m_SystemDetailsVisualElement.SearchFilter = SearchFilter;
                m_SystemDetailsVisualElement.Parent = this;
                m_SystemDetailsVisualElement.LastSelectedItem = m_LastSelectedItem;
                this.Add(m_SystemDetailsVisualElement);
            };
        }

        VisualElement MakeItem()
        {
            var systemItem = SystemSchedulePool.GetSystemInformationVisualElement(this);
            systemItem.World = m_World;
            return systemItem;
        }

        public void Refresh(World world, bool showInactiveSystems)
        {
            if ((m_World != world) && (this.Contains(m_SystemDetailsVisualElement)))
                this.Remove(m_SystemDetailsVisualElement);

            m_World = world;

            foreach (var root in m_TreeRootItems.OfType<SystemTreeViewItem>())
            {
                root.ReturnToPool();
            }
            m_TreeRootItems.Clear();

            var graph = PlayerLoopSystemGraph.Current;

            if (!string.IsNullOrEmpty(SearchFilter) && SearchFilter.Contains(Constants.SystemSchedule.k_SystemDependencyToken))
            {
                SystemScheduleUtilities.GetSystemDepListFromSystemTypes(GetSystemTypesFromNamesInSearchFilter(graph), m_SystemDependencyList);
                if (null == m_SystemDependencyList || !m_SystemDependencyList.Any())
                {
                    if (this.Contains(m_SystemDetailsVisualElement))
                        Remove(m_SystemDetailsVisualElement);
                }
            }

            foreach (var node in graph.Roots)
            {
                if (!node.ShowForWorld(m_World))
                    continue;

                if (!node.IsRunning && !showInactiveSystems)
                    continue;

                var item = SystemSchedulePool.GetSystemTreeViewItem(graph, node, null, m_World, showInactiveSystems);

                PopulateAllChildren(item);
                m_TreeRootItems.Add(item);
            }

            Refresh();
        }

        IEnumerable<Type> GetSystemTypesFromNamesInSearchFilter(PlayerLoopSystemGraph graph)
        {
            if (string.IsNullOrEmpty(SearchFilter))
                yield break;

            var systemNameList = SearchUtility.GetStringFollowedByGivenToken(SearchFilter, Constants.SystemSchedule.k_SystemDependencyToken).ToList();
            if (!systemNameList.Any())
                yield break;

            using (var pooled = PooledHashSet<Type>.Make())
            {
                foreach (var system in graph.AllSystems)
                {
                    foreach (var singleSystemName in systemNameList)
                    {
                        if (string.Compare(system.GetType().Name, singleSystemName, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            var type = system.GetType();
                            if (pooled.Set.Add(type))
                                yield return type;
                        }
                    }
                }
            }
        }

        void PopulateAllChildren(SystemTreeViewItem item)
        {
            if (item.id == m_LastSelectedItemId)
            {
                m_LastSelectedItem = item;
                m_SystemDetailsVisualElement.LastSelectedItem = m_LastSelectedItem;
            }

            if (!item.HasChildren)
                return;

            item.PopulateChildren(SearchFilter, m_SystemDependencyList);

            foreach (var child in item.children)
            {
                PopulateAllChildren(child as SystemTreeViewItem);
            }
        }

        /// <summary>
        /// Refresh tree view to update with latest information.
        /// </summary>
        void Refresh()
        {
            // This is needed because `ListView.Refresh` will re-create all the elements.
            SystemSchedulePool.ReturnAllToPool(this);
            SystemTreeView.Refresh();

            // System details need to be updated also.
            m_SystemDetailsVisualElement.Target = m_LastSelectedItem;
            m_SystemDetailsVisualElement.SearchFilter = SearchFilter;
        }

        void BindItem(VisualElement element, ITreeViewItem item)
        {
            var target = item as SystemTreeViewItem;
            var systemInformationElement = element as SystemInformationVisualElement;
            if (null == systemInformationElement)
                return;

            systemInformationElement.Target = target;
            systemInformationElement.World = m_World;
            systemInformationElement.Update();
        }
    }
}
