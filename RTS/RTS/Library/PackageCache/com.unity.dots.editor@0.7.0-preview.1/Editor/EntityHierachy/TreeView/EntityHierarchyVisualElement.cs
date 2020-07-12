using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Entities.Editor
{
    class EntityHierarchyVisualElement : VisualElement, IPoolable
    {
        public EntityHierarchyTreeView Owner { get; set; }

        readonly VisualElement m_Icon;
        readonly Label m_NameLabel;
        readonly VisualElement m_SystemButton;
        readonly VisualElement m_PingGameObject;

        IEntityHierarchy m_Hierarchy;
        EntityHierarchyNodeId m_NodeId;
        int? m_OriginatingId;

        public EntityHierarchyVisualElement()
        {
            Resources.Templates.EntityHierarchyItem.Clone(this);
            AddToClassList(UssClasses.DotsEditorCommon.CommonResources);
            AddToClassList(UssClasses.Resources.EntityHierarchy);

            m_Icon = this.Q<VisualElement>(className: UssClasses.EntityHierarchyWindow.Item.Icon);
            m_NameLabel = this.Q<Label>(className: UssClasses.EntityHierarchyWindow.Item.NameLabel);
            m_SystemButton = this.Q<VisualElement>(className: UssClasses.EntityHierarchyWindow.Item.SystemButton);
            m_PingGameObject = this.Q<VisualElement>(className: UssClasses.EntityHierarchyWindow.Item.PingGameObjectButton);
        }

        public void SetSource(IEntityHierarchy entityHierarchy, in EntityHierarchyNodeId nodeId)
        {
            m_Hierarchy = entityHierarchy;
            m_NodeId = nodeId;
            switch (nodeId.Kind)
            {
                case NodeKind.Entity:
                {
                    RenderEntityNode();
                    break;
                }
                case NodeKind.Scene:
                {
                    RenderSceneNode();
                    break;
                }
                case NodeKind.Root:
                case NodeKind.None:
                {
                    RenderInvalidNode(nodeId);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void IPoolable.Reset()
        {
            Owner = null;
            m_Hierarchy = null;
            m_NodeId = default;
            m_OriginatingId = null;
            ClearDynamicClasses();
        }

        void IPoolable.ReturnToPool() => EntityHierarchyPool.ReturnVisualElement(this);

        void RenderEntityNode()
        {
            m_NameLabel.text = m_Hierarchy.Strategy.GetNodeName(m_NodeId);

            m_Icon.AddToClassList(UssClasses.EntityHierarchyWindow.Item.IconEntity);
            m_SystemButton.AddToClassList(UssClasses.EntityHierarchyWindow.Item.VisibleOnHover);

            if (TryGetSourceGameObjectId(m_Hierarchy.Strategy.GetUnderlyingEntity(m_NodeId), m_Hierarchy.World, out var originatingId))
            {
                m_OriginatingId = originatingId;
                m_PingGameObject.AddToClassList(UssClasses.EntityHierarchyWindow.Item.VisibleOnHover);
                m_PingGameObject.RegisterCallback<MouseUpEvent>(OnPingGameObjectRequested);
            }
        }

        void OnPingGameObjectRequested(MouseUpEvent _)
        {
            if (!m_OriginatingId.HasValue)
                return;

            EditorGUIUtility.PingObject(m_OriginatingId.Value);
        }

        void RenderSceneNode()
        {
            m_Icon.AddToClassList(UssClasses.EntityHierarchyWindow.Item.IconScene);
            m_NameLabel.AddToClassList(UssClasses.EntityHierarchyWindow.Item.NameScene);
            m_NameLabel.text = m_Hierarchy.Strategy.GetNodeName(m_NodeId);
        }

        void RenderInvalidNode(EntityHierarchyNodeId nodeId)
        {
            m_NameLabel.text = $"<UNKNOWN> ({nodeId.ToString()})";
        }

        void ClearDynamicClasses()
        {
            m_NameLabel.RemoveFromClassList(UssClasses.EntityHierarchyWindow.Item.NameScene);

            m_Icon.RemoveFromClassList(UssClasses.EntityHierarchyWindow.Item.IconScene);
            m_Icon.RemoveFromClassList(UssClasses.EntityHierarchyWindow.Item.IconEntity);

            m_SystemButton.RemoveFromClassList(UssClasses.EntityHierarchyWindow.Item.VisibleOnHover);
            m_PingGameObject.RemoveFromClassList(UssClasses.EntityHierarchyWindow.Item.VisibleOnHover);

            m_PingGameObject.UnregisterCallback<MouseUpEvent>(OnPingGameObjectRequested);
        }

        static bool TryGetSourceGameObjectId(Entity entity, World world, out int? originatingId)
        {
            if (!world.EntityManager.Exists(entity) || !world.EntityManager.HasComponent<EntityGuid>(entity))
            {
                originatingId = null;
                return false;
            }

            originatingId = world.EntityManager.GetComponentData<EntityGuid>(entity).OriginatingId;
            return true;
        }
    }
}
