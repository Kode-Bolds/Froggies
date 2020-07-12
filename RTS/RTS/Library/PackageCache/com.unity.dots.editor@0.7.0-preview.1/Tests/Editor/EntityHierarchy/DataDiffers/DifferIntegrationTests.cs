using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.Transforms;

namespace Unity.Entities.Editor.Tests
{
    class DifferIntegrationTests : DifferTestFixture
    {
        NativeList<Entity> m_NewEntities;
        NativeList<Entity> m_RemovedEntities;
        IEntityHierarchyGroupingStrategy m_Strategy;
        EntityDiffer m_EntityDiffer;
        ComponentDataDiffer m_ComponentDiffer;
        TestHierarchyHelper m_AssertHelper;

        public override void Setup()
        {
            base.Setup();

            m_NewEntities = new NativeList<Entity>(Allocator.TempJob);
            m_RemovedEntities = new NativeList<Entity>(Allocator.TempJob);

            m_Strategy = new EntityHierarchyDefaultGroupingStrategy(World);
            m_AssertHelper = new TestHierarchyHelper(m_Strategy);

            m_EntityDiffer = new EntityDiffer(World);
            m_ComponentDiffer = new ComponentDataDiffer(m_Strategy.ComponentsToWatch[0]);
        }

        public override void Teardown()
        {
            m_NewEntities.Dispose();
            m_RemovedEntities.Dispose();
            m_Strategy.Dispose();
            m_EntityDiffer.Dispose();
            m_ComponentDiffer.Dispose();

            base.Teardown();
        }

        [Test]
        public unsafe void EntityAndComponentDiffer_EnsureReParenting()
        {
            World.EntityManager.GetCheckedEntityDataAccess()->EntityComponentStore->IncrementGlobalSystemVersion();
            var entityA = World.EntityManager.CreateEntity();
            var entityB = World.EntityManager.CreateEntity();
            {
                GatherChangesAndApplyToStrategy();
                var r = TestHierarchy.CreateRoot();
                r.AddChild(entityA);
                r.AddChild(entityB);

                m_AssertHelper.AssertHierarchy(r.Build());
            }

            World.EntityManager.GetCheckedEntityDataAccess()->EntityComponentStore->IncrementGlobalSystemVersion();
            World.EntityManager.AddComponentData(entityB, new Parent { Value = entityA });
            {
                GatherChangesAndApplyToStrategy();
                m_AssertHelper.AssertHierarchy(TestHierarchy.CreateRoot().AddChild(entityA).AddChild(entityB).Build());
            }

            World.EntityManager.GetCheckedEntityDataAccess()->EntityComponentStore->IncrementGlobalSystemVersion();
            World.EntityManager.RemoveComponent(entityB, typeof(Parent));
            {
                GatherChangesAndApplyToStrategy();
                var r = TestHierarchy.CreateRoot();
                r.AddChild(entityA);
                r.AddChild(entityB);

                m_AssertHelper.AssertHierarchy(r.Build());
            }
        }

        [Test]
        public unsafe void EntityAndComponentDiffer_EnsureParentingToSubEntityWithSimulatingParentSystem()
        {
            World.EntityManager.GetCheckedEntityDataAccess()->EntityComponentStore->IncrementGlobalSystemVersion();
            var entityA = World.EntityManager.CreateEntity();
            var entityB = World.EntityManager.CreateEntity();
            var entityC = World.EntityManager.CreateEntity();
            AssertInSameChunk(entityA, entityB, entityC);

            GatherChangesAndApplyToStrategy();

            var expectedHierarchy = TestHierarchy.CreateRoot();
            expectedHierarchy.AddChild(entityA);
            expectedHierarchy.AddChild(entityB);
            expectedHierarchy.AddChild(entityC);

            m_AssertHelper.AssertHierarchy(expectedHierarchy.Build());

            // All entities are at the root
            // Now parent B to A

            World.EntityManager.GetCheckedEntityDataAccess()->EntityComponentStore->IncrementGlobalSystemVersion();

            World.EntityManager.AddComponentData(entityB, new Parent { Value = entityA });
            AssertInSameChunk(entityA, entityC);
            AssertInDifferentChunks(entityA, entityB);

            GatherChangesAndApplyToStrategy();

            expectedHierarchy = TestHierarchy.CreateRoot();
            expectedHierarchy.AddChild(entityA)
                                .AddChild(entityB);
            expectedHierarchy.AddChild(entityC);

            m_AssertHelper.AssertHierarchy(expectedHierarchy.Build());

            // A and C are at the root, B is under A
            // Now simulate actual ParentSystem by adding Child buffer containing B to new parent A
            // This will move A to a different archetype

            World.EntityManager.GetCheckedEntityDataAccess()->EntityComponentStore->IncrementGlobalSystemVersion();
            World.EntityManager.AddBuffer<Child>(entityA).Add(new Child { Value = entityB });

            AssertInDifferentChunks(entityA, entityB, entityC);

            GatherChangesAndApplyToStrategy();

            expectedHierarchy = TestHierarchy.CreateRoot();
            expectedHierarchy.AddChild(entityA)
                                .AddChild(entityB);
            expectedHierarchy.AddChild(entityC);

            m_AssertHelper.AssertHierarchy(expectedHierarchy.Build());

            // A and C are at the root, B is under A
            // Now parent C to B

            World.EntityManager.GetCheckedEntityDataAccess()->EntityComponentStore->IncrementGlobalSystemVersion();

            World.EntityManager.AddComponentData(entityC, new Parent { Value = entityB });
            AssertInDifferentChunks(entityA, entityB);
            AssertInSameChunk(entityB, entityC);
            GatherChangesAndApplyToStrategy();

            expectedHierarchy = TestHierarchy.CreateRoot();
            expectedHierarchy.AddChild(entityA)
                                .AddChild(entityB)
                                    .AddChild(entityC);

            m_AssertHelper.AssertHierarchy(expectedHierarchy.Build());


            // A is at the root, B is under A, C is under B
            // Now simulate actual ParentSystem by adding Child buffer containing C to new parent B
            // This will move B to a different archetype

            World.EntityManager.GetCheckedEntityDataAccess()->EntityComponentStore->IncrementGlobalSystemVersion();

            var entityBChildren = World.EntityManager.AddBuffer<Child>(entityB);
            entityBChildren.Add(new Child { Value = entityC });

            AssertInDifferentChunks(entityA, entityB, entityC);
            GatherChangesAndApplyToStrategy();

            expectedHierarchy = TestHierarchy.CreateRoot();
            expectedHierarchy.AddChild(entityA)
                                .AddChild(entityB)
                                    .AddChild(entityC);

            m_AssertHelper.AssertHierarchy(expectedHierarchy.Build());
        }

        unsafe void AssertInSameChunk(params Entity[] entities)
        {
            ulong chunkSequenceNumber = 0;

            foreach (var e in entities)
            {
                var c = World.EntityManager.GetChunk(e);
                if (chunkSequenceNumber == 0)
                    chunkSequenceNumber = c.m_Chunk->SequenceNumber;
                else
                    Assert.That(c.m_Chunk->SequenceNumber, Is.EqualTo(chunkSequenceNumber));
            }
        }

        unsafe void AssertInDifferentChunks(params Entity[] entities)
        {
            var chunkSequenceNumbers = new HashSet<ulong>();

            foreach (var e in entities)
            {
                Assert.That(chunkSequenceNumbers.Add(World.EntityManager.GetChunk(e).m_Chunk->SequenceNumber), Is.True);
            }
        }

        void GatherChangesAndApplyToStrategy()
        {
            var entityJobHandle = m_EntityDiffer.GetEntityQueryMatchDiffAsync(World.EntityManager.UniversalQuery, m_NewEntities, m_RemovedEntities);
            using (var changes = m_ComponentDiffer.GatherComponentChangesAsync(World.EntityManager.UniversalQuery, Allocator.TempJob, out var componentJobHandle))
            {
                m_Strategy.BeginApply(World.EntityManager.GlobalSystemVersion);
                entityJobHandle.Complete();
                m_Strategy.ApplyEntityChanges(m_NewEntities, m_RemovedEntities, World.EntityManager.GlobalSystemVersion);
                componentJobHandle.Complete();
                m_Strategy.ApplyComponentDataChanges(typeof(Parent), changes, World.EntityManager.GlobalSystemVersion);
                m_Strategy.EndApply(World.EntityManager.GlobalSystemVersion);
            }
        }
    }
}
