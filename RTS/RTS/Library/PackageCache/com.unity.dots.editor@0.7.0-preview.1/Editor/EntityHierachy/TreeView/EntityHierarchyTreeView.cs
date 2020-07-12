using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Editor.Bridge;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityObject = UnityEngine.Object;

namespace Unity.Entities.Editor
{
    class EntityHierarchyTreeView : VisualElement, IDisposable
    {
        const int k_ItemHeight = 20;

        readonly List<ITreeViewItem> m_RootItems = new List<ITreeViewItem>();

        TreeView m_TreeView;
        EntitySelectionProxy m_SelectionProxy;
        IEntityHierarchy m_Hierarchy;
        uint m_RootVersion;
        bool m_StructureChanged;

        public EntityHierarchyTreeView()
        {
            style.flexGrow = 1.0f;

            CreateTreeView();
            CreateEntitySelectionProxy();
        }

        public void Dispose()
        {
            if (m_SelectionProxy != null)
                UnityObject.DestroyImmediate(m_SelectionProxy);
        }

        public void Refresh(IEntityHierarchy hierarchy)
        {
            if (m_Hierarchy == hierarchy)
                return;

            m_Hierarchy = hierarchy;
            m_StructureChanged = true;
            m_RootVersion = 0;
            OnUpdate();
        }

        public void OnUpdate()
        {
            if (m_Hierarchy?.Strategy == null)
                return;

            var rootVersion = m_Hierarchy.Strategy.GetNodeVersion(EntityHierarchyNodeId.Root);
            if (!m_StructureChanged && rootVersion == m_RootVersion)
                return;

            m_StructureChanged = false;
            m_RootVersion = rootVersion;

            RecreateRootItems();
            EntityHierarchyPool.ReturnAllVisualElements(this);
            m_TreeView.Refresh();
        }

        public void UpdateStructure()
        {
            // Topology changes will be applied during update
            m_StructureChanged = true;
        }

        void RecreateRootItems()
        {
            foreach (var child in m_RootItems)
                ((IPoolable)child).ReturnToPool();

            m_RootItems.Clear();

            if (m_Hierarchy?.Strategy == null)
                return;

            using (var rootNodes = m_Hierarchy.Strategy.GetChildren(EntityHierarchyNodeId.Root, Allocator.TempJob))
            {
                foreach (var node in rootNodes)
                    m_RootItems.Add(EntityHierarchyPool.GetTreeViewItem(null, node, m_Hierarchy.Strategy));
            }
        }

        void CreateTreeView()
        {
            m_TreeView = new TreeView(m_RootItems, k_ItemHeight, OnMakeItem, OnBindItem)
            {
                Filter = OnFilter,
            };
            m_TreeView.onSelectionChange += OnSelectionChange;
            m_TreeView.style.flexGrow = 1.0f;

            Add(m_TreeView);
        }

        void CreateEntitySelectionProxy()
        {
            m_SelectionProxy = ScriptableObject.CreateInstance<EntitySelectionProxy>();
            m_SelectionProxy.hideFlags = HideFlags.HideAndDontSave;
            m_SelectionProxy.EntityControlSelectButton += OnSelectionChangedByInspector;
        }

        void OnSelectionChange(IEnumerable<ITreeViewItem> selection)
        {
            var selectedItem = (EntityHierarchyTreeViewItem)selection.FirstOrDefault();
            if (selectedItem == null)
                return;

            // TODO: Support undo/redo (see: Hierarchy window)

            if (selectedItem.NodeId.Kind == NodeKind.Entity)
            {
                var entity = selectedItem.Strategy.GetUnderlyingEntity(selectedItem.NodeId);
                if (selectedItem.Strategy.GetUnderlyingEntity(selectedItem.NodeId) != Entity.Null)
                {
                    m_SelectionProxy.SetEntity(m_Hierarchy.World, entity);
                    Selection.activeObject = m_SelectionProxy;
                }
            }
            else
            {
                // TODO: Deal with non-Entity selections
                Selection.activeObject = null;
            }
        }

        void OnSelectionChangedByInspector(World world, Entity entity)
        {
            if (world != m_Hierarchy.World)
                return;

            m_TreeView.Select(new EntityHierarchyNodeId(NodeKind.Entity, entity.Index).GetHashCode());
        }

        VisualElement OnMakeItem() => EntityHierarchyPool.GetVisualElement(this);

        void OnBindItem(VisualElement element, ITreeViewItem item) => ((EntityHierarchyVisualElement)element).SetSource(m_Hierarchy, ((EntityHierarchyTreeViewItem)item).NodeId);

        bool OnFilter(ITreeViewItem item) => true;
    }
}
