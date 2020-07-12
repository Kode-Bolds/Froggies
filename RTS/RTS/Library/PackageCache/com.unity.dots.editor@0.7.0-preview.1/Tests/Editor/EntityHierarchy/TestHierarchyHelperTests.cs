using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Unity.Collections;

namespace Unity.Entities.Editor.Tests
{
    class TestHierarchyHelperTests
    {
        DummyStrategy m_Strategy;
        TestHierarchyHelper m_Helper;

        [SetUp]
        public void Setup()
        {
            m_Strategy = new DummyStrategy();
            m_Helper = new TestHierarchyHelper(m_Strategy);
        }

        [Test]
        public void TestHierarchy_AssertSimpleHierarchy()
        {
            m_Strategy.SetHierarchy(TestHierarchy.CreateRoot().Build());
            Assert.DoesNotThrow(() => m_Helper.AssertHierarchy(TestHierarchy.CreateRoot().Build()));

            m_Strategy.SetHierarchy(TestHierarchy.CreateRoot()
                                        .AddChild(new EntityHierarchyNodeId(NodeKind.Entity, 1))
                                        .Build());
            Assert.Throws<AssertionException>(() => m_Helper.AssertHierarchy(TestHierarchy
                                                                                 .CreateRoot()
                                                                                 .AddChild(new EntityHierarchyNodeId(NodeKind.Entity, 2))
                                                                                 .Build()));
        }

        [Test]
        public void TestHierarchy_AssertShouldNotFailOnChildrenOrdering()
        {
            var actualHierarchy = TestHierarchy.CreateRoot();
            actualHierarchy.AddChild(new EntityHierarchyNodeId(NodeKind.Entity, 1));
            actualHierarchy.AddChild(new EntityHierarchyNodeId(NodeKind.Entity, 2));

            var expectedHierarchy = TestHierarchy.CreateRoot();
            expectedHierarchy.AddChild(new EntityHierarchyNodeId(NodeKind.Entity, 2));
            expectedHierarchy.AddChild(new EntityHierarchyNodeId(NodeKind.Entity, 1));


            Assert.That(expectedHierarchy.Children, Is.EquivalentTo(actualHierarchy.Children));
            Assert.That(expectedHierarchy.Children.SequenceEqual(actualHierarchy.Children), Is.False);

            m_Strategy.SetHierarchy(actualHierarchy.Build());
            Assert.DoesNotThrow(() => m_Helper.AssertHierarchy(expectedHierarchy.Build()));
        }

        [Test]
        public void TestHierarchy_AssertSimpleHierarchyByKind()
        {
            m_Strategy.SetHierarchy(TestHierarchy.CreateRoot().Build());
            Assert.DoesNotThrow(() => m_Helper.AssertHierarchyByKind(TestHierarchy.CreateRoot().Build()));

            m_Strategy.SetHierarchy(TestHierarchy.CreateRoot()
                                                 .AddChild(new EntityHierarchyNodeId(NodeKind.Entity, 1))
                                                 .Build());

            // Different ids should not throw if kinds are the same
            Assert.DoesNotThrow(() => m_Helper.AssertHierarchyByKind(TestHierarchy
                                                                    .CreateRoot()
                                                                    .AddChild(new EntityHierarchyNodeId(NodeKind.Entity, 2))
                                                                    .Build()));

            m_Strategy.SetHierarchy(TestHierarchy.CreateRoot()
                                                 .AddChild(new EntityHierarchyNodeId(NodeKind.Scene, 1))
                                                 .Build());

            // Different kinds should throw even if ids are the same
            Assert.Throws<AssertionException>(() => m_Helper.AssertHierarchyByKind(TestHierarchy
                                                                                  .CreateRoot()
                                                                                  .AddChild(new EntityHierarchyNodeId(NodeKind.Entity, 1))
                                                                                  .Build()));
        }

        [Test]
        public void TestHierarchy_AssertComplexHierarchyByKind()
        {
            var sceneId = 0;
            var entityId = 0;

            var hierarchyA1 = TestHierarchy.CreateRoot();
            {
                hierarchyA1.AddChildren(
                    new EntityHierarchyNodeId(NodeKind.Entity, entityId++),
                    new EntityHierarchyNodeId(NodeKind.Entity, entityId++),
                    new EntityHierarchyNodeId(NodeKind.Entity, entityId++));
                hierarchyA1.AddChild(
                                new EntityHierarchyNodeId(NodeKind.Scene, sceneId++))
                               .AddChild(
                                    new EntityHierarchyNodeId(NodeKind.Scene, sceneId++))
                                   .AddChildren(
                                        new EntityHierarchyNodeId(NodeKind.Entity, entityId++),
                                        new EntityHierarchyNodeId(NodeKind.Scene, sceneId++));
                hierarchyA1.AddChild(
                                new EntityHierarchyNodeId(NodeKind.Scene, sceneId++))
                               .AddChildren(
                                    new EntityHierarchyNodeId(NodeKind.Entity, entityId++),
                                    new EntityHierarchyNodeId(NodeKind.Entity, entityId++));
            }

            // Matches A1, but with different ids and in a different order
            var hierarchyA2 = TestHierarchy.CreateRoot();
            {
                hierarchyA2.AddChild(
                                new EntityHierarchyNodeId(NodeKind.Scene, sceneId++))
                               .AddChild(
                                    new EntityHierarchyNodeId(NodeKind.Scene, sceneId++))
                                   .AddChildren(
                                        new EntityHierarchyNodeId(NodeKind.Scene, sceneId++),
                                        new EntityHierarchyNodeId(NodeKind.Entity, entityId++));
                hierarchyA2.AddChildren(
                    new EntityHierarchyNodeId(NodeKind.Entity, entityId++),
                    new EntityHierarchyNodeId(NodeKind.Entity, entityId++));
                hierarchyA2.AddChild(
                                new EntityHierarchyNodeId(NodeKind.Scene, sceneId++))
                               .AddChildren(
                                    new EntityHierarchyNodeId(NodeKind.Entity, entityId++),
                                    new EntityHierarchyNodeId(NodeKind.Entity, entityId++));
                hierarchyA2.AddChild(
                    new EntityHierarchyNodeId(NodeKind.Entity, entityId++));
            }

            // Does not match A1 or A2
            var hierarchyB = TestHierarchy.CreateRoot();
            {
                hierarchyB.AddChild(
                    new EntityHierarchyNodeId(NodeKind.Entity, entityId++));
                hierarchyB.AddChild(
                               new EntityHierarchyNodeId(NodeKind.Scene, sceneId++))
                              .AddChild(
                                   new EntityHierarchyNodeId(NodeKind.Entity, entityId++))
                                  .AddChildren(
                                       new EntityHierarchyNodeId(NodeKind.Scene, sceneId++),
                                       new EntityHierarchyNodeId(NodeKind.Entity, entityId++));
                hierarchyB.AddChildren(
                    new EntityHierarchyNodeId(NodeKind.Entity, entityId++),
                    new EntityHierarchyNodeId(NodeKind.Entity, entityId++));
                hierarchyB.AddChild(
                               new EntityHierarchyNodeId(NodeKind.Scene, sceneId++))
                              .AddChildren(
                                   new EntityHierarchyNodeId(NodeKind.Entity, entityId++),
                                   new EntityHierarchyNodeId(NodeKind.Entity, entityId++));
            }

            m_Strategy.SetHierarchy(hierarchyA1.Build());
            Assert.DoesNotThrow(() => m_Helper.AssertHierarchyByKind(hierarchyA2.Build()));
            Assert.Throws<AssertionException>(() => m_Helper.AssertHierarchyByKind(hierarchyB.Build()));
        }

        [Test]
        public void TestHierarchy_ErrorMessageShouldPrintOrderedChildren()
        {
            var actualHierarchy = TestHierarchy.CreateRoot();
            actualHierarchy.AddChild(new EntityHierarchyNodeId(NodeKind.Entity, 1));
            actualHierarchy.AddChild(new EntityHierarchyNodeId(NodeKind.Entity, 2));
            m_Strategy.SetHierarchy(actualHierarchy.Build());

            var testHierarchy = TestHierarchy.CreateRoot();
            testHierarchy.AddChild(new EntityHierarchyNodeId(NodeKind.Entity, 2));
            testHierarchy.AddChild(new EntityHierarchyNodeId(NodeKind.Entity, 1));

            var builder = new StringBuilder();

            testHierarchy.Build().WriteTree(builder, 0);
            var testHierarchyString = builder.ToString();

            builder.Clear();

            m_Helper.WriteActualStrategyTree(builder, EntityHierarchyNodeId.Root, 0);
            var strategyHierarchyString = builder.ToString();

            Assert.That(testHierarchyString, Is.EqualTo(strategyHierarchyString));
        }

        class DummyStrategy : IEntityHierarchyGroupingStrategy
        {
            Dictionary<EntityHierarchyNodeId, EntityHierarchyNodeId> m_Parents = new Dictionary<EntityHierarchyNodeId, EntityHierarchyNodeId>();
            Dictionary<EntityHierarchyNodeId, List<EntityHierarchyNodeId>> m_Children = new Dictionary<EntityHierarchyNodeId, List<EntityHierarchyNodeId>>();

            public void SetHierarchy(TestHierarchy expectedHierarchy)
            {
                m_Parents.Clear();
                m_Children.Clear();

                BuildState(expectedHierarchy.Root);
            }

            void BuildState(TestHierarchy.TestNode testNode)
            {
                if (!m_Children.TryGetValue(testNode.NodeId, out var children))
                {
                    children = new List<EntityHierarchyNodeId>();
                    m_Children.Add(testNode.NodeId, children);
                }

                foreach (var child in testNode.Children)
                {
                    children.Add(child.NodeId);
                    m_Parents[child.NodeId] = testNode.NodeId;
                }

                foreach (var child in testNode.Children)
                {
                    BuildState(child);
                }
            }

            public void Dispose() {}
            public World World { get; }
            public ComponentType[] ComponentsToWatch { get; }
            public void BeginApply(uint version) {}
            public void ApplyEntityChanges(NativeArray<Entity> newEntities, NativeArray<Entity> removedEntities, uint version) {}
            public void ApplyComponentDataChanges(ComponentType componentType, in ComponentDataDiffer.ComponentChanges componentChanges, uint version) {}
            public void ApplySharedComponentDataChanges(ComponentType componentType, in SharedComponentDataDiffer.ComponentChanges componentChanges, uint version) {}
            public bool EndApply(uint version) => false;

            public bool HasChildren(in EntityHierarchyNodeId nodeId)
                => m_Children.TryGetValue(nodeId, out var children) && children.Count > 0;

            public NativeArray<EntityHierarchyNodeId> GetChildren(in EntityHierarchyNodeId nodeId, Allocator allocator)
                => new NativeArray<EntityHierarchyNodeId>(m_Children[nodeId].ToArray(), allocator);

            public bool Exists(in EntityHierarchyNodeId nodeId)
                => m_Parents.ContainsKey(nodeId) || m_Children.ContainsKey(nodeId);

            public Entity GetUnderlyingEntity(in EntityHierarchyNodeId nodeId)
            {
                throw new NotImplementedException();
            }

            public uint GetNodeVersion(in EntityHierarchyNodeId nodeId)
            {
                throw new NotImplementedException();
            }

            public string GetNodeName(in EntityHierarchyNodeId nodeId)
            {
                throw new NotImplementedException();
            }
        }
    }
}
