using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Jobs;
using Unity.Scenes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Entities.Editor
{
    [DisableAutoCreation][UsedImplicitly]
    class SceneMappingSystem : SystemBase
    {
        readonly Dictionary<Hash128, Hash128> m_SubsceneOwnershipMap = new Dictionary<Hash128, Hash128>(8);
        NativeHashMap<Entity, Entity> m_SceneSectionToSubsceneMap;

        Hash128 m_ScenesCountFingerprint;
        public bool SceneManagerDirty { get; private set; }

        public Hash128 GetParentSceneHash(Hash128 subsceneGUID) => m_SubsceneOwnershipMap.TryGetValue(subsceneGUID, out var result) ? result : default;

        public void GetAllKnownScenes(HashSet<Hash128> result)
        {
            foreach (var kvp in m_SubsceneOwnershipMap)
            {
                result.Add(kvp.Key);
                result.Add(kvp.Value);
            }
        }

        public Hash128 GetSubsceneHash(Entity entity)
        {
            if (entity == Entity.Null)
                return default;

            var subsceneEntity = Entity.Null;

            if (m_SceneSectionToSubsceneMap.TryGetValue(entity, out var entityToSubscene))
            {
                // Entity is a scene section
                subsceneEntity = entityToSubscene;
            }
            else if (EntityManager.HasComponent<SceneTag>(entity))
            {
                // Entity is in a subscene
                var sceneEntity = EntityManager.GetSharedComponentData<SceneTag>(entity).SceneEntity;

                // Currently, it seems like SceneTag.SceneEntity does not point to an actual SceneEntity, but to a SceneSection. If so, use this reverse lookup.
                if (m_SceneSectionToSubsceneMap.TryGetValue(sceneEntity, out var sceneEntityToSubscene))
                    subsceneEntity = sceneEntityToSubscene;
                else
                    // Subscene may not be loaded
                    Debug.LogWarning($"Entity {EntityManager.GetName(entity)} has a {nameof(SceneTag)} component, but its subscene could not be found.");
            }

            if (subsceneEntity != Entity.Null)
            {
                if (EntityManager.HasComponent<SubScene>(subsceneEntity))
                    return EntityManager.GetComponentObject<SubScene>(subsceneEntity).SceneGUID;

                if (EntityManager.HasComponent<SceneReference>(subsceneEntity))
                    return EntityManager.GetComponentData<SceneReference>(subsceneEntity).SceneGUID;
            }

            return default;
        }

        protected override void OnCreate()
        {
            m_ScenesCountFingerprint = default;
            SceneManagerDirty = true; // Ensures cache rebuild on first tick
            SceneManager.sceneLoaded += SetSceneManagerDirty;
            SceneManager.sceneUnloaded += SetSceneManagerDirty;

            EditorSceneManager.sceneOpened += SetSceneManagerDirty;
            EditorSceneManager.sceneClosed += SetSceneManagerDirty;
            EditorSceneManager.newSceneCreated += SetSceneManagerDirty;
        }

        protected override void OnDestroy()
        {
            SceneManager.sceneLoaded -= SetSceneManagerDirty;
            SceneManager.sceneUnloaded -= SetSceneManagerDirty;

            EditorSceneManager.sceneOpened -= SetSceneManagerDirty;
            EditorSceneManager.sceneClosed -= SetSceneManagerDirty;
            EditorSceneManager.newSceneCreated -= SetSceneManagerDirty;

            if (m_SceneSectionToSubsceneMap.IsCreated)
                m_SceneSectionToSubsceneMap.Dispose();
        }

        void SetSceneManagerDirty(Scene scene) => SceneManagerDirty = true;
        void SetSceneManagerDirty(Scene scene, LoadSceneMode _) => SceneManagerDirty = true;
        void SetSceneManagerDirty(Scene scene, OpenSceneMode _) => SceneManagerDirty = true;
        void SetSceneManagerDirty(Scene scene, NewSceneSetup _, NewSceneMode __) => SceneManagerDirty = true;

        protected override void OnUpdate()
        {
            var newSceneCountFingerprint = new Hash128(
                (uint)SceneManager.sceneCount,
                (uint)EditorSceneManager.loadedSceneCount,
#if UNITY_2020_1_OR_NEWER
                (uint)EditorSceneManager.loadedRootSceneCount,
#else
                0,
#endif
                (uint)EditorSceneManager.previewSceneCount);

            if (SceneManagerDirty || m_ScenesCountFingerprint != newSceneCountFingerprint)
            {
                RebuildSubsceneMaps();
                m_ScenesCountFingerprint = newSceneCountFingerprint;
                SceneManagerDirty = false;
            }
        }

        void RebuildSubsceneMaps()
        {
            RebuildSubsceneOwnershipMap();
            RebuildSceneSectionsMap();
        }

        void RebuildSubsceneOwnershipMap()
        {
            m_SubsceneOwnershipMap.Clear();
            for (int i = 0; i < SceneManager.sceneCount; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;

                using (var rootGameObjects = PooledList<GameObject>.Make())
                {
                    scene.GetRootGameObjects(rootGameObjects);
                    foreach (var go in rootGameObjects.List)
                    {
                        foreach (var subSceneComponent in go.GetComponentsInChildren<SubScene>())
                        {
                            if (subSceneComponent.SceneAsset != null)
                            {
                                // There could be more than one scene referencing the same subscene, but it is not legal and already throws. Just ignore it here.
                                if (!m_SubsceneOwnershipMap.ContainsKey(subSceneComponent.SceneGUID))
                                    m_SubsceneOwnershipMap.Add(subSceneComponent.SceneGUID, new Hash128(AssetDatabase.AssetPathToGUID(scene.path)));
                            }
                        }
                    }
                }
            }
        }

        void RebuildSceneSectionsMap()
        {
            int sectionsCount = 0;
            Entities
               .WithName("CountSceneSections")
               .ForEach(
                (in DynamicBuffer<ResolvedSectionEntity> buffer) =>
                {
                    sectionsCount += buffer.Length;
                }).Run();

            if (m_SceneSectionToSubsceneMap.IsCreated)
                m_SceneSectionToSubsceneMap.Dispose();
            m_SceneSectionToSubsceneMap = new NativeHashMap<Entity, Entity>(sectionsCount, Allocator.Persistent);

            var parallelBuffer = m_SceneSectionToSubsceneMap.AsParallelWriter();
            var fillJobFence = new JobHandle();

            Entities
               .WithName("CreateSceneSectionLookup")
               .ForEach(
                (Entity entity, in DynamicBuffer<ResolvedSectionEntity> buffer) =>
                {
                    for (int i = 0; i < buffer.Length; ++i)
                        parallelBuffer.TryAdd(buffer[i].SectionEntity, entity);
                }).ScheduleParallel(fillJobFence).Complete();
        }
    }
}
