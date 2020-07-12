using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Transforms;
using UnityEditor;

namespace Unity.Entities.Editor
{
    class EntityHierarchyDefaultGroupingStrategy : IEntityHierarchyGroupingStrategy
    {
        const int k_DefaultNodeCapacity = 1024;
        const int k_DefaultChildrenCapacity = 8;
        const int k_InvalidSceneIndex = -1;

        static readonly string k_UnknownSceneName = L10n.Tr("<UnknownScene>");

        readonly World m_World;

        // Note: A performance issue with iterating over NativeHashMaps with medium to large capacity (regardless of the count) forces us to use Dictionaries here.
        // This prevents burstability and jobification, but it's also a 10+x speedup in the Boids sample, when there is no changes to compute.
        // We should go back to NativeHashMap if / when this performance issue is addressed.
        readonly Dictionary<EntityHierarchyNodeId, AddOperation> m_AddedNodes = new Dictionary<EntityHierarchyNodeId, AddOperation>(k_DefaultNodeCapacity);
        readonly Dictionary<EntityHierarchyNodeId, MoveOperation> m_MovedNodes = new Dictionary<EntityHierarchyNodeId, MoveOperation>(k_DefaultNodeCapacity);
        readonly Dictionary<EntityHierarchyNodeId, RemoveOperation> m_RemovedNodes = new Dictionary<EntityHierarchyNodeId, RemoveOperation>(k_DefaultNodeCapacity);

        NativeList<Hash128> m_SceneHashes = new NativeList<Hash128>(k_DefaultNodeCapacity, Allocator.Persistent);

        NativeHashMap<EntityHierarchyNodeId, Entity> m_EntityNodes = new NativeHashMap<EntityHierarchyNodeId, Entity>(k_DefaultNodeCapacity, Allocator.Persistent);
        NativeHashMap<EntityHierarchyNodeId, Hash128> m_SceneNodes = new NativeHashMap<EntityHierarchyNodeId, Hash128>(k_DefaultNodeCapacity, Allocator.Persistent);

        NativeHashMap<EntityHierarchyNodeId, uint> m_Versions = new NativeHashMap<EntityHierarchyNodeId, uint>(k_DefaultNodeCapacity, Allocator.Persistent);
        NativeHashMap<EntityHierarchyNodeId, EntityHierarchyNodeId> m_Parents = new NativeHashMap<EntityHierarchyNodeId, EntityHierarchyNodeId>(k_DefaultNodeCapacity, Allocator.Persistent);
        NativeHashMap<EntityHierarchyNodeId, UnsafeList<EntityHierarchyNodeId>> m_Children = new NativeHashMap<EntityHierarchyNodeId, UnsafeList<EntityHierarchyNodeId>>(k_DefaultNodeCapacity, Allocator.Persistent);
        EntityQuery m_RootEntitiesQuery;
        EntityQueryMask m_RootEntitiesQueryMask;

        public EntityHierarchyDefaultGroupingStrategy(World world)
        {
            m_World = world;
            m_Versions.Add(EntityHierarchyNodeId.Root, 0);
            m_Children.Add(EntityHierarchyNodeId.Root, new UnsafeList<EntityHierarchyNodeId>(k_DefaultChildrenCapacity, Allocator.Persistent));

            m_RootEntitiesQuery = m_World.EntityManager.CreateEntityQuery(new EntityQueryDesc { None = new ComponentType[] { typeof(Parent) } });
            m_RootEntitiesQueryMask = m_World.EntityManager.GetEntityQueryMask(m_RootEntitiesQuery);
        }

        public void Dispose()
        {
            m_RootEntitiesQuery.Dispose();
            m_SceneHashes.Dispose();
            m_EntityNodes.Dispose();
            m_SceneNodes.Dispose();

            m_Versions.Dispose();
            m_Parents.Dispose();
            new FreeChildrenListsJob { ChildrenLists = m_Children.GetValueArray(Allocator.TempJob) }.Run();
            m_Children.Dispose();
        }

        [BurstCompile]
        struct FreeChildrenListsJob : IJob
        {
            [ReadOnly, DeallocateOnJobCompletion] public NativeArray<UnsafeList<EntityHierarchyNodeId>> ChildrenLists;

            public void Execute()
            {
                for (var i = 0; i < ChildrenLists.Length; i++)
                {
                    ChildrenLists[i].Dispose();
                }
            }
        }

        public ComponentType[] ComponentsToWatch { get; } = { typeof(Parent), typeof(SceneTag) };

        void IEntityHierarchyGroupingStrategy.BeginApply(uint version)
        {
            m_AddedNodes.Clear();
            m_MovedNodes.Clear();
            m_RemovedNodes.Clear();
        }

        void IEntityHierarchyGroupingStrategy.ApplyEntityChanges(NativeArray<Entity> newEntities, NativeArray<Entity> removedEntities, uint version)
        {
            // Remove entities
            foreach (var entity in removedEntities)
                RegisterRemoveOperation(entity);

            // Add new entities
            foreach (var entity in newEntities)
                RegisterAddOperation(entity);
        }

        void IEntityHierarchyGroupingStrategy.ApplyComponentDataChanges(ComponentType componentType, in ComponentDataDiffer.ComponentChanges componentChanges, uint version)
        {
            if (componentType == typeof(Parent))
                ApplyParentComponentChanges(componentChanges);
        }

        void IEntityHierarchyGroupingStrategy.ApplySharedComponentDataChanges(ComponentType componentType, in SharedComponentDataDiffer.ComponentChanges componentChanges, uint version)
        {
            if (componentType == typeof(SceneTag))
                ApplySceneTagChanges(componentChanges, version);
        }

        bool IEntityHierarchyGroupingStrategy.EndApply(uint version)
        {
            // NOTE - Order matters: 1.Added 2.Moved 3.Removed 4.Scene Mapping

            var hasAdditions = m_AddedNodes.Count > 0;
            var hasMoves = m_MovedNodes.Count > 0;
            var hasRemovals = m_RemovedNodes.Count > 0;

            foreach (var kvp in m_AddedNodes)
            {
                var node = kvp.Key;
                var operation = kvp.Value;
                AddNode(operation.Parent, node, version);
                m_EntityNodes[node] = operation.Entity;
            }

            foreach (var kvp in m_MovedNodes)
            {
                var node = kvp.Key;
                var operation = kvp.Value;
                MoveNode(operation.FromNode, operation.ToNode, node, version);
            }

            foreach (var node in m_RemovedNodes.Keys)
                RemoveNode(node, version);

            if (hasRemovals)
            {
                // If there were any removals, we *may* have some scenes to prune
                var sceneMapper = World.DefaultGameObjectInjectionWorld?.GetExistingSystem<SceneMappingSystem>();
                if (sceneMapper == null)
                    return true;

                using (var knownScenes = PooledHashSet<Hash128>.Make())
                using (var kva = m_SceneNodes.GetKeyValueArrays(Allocator.Temp))
                {
                    sceneMapper.GetAllKnownScenes(knownScenes);

                    for (var i = kva.Keys.Length - 1; i >= 0; --i)
                    {
                        var sceneHash = kva.Values[i];
                        if (!knownScenes.Set.Contains(sceneHash))
                        {
                            var sceneNode = kva.Keys[i];
                            RemoveNode(sceneNode, version);
                            m_SceneNodes.Remove(sceneNode);
                        }
                    }
                }
            }

            return hasAdditions || hasMoves || hasRemovals;
        }

        bool IEntityHierarchyGroupingStrategy.HasChildren(in EntityHierarchyNodeId nodeId)
            => m_Children.TryGetValue(nodeId, out var l) && l.Length > 0;

        public unsafe NativeArray<EntityHierarchyNodeId> GetChildren(in EntityHierarchyNodeId nodeId, Allocator allocator)
        {
            var children = m_Children[nodeId];
            var result = new NativeArray<EntityHierarchyNodeId>(children.Length, allocator);
            UnsafeUtility.MemCpy(result.GetUnsafePtr(), children.Ptr, children.Length * sizeof(EntityHierarchyNodeId));
            return result;
        }

        public bool Exists(in EntityHierarchyNodeId nodeId)
            => m_Versions.ContainsKey(nodeId);

        public Entity GetUnderlyingEntity(in EntityHierarchyNodeId nodeId)
        {
            if (nodeId.Kind != NodeKind.Entity)
                throw new NotSupportedException();

            return m_EntityNodes.TryGetValue(nodeId, out var entity) ? entity : Entity.Null;
        }

        public uint GetNodeVersion(in EntityHierarchyNodeId nodeId)
            => m_Versions[nodeId];

        public string GetNodeName(in EntityHierarchyNodeId nodeId)
        {
            switch (nodeId.Kind)
            {
                case NodeKind.Scene:
                {
                    var sceneHash = GetSceneHash(nodeId);
                    var loadedSceneRef = AssetDatabase.LoadAssetAtPath<SceneAsset>(AssetDatabase.GUIDToAssetPath(sceneHash.ToString()));
                    return loadedSceneRef == null ? k_UnknownSceneName : loadedSceneRef.name;
                }
                case NodeKind.Entity:
                {
                    var entity = m_EntityNodes[nodeId];
                    var name = m_World.EntityManager.GetName(entity);
                    return string.IsNullOrEmpty(name) ? entity.ToString() : name;
                }
                default:
                {
                    throw new NotSupportedException();
                }
            }
        }

        void ApplyParentComponentChanges(ComponentDataDiffer.ComponentChanges componentChanges)
        {
            // parent removed
            if (componentChanges.RemovedComponentsCount > 0)
            {
                var(entities, parents) = componentChanges.GetRemovedComponents<Parent>(Allocator.TempJob);
                for (var i = 0; i < componentChanges.RemovedComponentsCount; i++)
                {
                    var entity = entities[i];
                    var entityNodeId = CreateEntityNodeId(entity);

                    var previousParentComponent = parents[i];
                    var previousParentEntityNodeId = CreateEntityNodeId(previousParentComponent.Value);

                    RegisterMoveOperation(previousParentEntityNodeId, EntityHierarchyNodeId.Root, entityNodeId);
                }

                entities.Dispose();
                parents.Dispose();
            }

            // parent added
            if (componentChanges.AddedComponentsCount > 0)
            {
                var(entities, parents) = componentChanges.GetAddedComponents<Parent>(Allocator.TempJob);
                for (var i = 0; i < componentChanges.AddedComponentsCount; i++)
                {
                    var entity = entities[i];
                    var entityNodeId = CreateEntityNodeId(entity);
                    var newParentComponent = parents[i];
                    var newParentEntityNodeId = CreateEntityNodeId(newParentComponent.Value);

                    RegisterMoveOperation(newParentEntityNodeId, entityNodeId);
                }

                entities.Dispose();
                parents.Dispose();
            }
        }

        void ApplySceneTagChanges(SharedComponentDataDiffer.ComponentChanges componentChanges, uint version)
        {
            var sceneMapper = World.DefaultGameObjectInjectionWorld.GetExistingSystem<SceneMappingSystem>();

            for (var i = 0; i < componentChanges.RemovedEntitiesCount; ++i)
            {
                var entity = componentChanges.GetRemovedEntity(i);
                if (!m_RootEntitiesQueryMask.Matches(entity))
                    continue;

                var tag = componentChanges.GetRemovedComponent<SceneTag>(i);

                var entityNodeId = CreateEntityNodeId(entity);

                var subsceneHash = sceneMapper.GetSubsceneHash(tag.SceneEntity);
                if (subsceneHash == default)
                    continue; // Previous parent was not a scene or was a scene that does not exist anymore; skip

                var previousParentEntityNodeId = CreateSceneNodeId(GetOrCreateSceneIndex(subsceneHash));

                // If this is not the first move, this entity didn't have a parent before and now it does, skip!
                RegisterFirstMoveOperation(previousParentEntityNodeId, EntityHierarchyNodeId.Root, entityNodeId);
            }

            for (var i = 0; i < componentChanges.AddedEntitiesCount; ++i)
            {
                var entity = componentChanges.GetAddedEntity(i);
                if (!m_RootEntitiesQueryMask.Matches(entity))
                    continue;

                var tag = componentChanges.GetAddedComponent<SceneTag>(i);

                var subsceneHash = sceneMapper.GetSubsceneHash(tag.SceneEntity);

                var entityNodeId = CreateEntityNodeId(entity);
                var newParentNodeId = subsceneHash == default ? EntityHierarchyNodeId.Root : GetOrCreateSubsceneNode(subsceneHash, version);

                RegisterMoveOperation(newParentNodeId, entityNodeId);
            }
        }

        static EntityHierarchyNodeId CreateEntityNodeId(Entity e)
            => new EntityHierarchyNodeId(NodeKind.Entity, e.Index);

        static EntityHierarchyNodeId CreateSceneNodeId(int sceneIndex)
            => new EntityHierarchyNodeId(NodeKind.Scene, sceneIndex);

        int GetSceneIndex(Hash128 sceneHash) => m_SceneHashes.IndexOf(sceneHash);

        int GetOrCreateSceneIndex(Hash128 sceneHash)
        {
            var index = m_SceneHashes.IndexOf(sceneHash);
            if (index > -1)
                return index;

            m_SceneHashes.Add(sceneHash);
            return m_SceneHashes.Length - 1;
        }

        Hash128 GetSceneHash(in EntityHierarchyNodeId nodeId)
            => m_SceneHashes[nodeId.Id];

        EntityHierarchyNodeId GetOrCreateSubsceneNode(Hash128 subsceneHash, uint version)
        {
            var subsceneNodeId = new EntityHierarchyNodeId(NodeKind.Scene, GetSceneIndex(subsceneHash));
            if (!Exists(subsceneNodeId))
            {
                var sceneMapper = World.DefaultGameObjectInjectionWorld.GetExistingSystem<SceneMappingSystem>();
                var parentSceneHash = sceneMapper.GetParentSceneHash(subsceneHash);
                var parentSceneNodeId = new EntityHierarchyNodeId(NodeKind.Scene, GetOrCreateSceneIndex(parentSceneHash));

                if (!Exists(parentSceneNodeId))
                {
                    m_SceneNodes[parentSceneNodeId] = parentSceneHash;
                    AddNode(EntityHierarchyNodeId.Root, parentSceneNodeId, version);
                }

                // If scene index was not found, create it
                if (subsceneNodeId.Id == k_InvalidSceneIndex)
                    subsceneNodeId = new EntityHierarchyNodeId(NodeKind.Scene, GetOrCreateSceneIndex(subsceneHash));

                m_SceneNodes[subsceneNodeId] = subsceneHash;
                AddNode(parentSceneNodeId, subsceneNodeId, version);
            }

            return subsceneNodeId;
        }

        void RegisterAddOperation(Entity entity)
        {
            var node = CreateEntityNodeId(entity);
            if (m_RemovedNodes.ContainsKey(node))
                m_RemovedNodes.Remove(node);
            else
                m_AddedNodes[node] = new AddOperation {Entity = entity, Parent = EntityHierarchyNodeId.Root};
        }

        void RegisterRemoveOperation(Entity entity)
        {
            var node = CreateEntityNodeId(entity);
            if (m_AddedNodes.ContainsKey(node))
                m_AddedNodes.Remove(node);
            else
                m_RemovedNodes[node] = new RemoveOperation();
        }

        void RegisterMoveOperation(EntityHierarchyNodeId toNode, EntityHierarchyNodeId node)
        {
            var previousParentNodeId = m_Parents.ContainsKey(node) ? m_Parents[node] : default;
            RegisterMoveOperation(previousParentNodeId, toNode, node);
        }

        void RegisterMoveOperation(EntityHierarchyNodeId fromNode, EntityHierarchyNodeId toNode, EntityHierarchyNodeId node)
        {
            if (m_RemovedNodes.ContainsKey(node))
                return;

            if (m_AddedNodes.ContainsKey(node))
            {
                var addOperation = m_AddedNodes[node];
                addOperation.Parent = toNode;
                m_AddedNodes[node] = addOperation;
            }
            else if (m_MovedNodes.ContainsKey(node))
            {
                var moveOperation = m_MovedNodes[node];
                moveOperation.ToNode = toNode;
                m_MovedNodes[node] = moveOperation;
            }
            else
            {
                m_MovedNodes[node] = new MoveOperation {FromNode = fromNode, ToNode = toNode};
            }
        }

        // Only register a move operation if this is the first move detected
        void RegisterFirstMoveOperation(EntityHierarchyNodeId fromNode, EntityHierarchyNodeId toNode, EntityHierarchyNodeId node)
        {
            if (m_MovedNodes.ContainsKey(node))
                return;

            RegisterMoveOperation(fromNode, toNode, node);
        }

        void AddNode(in EntityHierarchyNodeId parentNode, in EntityHierarchyNodeId newNode, uint version)
        {
            if (parentNode.Equals(default))
                throw new ArgumentException("Trying to add a new node to an invalid parent node.");

            if (newNode.Equals(default))
                throw new ArgumentException("Trying to add an invalid node to the tree.");

            m_Versions[newNode] = version;
            m_Versions[parentNode] = version;
            m_Parents[newNode] = parentNode;

            AddChild(m_Children, parentNode, newNode);
        }

        void RemoveNode(in EntityHierarchyNodeId node, uint version)
        {
            if (node.Equals(default))
                throw new ArgumentException("Trying to remove an invalid node from the tree.");

            if (!m_Versions.Remove(node))
                return;

            if (!m_Parents.TryGetValue(node, out var parentNodeId))
                return;

            m_Parents.Remove(node);
            m_Versions[parentNodeId] = version;
            RemoveChild(m_Children, parentNodeId, node);

            // TODO: Validate that code...
            // Find eventual children and attach them to the parent
            if (m_Children.TryGetValue(node, out var children))
            {
                var siblings = m_Children[parentNodeId];
                siblings.AddRange(children);
                m_Children[parentNodeId] = siblings;
                children.Dispose();
                m_Children.Remove(node);
            }
        }

        void MoveNode(in EntityHierarchyNodeId previousParent, in EntityHierarchyNodeId newParent, in EntityHierarchyNodeId node, uint version)
        {
            if (previousParent.Equals(default))
                throw new ArgumentException("Trying to unparent from an invalid node.");

            if (newParent.Equals(default))
                throw new ArgumentException("Trying to parent to an invalid node.");

            if (node.Equals(default))
                throw new ArgumentException("Trying to add an invalid node to the tree.");

            if (previousParent.Equals(newParent))
                return; // NOOP

            RemoveChild(m_Children, previousParent, node);
            m_Versions[previousParent] = version;

            m_Parents[node] = newParent;
            AddChild(m_Children, newParent, node);
            m_Versions[newParent] = version;
        }

        static void AddChild(NativeHashMap<EntityHierarchyNodeId, UnsafeList<EntityHierarchyNodeId>> children, in EntityHierarchyNodeId parentId, in EntityHierarchyNodeId newChild)
        {
            if (!children.TryGetValue(parentId, out var siblings))
                siblings = new UnsafeList<EntityHierarchyNodeId>(k_DefaultChildrenCapacity, Allocator.Persistent);

            siblings.Add(newChild);
            children[parentId] = siblings;
        }

        static unsafe void RemoveChild(NativeHashMap<EntityHierarchyNodeId, UnsafeList<EntityHierarchyNodeId>> children, in EntityHierarchyNodeId parentId, in EntityHierarchyNodeId childToRemove)
        {
            if (!children.TryGetValue(parentId, out var siblings))
                return;

            fixed(EntityHierarchyNodeId* childToRemovePtr = &childToRemove)
            {
                for (var childId = 0; childId < siblings.Length; childId++)
                {
                    if (UnsafeUtility.MemCmp(siblings.Ptr + childId, childToRemovePtr, sizeof(EntityHierarchyNodeId)) != 0)
                        continue;

                    siblings.RemoveAtSwapBack(childId);
                }
            }

            children[parentId] = siblings;
        }

        struct AddOperation
        {
            public EntityHierarchyNodeId Parent;
            public Entity Entity;
        }

        struct MoveOperation
        {
            public EntityHierarchyNodeId FromNode;
            public EntityHierarchyNodeId ToNode;
        }

        struct RemoveOperation
        {
        }
    }
}
