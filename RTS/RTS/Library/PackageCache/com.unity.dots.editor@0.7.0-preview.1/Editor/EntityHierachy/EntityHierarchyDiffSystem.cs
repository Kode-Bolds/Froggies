using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Scenes;
using UnityEngine;

namespace Unity.Entities.Editor
{
    [UsedImplicitly, DisableAutoCreation, UpdateAfter(typeof(SceneSystemGroup)), ExecuteAlways] // ReSharper disable once RequiredBaseTypesIsNotInherited
    class EntityHierarchyDiffSystem : SystemBase
    {
        readonly Dictionary<IEntityHierarchy, Differs> m_DiffersPerContainer = new Dictionary<IEntityHierarchy, Differs>();

        SceneMappingSystem m_SceneMappingSystem;

        public static void Register(IEntityHierarchy hierarchy)
        {
            var system = hierarchy.World.GetOrCreateSystem<EntityHierarchyDiffSystem>();
            system.DoRegister(hierarchy);
            system.Update();

            if (system.m_DiffersPerContainer.Count == 1)
            {
                World.DefaultGameObjectInjectionWorld.GetExistingSystem<InitializationSystemGroup>().AddSystemToUpdateList(system);
                EditorUpdateUtility.EditModeQueuePlayerLoopUpdate();
            }
        }

        public static void Unregister(IEntityHierarchy hierarchy)
        {
            if (!hierarchy.World.IsCreated)
                return; // World was already disposed.

            var system = hierarchy.World.GetExistingSystem<EntityHierarchyDiffSystem>();
            if (system == null)
            {
                Debug.LogWarning("No system found for this strategy for world: " + hierarchy.World.Name);
                return;
            }

            system.DoUnregister(hierarchy);

            if (system.m_DiffersPerContainer.Count == 0)
            {
                World.DefaultGameObjectInjectionWorld.GetExistingSystem<InitializationSystemGroup>().RemoveSystemFromUpdateList(system);
                EditorUpdateUtility.EditModeQueuePlayerLoopUpdate();
            }
        }

        void DoRegister(IEntityHierarchy hierarchy)
        {
            if (m_DiffersPerContainer.ContainsKey(hierarchy))
                return;

            m_DiffersPerContainer.Add(hierarchy, new Differs(hierarchy));
        }

        void DoUnregister(IEntityHierarchy hierarchy)
        {
            if (!m_DiffersPerContainer.ContainsKey(hierarchy))
                return;

            var differs = m_DiffersPerContainer[hierarchy];
            differs.Dispose();
            m_DiffersPerContainer.Remove(hierarchy);
        }

        protected override void OnCreate()
        {
            m_SceneMappingSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<SceneMappingSystem>();
        }

        protected override void OnDestroy()
        {
            while (m_DiffersPerContainer.Count > 0)
                DoUnregister(m_DiffersPerContainer.Keys.First());
        }

        protected override void OnUpdate()
        {
            var version = World.EntityManager.GlobalSystemVersion;

            var handles = new NativeArray<JobHandle>(m_DiffersPerContainer.Count, Allocator.Temp);
            var handleIdx = 0;
            foreach (var differ in m_DiffersPerContainer.Values)
            {
                handles[handleIdx++] = differ.GetDiffSinceLastFrameAsync();
            }

            JobHandle.CompleteAll(handles);
            handles.Dispose();

            var sceneManagerDirty = m_SceneMappingSystem.SceneManagerDirty;
            m_SceneMappingSystem.Update();

            foreach (var kvp in m_DiffersPerContainer)
            {
                kvp.Value.ApplyDiffResultsToStrategy(version, out var strategyStateChanged);
                if (sceneManagerDirty || strategyStateChanged)
                    kvp.Key.OnStructuralChangeDetected();
            }
        }

        class Differs : IDisposable
        {
            readonly IEntityHierarchy m_Hierarchy;
            readonly EntityDiffer m_EntityDiffer;

            readonly List<ComponentDataDiffer> m_ComponentDataDiffers = new List<ComponentDataDiffer>();
            readonly List<SharedComponentDataDiffer> m_SharedComponentDataDiffers = new List<SharedComponentDataDiffer>();

            EntityQueryDesc m_CachedQueryDescription;
            EntityQuery m_MainQuery;

            // Storage for temp differ results
            NativeList<Entity> m_NewEntities;
            NativeList<Entity> m_RemovedEntities;
            readonly ComponentDataDiffer.ComponentChanges[] m_ComponentDataDifferResults;
            readonly SharedComponentDataDiffer.ComponentChanges[] m_SharedComponentDataDifferResults;

            public Differs(IEntityHierarchy hierarchy)
            {
                foreach (var componentType in hierarchy.Strategy.ComponentsToWatch)
                {
                    if (!ComponentDataDiffer.CanWatch(componentType) && !SharedComponentDataDiffer.CanWatch(componentType))
                        throw new NotSupportedException($" The component {componentType} requested by strategy of type {hierarchy.Strategy.GetType()} cannot be watched. No suitable differ available.");
                }

                m_Hierarchy = hierarchy;
                m_EntityDiffer = new EntityDiffer(hierarchy.World);
                foreach (var componentToWatch in hierarchy.Strategy.ComponentsToWatch)
                {
                    var typeInfo = TypeManager.GetTypeInfo(componentToWatch.TypeIndex);

                    switch (typeInfo.Category)
                    {
                        case TypeManager.TypeCategory.ComponentData when UnsafeUtility.IsUnmanaged(componentToWatch.GetManagedType()):
                            m_ComponentDataDiffers.Add((new ComponentDataDiffer(componentToWatch)));
                            break;
                        case TypeManager.TypeCategory.ISharedComponentData:
                            m_SharedComponentDataDiffers.Add((new SharedComponentDataDiffer(componentToWatch)));
                            break;
                    }
                }

                m_ComponentDataDifferResults = new ComponentDataDiffer.ComponentChanges[m_ComponentDataDiffers.Count];
                m_SharedComponentDataDifferResults = new SharedComponentDataDiffer.ComponentChanges[m_SharedComponentDataDiffers.Count];
            }

            public void Dispose()
            {
                m_EntityDiffer.Dispose();
                if (m_MainQuery != default && m_MainQuery != m_Hierarchy.World.EntityManager.UniversalQuery && m_Hierarchy.World.EntityManager.IsQueryValid(m_MainQuery))
                    m_MainQuery.Dispose();

                foreach (var componentDataDiffer in m_ComponentDataDiffers)
                    componentDataDiffer.Dispose();

                foreach (var sharedComponentDataDiffer in m_SharedComponentDataDiffers)
                    sharedComponentDataDiffer.Dispose();
            }

            public JobHandle GetDiffSinceLastFrameAsync()
            {
                UpdateCachedQueries();

                var handles = new NativeArray<JobHandle>(m_ComponentDataDiffers.Count + 1, Allocator.Temp);
                var handleIdx = 0;

                m_NewEntities = new NativeList<Entity>(Allocator.TempJob);
                m_RemovedEntities = new NativeList<Entity>(Allocator.TempJob);
                handles[handleIdx++] = m_EntityDiffer.GetEntityQueryMatchDiffAsync(m_MainQuery, m_NewEntities, m_RemovedEntities);

                for (var i = 0; i < m_ComponentDataDiffers.Count; i++)
                {
                    m_ComponentDataDifferResults[i] = m_ComponentDataDiffers[i].GatherComponentChangesAsync(m_MainQuery, Allocator.TempJob, out var componentDataDifferHandle);
                    handles[handleIdx++] = componentDataDifferHandle;
                }

                for (var i = 0; i < m_SharedComponentDataDiffers.Count; i++)
                {
                    m_SharedComponentDataDifferResults[i] = m_SharedComponentDataDiffers[i].GatherComponentChanges(m_Hierarchy.World.EntityManager, m_MainQuery, Allocator.TempJob);
                }

                var handle = JobHandle.CombineDependencies(handles);
                handles.Dispose();

                return handle;
            }

            void UpdateCachedQueries()
            {
                var entityManager = m_Hierarchy.World.EntityManager;

                if (m_Hierarchy.QueryDesc != null && m_Hierarchy.QueryDesc == m_CachedQueryDescription
                    || m_Hierarchy.QueryDesc == null && m_MainQuery == entityManager.UniversalQuery)
                    return;

                m_CachedQueryDescription = m_Hierarchy.QueryDesc;
                if (m_MainQuery != entityManager.UniversalQuery && entityManager.IsQueryValid(m_MainQuery))
                    m_MainQuery.Dispose();

                m_MainQuery = m_Hierarchy.QueryDesc != null ? entityManager.CreateEntityQuery(m_Hierarchy.QueryDesc) : entityManager.UniversalQuery;
            }

            public void ApplyDiffResultsToStrategy(uint version, out bool strategyStateChanged)
            {
                var strategy = m_Hierarchy.Strategy;
                strategy.BeginApply(version);
                strategy.ApplyEntityChanges(m_NewEntities, m_RemovedEntities, version);

                for (var i = 0; i < m_ComponentDataDifferResults.Length; i++)
                {
                    var componentType = m_ComponentDataDiffers[i].WatchedComponentType;
                    strategy.ApplyComponentDataChanges(componentType, m_ComponentDataDifferResults[i], version);
                    m_ComponentDataDifferResults[i].Dispose();
                }

                for (var i = 0; i < m_SharedComponentDataDifferResults.Length; i++)
                {
                    var componentType = m_SharedComponentDataDiffers[i].WatchedComponentType;
                    strategy.ApplySharedComponentDataChanges(componentType, m_SharedComponentDataDifferResults[i], version);
                    m_SharedComponentDataDifferResults[i].Dispose();
                }

                strategyStateChanged = strategy.EndApply(version);

                m_NewEntities.Dispose();
                m_RemovedEntities.Dispose();
            }
        }
    }


}
