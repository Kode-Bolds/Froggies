using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Editor.Bridge;
using UnityEngine;

namespace Unity.Entities.Editor
{
    class SystemTreeViewItem : ITreeViewItem, IPoolable
    {
        readonly List<ITreeViewItem> m_CachedChildren = new List<ITreeViewItem>();
        public IPlayerLoopNode Node;
        public PlayerLoopSystemGraph Graph;
        public World World;
        public bool ShowInactiveSystems;

        public ComponentSystemBase System => (Node as IComponentSystemNode)?.System;

        public bool HasChildren => Node.Children.Count > 0;

        public string GetSystemName(World world = null)
        {
            return null == world ? Node.WorldName : Node.Name;
        }

        public bool GetParentState()
        {
            return Node.EnabledInHierarchy;
        }

        public void SetPlayerLoopSystemState(bool state)
        {
            Node.Enabled = state;
        }

        public void SetSystemState(bool state)
        {
            Node.Enabled = state;
        }

        public string GetEntityMatches()
        {
            if (HasChildren) // Group system do not need entity matches.
                return string.Empty;

            if (null == System?.EntityQueries)
                return string.Empty;

            var matchedEntityCount = System.EntityQueries.Sum(query => query.CalculateEntityCount());

            return matchedEntityCount.ToString();
        }

        float GetAverageRunningTime(ComponentSystemBase system)
        {
            switch (system)
            {
                case ComponentSystemGroup systemGroup:
                {
                    if (systemGroup.Systems != null)
                        return systemGroup.Systems.Sum(GetAverageRunningTime);
                }
                break;
                case ComponentSystemBase systemBase:
                {
                    return Graph.RecordersBySystem.ContainsKey(systemBase)
                        ? Graph.RecordersBySystem[systemBase].ReadMilliseconds()
                        : 0.0f;
                }
            }

            return -1;
        }

        public string GetRunningTime()
        {
            var totalTime = 0.0f;

            if (Node is IPlayerLoopSystemData)
                return string.Empty;

            if (children.Count() != 0)
            {
                totalTime = !Node.Enabled || !NodeParentsAllEnabled(Node)
                    ? 0.0f
                    : Node.Children.OfType<IComponentSystemNode>().Sum(child => GetAverageRunningTime(child.System));
            }
            else
            {
                if (Node.IsRunning && Node is IComponentSystemNode data)
                {
                    totalTime = !Node.Enabled || !NodeParentsAllEnabled(Node)
                        ? 0.0f
                        : GetAverageRunningTime(data.System);
                }
                else
                {
                    return "-";
                }
            }

            return totalTime.ToString("f2");
        }

        bool NodeParentsAllEnabled(IPlayerLoopNode node)
        {
            if (node.Parent != null)
            {
                if (!node.Parent.Enabled) return false;
                if (!NodeParentsAllEnabled(node.Parent)) return false;
            }

            return true;
        }

        public int id => Node.Hash;
        public ITreeViewItem parent { get; internal set; }

        public IEnumerable<ITreeViewItem> children => m_CachedChildren;

        bool ITreeViewItem.hasChildren => HasChildren;

        public void AddChild(ITreeViewItem child)
        {
            throw new NotImplementedException();
        }

        public void AddChildren(IList<ITreeViewItem> children)
        {
            throw new NotImplementedException();
        }

        public void RemoveChild(ITreeViewItem child)
        {
            throw new NotImplementedException();
        }

        public void PopulateChildren(string searchFilter = null, List<Type> systemDependencyList = null)
        {
            m_CachedChildren.Clear();

            foreach (var child in Node.Children)
            {
                if (!child.ShowForWorld(World))
                    continue;

                if (!child.IsRunning && !ShowInactiveSystems)
                    continue;

                // Filter systems by system name, component types, system dependencies.
                if (!string.IsNullOrEmpty(searchFilter) && !FilterSystem(child, searchFilter, systemDependencyList))
                    continue;

                var item = SystemSchedulePool.GetSystemTreeViewItem(Graph, child, this, World, ShowInactiveSystems);
                m_CachedChildren.Add(item);
            }
        }

        static bool FilterSystem(IPlayerLoopNode node, string searchFilter, List<Type> systemDependencyList)
        {
            switch (node)
            {
                case ComponentSystemBaseNode baseNode:
                {
                   return FilterBaseSystem(baseNode.System, searchFilter, systemDependencyList);
                }

                case ComponentGroupNode groupNode:
                {
                    // Deal with group node dependencies first.
                    if (FilterBaseSystem(groupNode.System, searchFilter, systemDependencyList))
                        return true;

                    // Then their children.
                    if (groupNode.Children.Any(child => FilterSystem(child, searchFilter, systemDependencyList)))
                        return true;

                    break;
                }
            }

            return false;
        }

        static bool FilterBaseSystem(ComponentSystemBase system, string searchFilter, List<Type> systemDependencyList)
        {
            if (null == system)
                return false;

            var systemName = system.GetType().Name;

            foreach (var singleString in SearchUtility.SplitSearchStringBySpace(searchFilter))
            {
                if (singleString.StartsWith(Constants.SystemSchedule.k_ComponentToken, StringComparison.OrdinalIgnoreCase))
                {
                    if (!EntityQueryUtility.ContainsThisComponentType(system, singleString.Substring(Constants.SystemSchedule.k_ComponentTokenLength)))
                        return false;
                }
                else if (singleString.StartsWith(Constants.SystemSchedule.k_SystemDependencyToken, StringComparison.OrdinalIgnoreCase))
                {
                    if (null != systemDependencyList && !systemDependencyList.Contains(system.GetType()))
                        return false;
                }
                else
                {
                    if (systemName.IndexOf(singleString, StringComparison.OrdinalIgnoreCase) < 0)
                        return false;
                }
            }

            return true;
        }

        public void Reset()
        {
            World = null;
            Graph = null;
            Node = null;
            parent = null;
            ShowInactiveSystems = false;
            m_CachedChildren.Clear();
        }

        public void ReturnToPool()
        {
            foreach (var child in m_CachedChildren.OfType<SystemTreeViewItem>())
            {
                child.ReturnToPool();
            }

            SystemSchedulePool.ReturnToPool(this);
        }
    }
}
