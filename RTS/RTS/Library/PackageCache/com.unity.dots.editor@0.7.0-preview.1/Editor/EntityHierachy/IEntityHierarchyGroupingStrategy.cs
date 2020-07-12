using JetBrains.Annotations;
using System;
using Unity.Collections;

namespace Unity.Entities.Editor
{
    interface IEntityHierarchyGroupingStrategy : IDisposable
    {
        ComponentType[] ComponentsToWatch { get; }

        void BeginApply(uint version);
        void ApplyEntityChanges(NativeArray<Entity> newEntities, NativeArray<Entity> removedEntities, uint version);
        void ApplyComponentDataChanges(ComponentType componentType, in ComponentDataDiffer.ComponentChanges componentChanges, uint version);
        void ApplySharedComponentDataChanges(ComponentType componentType, in SharedComponentDataDiffer.ComponentChanges componentChanges, uint version);
        bool EndApply(uint version);

        bool HasChildren(in EntityHierarchyNodeId nodeId);

        NativeArray<EntityHierarchyNodeId> GetChildren(in EntityHierarchyNodeId nodeId, Allocator allocator);

        bool Exists(in EntityHierarchyNodeId nodeId);

        Entity GetUnderlyingEntity(in EntityHierarchyNodeId nodeId);

        uint GetNodeVersion(in EntityHierarchyNodeId nodeId);

        string GetNodeName(in EntityHierarchyNodeId nodeId);
    }

    struct VirtualTreeNode
    {
        [UsedImplicitly]
        public Hash128 SceneId;
    }
}
