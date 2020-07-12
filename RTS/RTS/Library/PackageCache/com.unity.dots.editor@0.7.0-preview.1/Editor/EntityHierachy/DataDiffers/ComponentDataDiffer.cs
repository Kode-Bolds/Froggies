using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Unity.Entities.Editor
{
    class ComponentDataDiffer : IDisposable
    {
        readonly int m_TypeIndex;
        readonly int m_ComponentSize;
        readonly NativeHashMap<ulong, ShadowChunk> m_PreviousChunksBySequenceNumber;

        public ComponentDataDiffer(ComponentType componentType)
        {
            if (!CanWatch(componentType))
                throw new ArgumentException($"{nameof(ComponentDataDiffer)} only supports unmanaged {nameof(IComponentData)} components.", nameof(componentType));

            var typeInfo = TypeManager.GetTypeInfo(componentType.TypeIndex);

            WatchedComponentType = componentType;
            m_TypeIndex = typeInfo.TypeIndex;
            m_ComponentSize = typeInfo.SizeInChunk;
            m_PreviousChunksBySequenceNumber = new NativeHashMap<ulong, ShadowChunk>(16, Allocator.Persistent);
        }

        public ComponentType WatchedComponentType { get; }

        public static bool CanWatch(ComponentType componentType)
        {
            var typeInfo = TypeManager.GetTypeInfo(componentType.TypeIndex);
            return typeInfo.Category == TypeManager.TypeCategory.ComponentData && UnsafeUtility.IsUnmanaged(componentType.GetManagedType());
        }

        public void Dispose()
        {
            unsafe
            {
                using (var array = m_PreviousChunksBySequenceNumber.GetValueArray(Allocator.Temp))
                {
                    for (var i = 0; i < array.Length; i++)
                    {
                        UnsafeUtility.Free(array[i].EntityDataBuffer, Allocator.Persistent);
                        UnsafeUtility.Free(array[i].ComponentDataBuffer, Allocator.Persistent);
                    }
                }
            }

            m_PreviousChunksBySequenceNumber.Dispose();
        }

        public unsafe ComponentChanges GatherComponentChangesAsync(EntityQuery query, Allocator allocator, out JobHandle jobHandle)
        {
            var chunks = query.CreateArchetypeChunkArrayAsync(Allocator.TempJob, out var chunksJobHandle);
            var allocatedShadowChunksForTheFrame = new NativeArray<ShadowChunk>(chunks.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var gatheredChanges = new NativeArray<ChangesCollector>(chunks.Length, Allocator.TempJob);
            var removedChunkBuffer = new NativeList<byte>(Allocator.TempJob);
            var removedChunkEntities = new NativeList<Entity>(Allocator.TempJob);

            var buffer = new NativeList<byte>(allocator);
            var addedComponents = new NativeList<Entity>(allocator);
            var removedComponents = new NativeList<Entity>(allocator);

            var changesJobHandle = new GatherComponentChangesJob
            {
                TypeIndex = m_TypeIndex,
                ComponentSize = m_ComponentSize,
                Chunks = chunks,
                ShadowChunksBySequenceNumber = m_PreviousChunksBySequenceNumber,
                GatheredChanges = (ChangesCollector*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(gatheredChanges)
            }.Schedule(chunks.Length, 1, chunksJobHandle);

            var allocateNewShadowChunksJobHandle = new AllocateNewShadowChunksJob
            {
                TypeIndex = m_TypeIndex,
                ComponentSize = m_ComponentSize,
                Chunks = chunks,
                ShadowChunksBySequenceNumber = m_PreviousChunksBySequenceNumber,
                AllocatedShadowChunks = (ShadowChunk*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(allocatedShadowChunksForTheFrame)
            }.Schedule(chunks.Length, 1, chunksJobHandle);

            var copyJobHandle = new CopyComponentDataJob
            {
                TypeIndex = m_TypeIndex,
                ComponentSize = m_ComponentSize,
                Chunks = chunks,
                ShadowChunksBySequenceNumber = m_PreviousChunksBySequenceNumber,
                AllocatedShadowChunks = (ShadowChunk*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(allocatedShadowChunksForTheFrame),
                RemovedChunkComponentDataBuffer = removedChunkBuffer,
                RemovedChunkEntities = removedChunkEntities
            }.Schedule(JobHandle.CombineDependencies(changesJobHandle, allocateNewShadowChunksJobHandle));

            var concatResultJobHandle = new ConcatResultJob
            {
                ComponentSize = m_ComponentSize,
                GatheredChanges = gatheredChanges,
                RemovedChunkComponentDataBuffer = removedChunkBuffer.AsDeferredJobArray(),
                RemovedChunkEntities = removedChunkEntities.AsDeferredJobArray(),

                ComponentDataBuffer = buffer,
                AddedComponents = addedComponents,
                RemovedComponents = removedComponents
            }.Schedule(copyJobHandle);

            var handles = new NativeArray<JobHandle>(5, Allocator.Temp)
            {
                [0] = chunks.Dispose(copyJobHandle),
                [1] = gatheredChanges.Dispose(concatResultJobHandle),
                [2] = removedChunkBuffer.Dispose(concatResultJobHandle),
                [3] = removedChunkEntities.Dispose(concatResultJobHandle),
                [4] = allocatedShadowChunksForTheFrame.Dispose(copyJobHandle)
            };
            jobHandle = JobHandle.CombineDependencies(handles);
            handles.Dispose();

            return new ComponentChanges(m_TypeIndex, buffer, addedComponents, removedComponents);
        }

        [BurstCompile]
        unsafe struct GatherComponentChangesJob : IJobParallelFor
        {
            public int TypeIndex;
            public int ComponentSize;

            [ReadOnly] public NativeArray<ArchetypeChunk> Chunks;
            [ReadOnly] public NativeHashMap<ulong, ShadowChunk> ShadowChunksBySequenceNumber;
            [NativeDisableUnsafePtrRestriction] public ChangesCollector* GatheredChanges;

            public void Execute(int index)
            {
                var chunk = Chunks[index].m_Chunk;
                var archetype = chunk->Archetype;
                var indexInTypeArray = ChunkDataUtility.GetIndexInTypeArray(archetype, TypeIndex);
                if (indexInTypeArray == -1) // Archetype doesn't match required component
                    return;

                var changesForChunk = GatheredChanges + index;

                if (ShadowChunksBySequenceNumber.TryGetValue(chunk->SequenceNumber, out var shadow))
                {
                    if (!ChangeVersionUtility.DidChange(chunk->GetChangeVersion(indexInTypeArray), shadow.ComponentVersion)
                        && !ChangeVersionUtility.DidChange(chunk->GetChangeVersion(0), shadow.EntityVersion))
                        return;

                    if (!changesForChunk->AddedComponentEntities.IsCreated)
                    {
                        changesForChunk->AddedComponentEntities = new UnsafeList(Allocator.TempJob);
                        changesForChunk->AddedComponentDataBuffer = new UnsafeList(Allocator.TempJob);
                        changesForChunk->RemovedComponentEntities = new UnsafeList(Allocator.TempJob);
                        changesForChunk->RemovedComponentDataBuffer = new UnsafeList(Allocator.TempJob);
                    }

                    var entityDataPtr = chunk->Buffer + archetype->Offsets[0];
                    var componentDataPtr = chunk->Buffer + archetype->Offsets[indexInTypeArray];

                    var currentCount = chunk->Count;
                    var previousCount = shadow.Count;

                    var i = 0;

                    for (; i < currentCount && i < previousCount; i++)
                    {
                        var currentComponentData = componentDataPtr + ComponentSize * i;
                        var previousComponentData = shadow.ComponentDataBuffer + ComponentSize * i;

                        var entity = *(Entity*)(entityDataPtr + sizeof(Entity) * i);
                        var previousEntity = *(Entity*)(shadow.EntityDataBuffer + sizeof(Entity) * i);

                        if (entity != previousEntity
                            || UnsafeUtility.MemCmp(currentComponentData, previousComponentData, ComponentSize) != 0)
                        {
                            // CHANGED COMPONENT DATA!
                            OnRemovedComponent(changesForChunk, previousEntity, previousComponentData, ComponentSize);
                            OnNewComponent(changesForChunk, entity, currentComponentData, ComponentSize);
                        }
                    }

                    for (; i < currentCount; i++)
                    {
                        // NEW COMPONENT DATA!
                        var entity = *(Entity*)(entityDataPtr + sizeof(Entity) * i);
                        var currentComponentData = componentDataPtr + ComponentSize * i;
                        OnNewComponent(changesForChunk, entity, currentComponentData, ComponentSize);
                    }

                    for (; i < previousCount; i++)
                    {
                        // REMOVED COMPONENT DATA!
                        var entity = *(Entity*)(entityDataPtr + sizeof(Entity) * i);
                        var previousComponentData = shadow.ComponentDataBuffer + ComponentSize * i;
                        OnRemovedComponent(changesForChunk, entity, previousComponentData, ComponentSize);
                    }
                }
                else
                {
                    // This is a new chunk
                    var addedComponentDataBuffer = new UnsafeList(ComponentSize, 4, chunk->Count, Allocator.TempJob);
                    var addedComponentEntities = new UnsafeList(sizeof(Entity), 4, chunk->Count, Allocator.TempJob);

                    var entityDataPtr = chunk->Buffer + archetype->Offsets[0];
                    var componentDataPtr = chunk->Buffer + archetype->Offsets[indexInTypeArray];

                    addedComponentDataBuffer.AddRange<byte>(componentDataPtr, chunk->Count * ComponentSize);
                    addedComponentEntities.AddRange<Entity>(entityDataPtr, chunk->Count);

                    changesForChunk->AddedComponentDataBuffer = addedComponentDataBuffer;
                    changesForChunk->AddedComponentEntities = addedComponentEntities;
                }
            }

            static void OnNewComponent(ChangesCollector* changesForChunk, Entity entity, byte* currentComponentData, int componentSize)
            {
                changesForChunk->AddedComponentEntities.Add(entity);
                changesForChunk->AddedComponentDataBuffer.AddRange<byte>(currentComponentData, componentSize);
            }

            static void OnRemovedComponent(ChangesCollector* changesForChunk, Entity entity, byte* previousComponentData, int componentSize)
            {
                changesForChunk->RemovedComponentEntities.Add(entity);
                changesForChunk->RemovedComponentDataBuffer.AddRange<byte>(previousComponentData, componentSize);
            }
        }

        [BurstCompile]
        unsafe struct AllocateNewShadowChunksJob : IJobParallelFor
        {
            public int TypeIndex;
            public int ComponentSize;
            [ReadOnly] public NativeArray<ArchetypeChunk> Chunks;
            [ReadOnly] public NativeHashMap<ulong, ShadowChunk> ShadowChunksBySequenceNumber;
            [NativeDisableUnsafePtrRestriction] public ShadowChunk* AllocatedShadowChunks;

            public void Execute(int index)
            {
                var chunk = Chunks[index].m_Chunk;
                var archetype = chunk->Archetype;
                var indexInTypeArray = ChunkDataUtility.GetIndexInTypeArray(archetype, TypeIndex);
                if (indexInTypeArray == -1) // Archetype doesn't match required component
                    return;

                var sequenceNumber = chunk->SequenceNumber;
                if (ShadowChunksBySequenceNumber.TryGetValue(sequenceNumber, out var shadow))
                    return;

                var entityDataPtr = chunk->Buffer + archetype->Offsets[0];
                var componentDataPtr = chunk->Buffer + archetype->Offsets[indexInTypeArray];

                shadow = new ShadowChunk
                {
                    Count = chunk->Count,
                    ComponentVersion = chunk->GetChangeVersion(indexInTypeArray),
                    EntityVersion = chunk->GetChangeVersion(0),
                    EntityDataBuffer = (byte*)UnsafeUtility.Malloc(sizeof(Entity) * chunk->Capacity, 4, Allocator.Persistent),
                    ComponentDataBuffer = (byte*)UnsafeUtility.Malloc(ComponentSize * chunk->Capacity, 4, Allocator.Persistent)
                };

                UnsafeUtility.MemCpy(shadow.EntityDataBuffer, entityDataPtr, chunk->Count * sizeof(Entity));
                UnsafeUtility.MemCpy(shadow.ComponentDataBuffer, componentDataPtr, chunk->Count * ComponentSize);

                AllocatedShadowChunks[index] = shadow;
            }
        }

        [BurstCompile]
        unsafe struct CopyComponentDataJob : IJob
        {
            public int TypeIndex;
            public int ComponentSize;

            [ReadOnly] public NativeArray<ArchetypeChunk> Chunks;
            [ReadOnly, NativeDisableUnsafePtrRestriction] public ShadowChunk* AllocatedShadowChunks;
            public NativeHashMap<ulong, ShadowChunk> ShadowChunksBySequenceNumber;
            [WriteOnly] public NativeList<byte> RemovedChunkComponentDataBuffer;
            [WriteOnly] public NativeList<Entity> RemovedChunkEntities;

            public void Execute()
            {
                var knownChunks = ShadowChunksBySequenceNumber.GetKeyArray(Allocator.Temp);
                var processedChunks = new NativeHashMap<ulong, byte>(Chunks.Length, Allocator.Temp);
                for (var index = 0; index < Chunks.Length; index++)
                {
                    var chunk = Chunks[index].m_Chunk;
                    var archetype = chunk->Archetype;
                    var indexInTypeArray = ChunkDataUtility.GetIndexInTypeArray(archetype, TypeIndex);
                    if (indexInTypeArray == -1) // Archetype doesn't match required component
                        continue;

                    var componentVersion = chunk->GetChangeVersion(indexInTypeArray);
                    var entityVersion = chunk->GetChangeVersion(0);
                    var sequenceNumber = chunk->SequenceNumber;
                    processedChunks.Add(sequenceNumber, 0);
                    var entityDataPtr = chunk->Buffer + archetype->Offsets[0];
                    var componentDataPtr = chunk->Buffer + archetype->Offsets[indexInTypeArray];

                    if (ShadowChunksBySequenceNumber.TryGetValue(sequenceNumber, out var shadow))
                    {
                        if (!ChangeVersionUtility.DidChange(componentVersion, shadow.ComponentVersion)
                            && !ChangeVersionUtility.DidChange(entityVersion, shadow.EntityVersion))
                            continue;

                        UnsafeUtility.MemCpy(shadow.EntityDataBuffer, entityDataPtr, chunk->Count * sizeof(Entity));
                        UnsafeUtility.MemCpy(shadow.ComponentDataBuffer, componentDataPtr, chunk->Count * ComponentSize);

                        shadow.Count = chunk->Count;
                        shadow.ComponentVersion = componentVersion;
                        shadow.EntityVersion = entityVersion;

                        ShadowChunksBySequenceNumber[sequenceNumber] = shadow;
                    }
                    else
                    {
                        ShadowChunksBySequenceNumber.Add(sequenceNumber, *(AllocatedShadowChunks + index));
                    }
                }

                for (var i = 0; i < knownChunks.Length; i++)
                {
                    var chunkSequenceNumber = knownChunks[i];
                    if (!processedChunks.ContainsKey(chunkSequenceNumber))
                    {
                        // This is a missing chunk
                        var shadowChunk = ShadowChunksBySequenceNumber[chunkSequenceNumber];

                        // REMOVED COMPONENT DATA!
                        RemovedChunkComponentDataBuffer.AddRange(shadowChunk.ComponentDataBuffer, shadowChunk.Count * ComponentSize);
                        RemovedChunkEntities.AddRange(shadowChunk.EntityDataBuffer, shadowChunk.Count);

                        UnsafeUtility.Free(shadowChunk.ComponentDataBuffer, Allocator.Persistent);
                        UnsafeUtility.Free(shadowChunk.EntityDataBuffer, Allocator.Persistent);

                        ShadowChunksBySequenceNumber.Remove(chunkSequenceNumber);
                    }
                }

                knownChunks.Dispose();
                processedChunks.Dispose();
            }
        }

        [BurstCompile]
        unsafe struct ConcatResultJob : IJob
        {
            public int ComponentSize;
            [ReadOnly] public NativeArray<ChangesCollector> GatheredChanges;
            [ReadOnly] public NativeArray<byte> RemovedChunkComponentDataBuffer;
            [ReadOnly] public NativeArray<Entity> RemovedChunkEntities;

            [WriteOnly] public NativeList<byte> ComponentDataBuffer;
            [WriteOnly] public NativeList<Entity> AddedComponents;
            [WriteOnly] public NativeList<Entity> RemovedComponents;

            public void Execute()
            {
                var addedEntityCount = 0;
                var removedEntityCount = RemovedChunkEntities.Length;
                for (var i = 0; i < GatheredChanges.Length; i++)
                {
                    var changesForChunk = GatheredChanges[i];
                    addedEntityCount += changesForChunk.AddedComponentEntities.Length;
                    removedEntityCount += changesForChunk.RemovedComponentEntities.Length;
                }

                if (addedEntityCount == 0 && removedEntityCount == 0)
                    return;

                ComponentDataBuffer.Capacity = (addedEntityCount + removedEntityCount) * ComponentSize;
                AddedComponents.Capacity = addedEntityCount;
                RemovedComponents.Capacity = removedEntityCount;

                var chunksWithRemovedData = new NativeList<int>(GatheredChanges.Length, Allocator.Temp);
                for (var i = 0; i < GatheredChanges.Length; i++)
                {
                    var changesForChunk = GatheredChanges[i];
                    if (changesForChunk.AddedComponentDataBuffer.IsCreated)
                    {
                        ComponentDataBuffer.AddRangeNoResize(changesForChunk.AddedComponentDataBuffer.Ptr, changesForChunk.AddedComponentDataBuffer.Length);
                        AddedComponents.AddRangeNoResize(changesForChunk.AddedComponentEntities.Ptr, changesForChunk.AddedComponentEntities.Length);

                        changesForChunk.AddedComponentDataBuffer.Dispose();
                        changesForChunk.AddedComponentEntities.Dispose();
                    }

                    if (changesForChunk.RemovedComponentDataBuffer.IsCreated)
                        chunksWithRemovedData.AddNoResize(i);
                }

                for (var i = 0; i < chunksWithRemovedData.Length; i++)
                {
                    var changesForChunk = GatheredChanges[chunksWithRemovedData[i]];
                    ComponentDataBuffer.AddRangeNoResize(changesForChunk.RemovedComponentDataBuffer.Ptr, changesForChunk.RemovedComponentDataBuffer.Length);
                    RemovedComponents.AddRangeNoResize(changesForChunk.RemovedComponentEntities.Ptr, changesForChunk.RemovedComponentEntities.Length);

                    changesForChunk.RemovedComponentDataBuffer.Dispose();
                    changesForChunk.RemovedComponentEntities.Dispose();
                }

                chunksWithRemovedData.Dispose();

                ComponentDataBuffer.AddRangeNoResize(RemovedChunkComponentDataBuffer.GetUnsafeReadOnlyPtr(), RemovedChunkComponentDataBuffer.Length);
                RemovedComponents.AddRangeNoResize(RemovedChunkEntities.GetUnsafeReadOnlyPtr(), RemovedChunkEntities.Length);
            }
        }

        unsafe struct ShadowChunk
        {
            public uint ComponentVersion;
            public uint EntityVersion;
            public int Count;
            public byte* EntityDataBuffer;
            public byte* ComponentDataBuffer;
        }

        struct ChangesCollector
        {
            public UnsafeList AddedComponentDataBuffer;
            public UnsafeList RemovedComponentDataBuffer;
            public UnsafeList AddedComponentEntities;
            public UnsafeList RemovedComponentEntities;
        }

        internal readonly struct ComponentChanges : IDisposable
        {
            readonly int m_ComponentTypeIndex;
            readonly NativeList<byte> m_Buffer;
            readonly NativeList<Entity> m_AddedComponents;
            readonly NativeList<Entity> m_RemovedComponents;

            public ComponentChanges(int componentTypeIndex,
                                    NativeList<byte> buffer,
                                    NativeList<Entity> addedComponents,
                                    NativeList<Entity> removedComponents)
            {
                m_ComponentTypeIndex = componentTypeIndex;
                m_Buffer = buffer;
                m_AddedComponents = addedComponents;
                m_RemovedComponents = removedComponents;
            }

            public int AddedComponentsCount => m_AddedComponents.Length;
            public int RemovedComponentsCount => m_RemovedComponents.Length;

            public unsafe (NativeArray<Entity> entities, NativeArray<T> componentData) GetAddedComponents<T>(Allocator allocator) where T : struct
            {
                EnsureIsExpectedComponent<T>();

                var entities = new NativeArray<Entity>(m_AddedComponents, allocator);
                var components = new NativeArray<T>(m_AddedComponents.Length, allocator);
                UnsafeUtility.MemCpy(components.GetUnsafePtr(), (byte*)m_Buffer.GetUnsafeReadOnlyPtr(), m_AddedComponents.Length * UnsafeUtility.SizeOf<T>());

                return (entities, components);
            }

            public unsafe (NativeArray<Entity> entities, NativeArray<T> componentData) GetRemovedComponents<T>(Allocator allocator) where T : struct
            {
                EnsureIsExpectedComponent<T>();

                var entities = new NativeArray<Entity>(m_RemovedComponents, allocator);
                var components = new NativeArray<T>(m_RemovedComponents.Length, allocator);
                UnsafeUtility.MemCpy(components.GetUnsafePtr(), (byte*)m_Buffer.GetUnsafeReadOnlyPtr() + m_AddedComponents.Length * UnsafeUtility.SizeOf<T>(), m_RemovedComponents.Length * UnsafeUtility.SizeOf<T>());

                return (entities, components);
            }

            void EnsureIsExpectedComponent<T>() where T : struct
            {
                if (TypeManager.GetTypeIndex<T>() != m_ComponentTypeIndex)
                    throw new InvalidOperationException($"Unable to retrieve data for component type {typeof(T)} (type index {TypeManager.GetTypeIndex<T>()}), this container only holds data for the type with type index {m_ComponentTypeIndex}.");
            }

            public void Dispose()
            {
                m_Buffer.Dispose();
                m_AddedComponents.Dispose();
                m_RemovedComponents.Dispose();
            }
        }
    }
}
