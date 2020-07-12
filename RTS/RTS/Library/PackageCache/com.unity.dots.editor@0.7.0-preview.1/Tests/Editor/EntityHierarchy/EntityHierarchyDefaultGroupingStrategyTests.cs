using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Transforms;

namespace Unity.Entities.Editor.Tests
{
    [TestFixture]
    class EntityHierarchyDefaultGroupingStrategyTests
    {
        IEntityHierarchyGroupingStrategy m_Strategy;
        TestHierarchyHelper m_AssertHelper;
        World m_World;

        [SetUp]
        public void Setup()
        {
            m_World = new World("test");
            m_Strategy = new EntityHierarchyDefaultGroupingStrategy(m_World);
            m_AssertHelper = new TestHierarchyHelper(m_Strategy);
        }

        [TearDown]
        public void Teardown()
        {
            m_Strategy.Dispose();
            m_World.Dispose();
        }

        [Test]
        public void EntityHierarchyDefaultGroupingStrategy_NoParenting()
        {
            var entityA = new Entity { Index = 1, Version = 1 };
            var entityB = new Entity { Index = 2, Version = 1 };

            using (var s = new ApplyScope(m_Strategy, 0))
            {
                s.Apply(new[] { entityA, entityB }, new Entity[0]);
            }

            AssertHierarchy(TestHierarchy.CreateRoot()
                                .AddChildren(entityA, entityB)
                                .Build());

            var entityC = new Entity { Index = 3, Version = 1 };

            using (var s = new ApplyScope(m_Strategy, 0))
            {
                s.Apply(new[] { entityC }, new[] { entityB });
            }

            AssertHierarchy(TestHierarchy.CreateRoot()
                                .AddChildren(entityA, entityC)
                                .Build());
        }

        [Test]
        public void EntityHierarchyDefaultGroupingStrategy_SimpleParenting()
        {
            var entityA = new Entity { Index = 1, Version = 1 };
            var entityB = new Entity { Index = 2, Version = 1 };
            var entityC = new Entity { Index = 3, Version = 1 };

            using (var s = new ApplyScope(m_Strategy, 0))
            {
                s.Apply(new[] { entityA, entityB, entityC }, new Entity[0]);
                s.Apply(new[]
                {
                    (entityC, new Parent { Value = entityB }),
                    (entityB, new Parent { Value = entityA })
                }, new (Entity entity, Parent removedParent)[0]);
            }

            AssertHierarchy(TestHierarchy
                                .CreateRoot()
                                    .AddChild(entityA)
                                        .AddChild(entityB)
                                            .AddChild(entityC)
                                .Build());

            using (var s = new ApplyScope(m_Strategy, 0))
            {
                s.Apply( new (Entity entity, Parent removedParent)[0], new[]
                {
                    (entityB, new Parent { Value = entityA })
                });
            }

            var root = TestHierarchy.CreateRoot();
            root.AddChild(entityA);
            root.AddChild(entityB).AddChild(entityC);

            AssertHierarchy(root.Build());
        }

        [Test]
        public void EntityHierarchyDefaultGroupingStrategy_BugRepro()
        {
            var entityA = new Entity { Index = 1, Version = 1 };
            var entityB = new Entity { Index = 2, Version = 1 };
            var entityC = new Entity { Index = 3, Version = 1 };

            using (var s = new ApplyScope(m_Strategy, 0))
            {
                s.Apply(new[] { entityA, entityB, entityC }, new Entity[0]);
                s.Apply(new[]
                {
                    (entityC, new Parent { Value = entityB }),
                    (entityB, new Parent { Value = entityA })
                }, new (Entity entity, Parent removedParent)[0]);
            }

            AssertHierarchy(TestHierarchy
                                .CreateRoot()
                                .AddChild(entityA)
                                .AddChild(entityB)
                                .AddChild(entityC)
                                .Build());

            using (var s = new ApplyScope(m_Strategy, 0))
            {
                s.Apply( new (Entity entity, Parent removedParent)[0], new[]
                {
                    (entityB, new Parent { Value = entityA })
                });
            }

            var root = TestHierarchy.CreateRoot();
            root.AddChild(entityA);
            root.AddChild(entityB).AddChild(entityC);

            AssertHierarchy(root.Build());
        }

        void AssertHierarchy(TestHierarchy expectedHierarchy) => m_AssertHelper.AssertHierarchy(expectedHierarchy);

        class ApplyScope : IDisposable
        {
            IEntityHierarchyGroupingStrategy m_Strategy;
            uint m_Version;

            public ApplyScope(IEntityHierarchyGroupingStrategy strategy, uint version)
            {
                m_Strategy = strategy;
                m_Version = version;
                m_Strategy.BeginApply(m_Version);
            }

            public void Apply(Entity[] newEntities, Entity[] removedEntities)
            {
                var n = new NativeArray<Entity>(newEntities, Allocator.TempJob);
                var r = new NativeArray<Entity>(removedEntities, Allocator.TempJob);

                m_Strategy.ApplyEntityChanges(n, r, m_Version);

                n.Dispose();
                r.Dispose();
            }

            public unsafe void Apply((Entity entity, Parent newParent)[] newParenting, (Entity entity, Parent removedParent)[] removedParenting)
            {
                var buffer = new NativeList<byte>(newParenting.Length + removedParenting.Length * sizeof(Parent), Allocator.TempJob);
                var n = new NativeList<Entity>(newParenting.Length, Allocator.TempJob);
                var r = new NativeList<Entity>(removedParenting.Length, Allocator.TempJob);

                for (var i = 0; i < newParenting.Length; i++)
                {
                    var (entity, newParent) = newParenting[i];
                    n.Add(entity);

                    UnsafeUtility.WriteArrayElement(buffer.GetUnsafePtr(), i, newParent);
                }

                for (var i = 0; i < removedParenting.Length; i++)
                {
                    var (entity, removedParent) = removedParenting[i];
                    r.Add(entity);

                    UnsafeUtility.WriteArrayElement(buffer.GetUnsafePtr(), newParenting.Length + i, removedParent);
                }

                var changes = new ComponentDataDiffer.ComponentChanges(TypeManager.GetTypeIndex<Parent>(), buffer, n, r);
                m_Strategy.ApplyComponentDataChanges(typeof(Parent), changes, m_Version);
                changes.Dispose();
            }

            public void Dispose()
            {
                m_Strategy.EndApply(m_Version);
            }
        }
    }
}
