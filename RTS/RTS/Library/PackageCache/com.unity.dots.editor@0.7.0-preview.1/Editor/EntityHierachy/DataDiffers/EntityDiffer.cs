using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Unity.Entities.Editor
{
    class EntityDiffer : IDisposable
    {
        readonly World m_World;

        NativeHashMap<ulong, ShadowChunk> m_ShadowChunks;

        public EntityDiffer(World world)
        {
            m_World = world;
            m_ShadowChunks = new NativeHashMap<ulong, ShadowChunk>(16, Allocator.Persistent);
        }

        public void Dispose()
        {
            m_ShadowChunks.Dispose();
        }

        public unsafe JobHandle GetEntityQueryMatchDiffAsync(EntityQuery query, NativeList<Entity> newEntities, NativeList<Entity> missingEntities)
        {
            newEntities.Clear();
            missingEntities.Clear();

            var queryMask = m_World.EntityManager.GetEntityQueryMask(query);

            var chunks = query.CreateArchetypeChunkArrayAsync(Allocator.TempJob, out var chunksJobHandle);
            var gatheredChanges = new NativeArray<ChangesCollector>(chunks.Length, Allocator.TempJob);
            var allocatedShadowChunksForTheFrame = new NativeArray<ShadowChunk>(chunks.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var removedChunkEntities = new NativeList<Entity>(Allocator.TempJob);

            var gatherEntityChangesJob = new GatherEntityChangesJob
            {
                QueryMask = queryMask,
                Chunks = chunks,
                ShadowChunksBySequenceNumber = m_ShadowChunks,
                GatheredChanges = (ChangesCollector*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(gatheredChanges)
            }.Schedule(chunks.Length, 1, chunksJobHandle);

            var allocateNewShadowChunksJobHandle = new AllocateNewShadowChunksJob
            {
                QueryMask = queryMask,
                Chunks = chunks,
                ShadowChunksBySequenceNumber = m_ShadowChunks,
                AllocatedShadowChunks = (ShadowChunk*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(allocatedShadowChunksForTheFrame)
            }.Schedule(chunks.Length, 1, chunksJobHandle);

            var copyJobHandle = new CopyEntityDataJob
            {
                QueryMask = queryMask,
                Chunks = chunks, // deallocate on job completion
                ShadowChunksBySequenceNumber = m_ShadowChunks,
                AllocatedShadowChunks = allocatedShadowChunksForTheFrame, // deallocate on job completion
                RemovedChunkEntities = removedChunkEntities
            }.Schedule(JobHandle.CombineDependencies(gatherEntityChangesJob, allocateNewShadowChunksJobHandle));

            var concatResults = new ConcatResultsJob
            {
                GatheredChanges = gatheredChanges, // deallocate on job completion
                RemovedChunkEntities = removedChunkEntities.AsDeferredJobArray(),
                AddedEntities = newEntities,
                RemovedEntities = missingEntities
            }.Schedule(copyJobHandle);

            return removedChunkEntities.Dispose(concatResults);
        }

        unsafe struct ShadowChunk
        {
            public uint Version;
            public int Count;
            public byte* EntityBuffer;
        }

        struct ChangesCollector
        {
            public UnsafeList AddedEntities;
            public UnsafeList RemovedEntities;
        }

        [BurstCompile]
        unsafe struct GatherEntityChangesJob : IJobParallelFor
        {
            public EntityQueryMask QueryMask;

            [ReadOnly]
            public NativeArray<ArchetypeChunk> Chunks;
            [ReadOnly]
            public NativeHashMap<ulong, ShadowChunk> ShadowChunksBySequenceNumber;
            [NativeDisableUnsafePtrRestriction]
            public ChangesCollector* GatheredChanges;

            public void Execute(int index)
            {
                var chunk = Chunks[index].m_Chunk;
                var archetype = chunk->Archetype;
                if (!archetype->CompareMask(QueryMask)) // Archetype doesn't match required query
                    return;

                var changesForChunk = GatheredChanges + index;

                if (ShadowChunksBySequenceNumber.TryGetValue(chunk->SequenceNumber, out var shadow))
                {
                    if (!ChangeVersionUtility.DidChange(chunk->GetChangeVersion(0), shadow.Version))
                        return;

                    if (!changesForChunk->AddedEntities.IsCreated)
                    {
                        changesForChunk->AddedEntities = new UnsafeList(Allocator.TempJob);
                        changesForChunk->RemovedEntities = new UnsafeList(Allocator.TempJob);
                    }

                    var currentEntity = (Entity*)(chunk->Buffer + archetype->Offsets[0]);
                    var entityFromShadow = (Entity*)shadow.EntityBuffer;

                    var currentCount = chunk->Count;
                    var shadowCount = shadow.Count;

                    var i = 0;

                    for (; i < currentCount && i < shadowCount; i++)
                    {
                        if (currentEntity[i] == entityFromShadow[i])
                            continue;

                        // Was a valid entity but version was incremented, thus destroyed
                        if (entityFromShadow[i].Version != 0)
                        {
                            changesForChunk->RemovedEntities.Add(entityFromShadow[i]);
                            changesForChunk->AddedEntities.Add(currentEntity[i]);
                        }
                    }

                    for (; i < currentCount; i++)
                    {
                        // NEW ENTITY!
                        changesForChunk->AddedEntities.Add(currentEntity[i]);
                    }

                    for (; i < shadowCount; i++)
                    {
                        // REMOVED ENTITY!
                        changesForChunk->RemovedEntities.Add(entityFromShadow[i]);
                    }
                }
                else
                {
                    // This is a new chunk
                    var addedComponentEntities = new UnsafeList(sizeof(Entity), 4, chunk->Count, Allocator.TempJob);

                    var entityDataPtr = chunk->Buffer + archetype->Offsets[0];

                    addedComponentEntities.AddRange<Entity>(entityDataPtr, chunk->Count);

                    changesForChunk->AddedEntities = addedComponentEntities;
                }
            }
        }

        [BurstCompile]
        unsafe struct AllocateNewShadowChunksJob : IJobParallelFor
        {
            public EntityQueryMask QueryMask;
            [ReadOnly]
            public NativeArray<ArchetypeChunk> Chunks;
            [ReadOnly]
            public NativeHashMap<ulong, ShadowChunk> ShadowChunksBySequenceNumber;
            [NativeDisableUnsafePtrRestriction]
            public ShadowChunk* AllocatedShadowChunks;

            public void Execute(int index)
            {
                var chunk = Chunks[index].m_Chunk;
                var archetype = chunk->Archetype;
                if (!archetype->CompareMask(QueryMask)) // Archetype doesn't match query
                    return;

                var sequenceNumber = chunk->SequenceNumber;
                if (ShadowChunksBySequenceNumber.TryGetValue(sequenceNumber, out var shadow))
                    return;

                var entityDataPtr = chunk->Buffer + archetype->Offsets[0];

                shadow = new ShadowChunk
                {
                    Count = chunk->Count,
                    Version = chunk->GetChangeVersion(0),
                    EntityBuffer = (byte*)UnsafeUtility.Malloc(sizeof(Entity) * chunk->Capacity, 4, Allocator.Persistent),
                };

                UnsafeUtility.MemCpy(shadow.EntityBuffer, entityDataPtr, chunk->Count * sizeof(Entity));

                AllocatedShadowChunks[index] = shadow;
            }
        }

        [BurstCompile]
        unsafe struct CopyEntityDataJob : IJob
        {
            public EntityQueryMask QueryMask;

            [ReadOnly, DeallocateOnJobCompletion]
            public NativeArray<ArchetypeChunk> Chunks;
            [ReadOnly, DeallocateOnJobCompletion]
            public NativeArray<ShadowChunk> AllocatedShadowChunks;
            public NativeHashMap<ulong, ShadowChunk> ShadowChunksBySequenceNumber;
            [WriteOnly]
            public NativeList<Entity> RemovedChunkEntities;

            public void Execute()
            {
                var knownChunks = ShadowChunksBySequenceNumber.GetKeyArray(Allocator.Temp);
                var processedChunks = new NativeHashMap<ulong, byte>(Chunks.Length, Allocator.Temp);
                for (var index = 0; index < Chunks.Length; index++)
                {
                    var chunk = Chunks[index].m_Chunk;
                    var archetype = chunk->Archetype;
                    if (!archetype->CompareMask(QueryMask)) // Archetype doesn't match query
                        continue;

                    var version = chunk->GetChangeVersion(0);
                    var sequenceNumber = chunk->SequenceNumber;
                    processedChunks.Add(sequenceNumber, 0);
                    var entityDataPtr = chunk->Buffer + archetype->Offsets[0];

                    if (ShadowChunksBySequenceNumber.TryGetValue(sequenceNumber, out var shadow))
                    {
                        if (!ChangeVersionUtility.DidChange(version, shadow.Version))
                            continue;

                        UnsafeUtility.MemCpy(shadow.EntityBuffer, entityDataPtr, chunk->Count * sizeof(Entity));

                        shadow.Count = chunk->Count;
                        shadow.Version = version;

                        ShadowChunksBySequenceNumber[sequenceNumber] = shadow;
                    }
                    else
                    {
                        ShadowChunksBySequenceNumber.Add(sequenceNumber, AllocatedShadowChunks[index]);
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
                        RemovedChunkEntities.AddRange(shadowChunk.EntityBuffer, shadowChunk.Count);

                        UnsafeUtility.Free(shadowChunk.EntityBuffer, Allocator.Persistent);

                        ShadowChunksBySequenceNumber.Remove(chunkSequenceNumber);
                    }
                }

                knownChunks.Dispose();
                processedChunks.Dispose();
            }
        }

        [BurstCompile]
        unsafe struct ConcatResultsJob : IJob
        {
            [ReadOnly, DeallocateOnJobCompletion] public NativeArray<ChangesCollector> GatheredChanges;
            [ReadOnly] public NativeArray<Entity> RemovedChunkEntities;

            [WriteOnly] public NativeList<Entity> AddedEntities;
            [WriteOnly] public NativeList<Entity> RemovedEntities;

            public void Execute()
            {
                var addedEntityCount = 0;
                var removedEntityCount = RemovedChunkEntities.Length;
                for (var i = 0; i < GatheredChanges.Length; i++)
                {
                    var changesForChunk = GatheredChanges[i];
                    addedEntityCount += changesForChunk.AddedEntities.Length;
                    removedEntityCount += changesForChunk.RemovedEntities.Length;
                }

                if (addedEntityCount == 0 && removedEntityCount == 0)
                    return;

                AddedEntities.Capacity = addedEntityCount;
                RemovedEntities.Capacity = removedEntityCount;

                for (var i = 0; i < GatheredChanges.Length; i++)
                {
                    var changes = GatheredChanges[i];
                    if (changes.AddedEntities.IsCreated)
                    {
                        AddedEntities.AddRangeNoResize(changes.AddedEntities.Ptr, changes.AddedEntities.Length);
                        changes.AddedEntities.Dispose();
                    }

                    if (changes.RemovedEntities.IsCreated)
                    {
                        RemovedEntities.AddRangeNoResize(changes.RemovedEntities.Ptr, changes.RemovedEntities.Length);
                        changes.RemovedEntities.Dispose();
                    }
                }

                RemovedEntities.AddRangeNoResize(RemovedChunkEntities.GetUnsafeReadOnlyPtr(), RemovedChunkEntities.Length);
            }
        }
    }
}
