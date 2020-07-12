using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Unity.Collections;

namespace Unity.Entities.Editor.Tests
{
    class TestHierarchyHelper
    {
        IEntityHierarchyGroupingStrategy m_Strategy;

        public TestHierarchyHelper(IEntityHierarchyGroupingStrategy strategy)
        {
            m_Strategy = strategy;
        }

        public void AssertHierarchy(TestHierarchy expectedHierarchy)
        {
            if (!AssertNode(expectedHierarchy.Root))
                throw new AssertionException(GenerateTreeErrorMessage(expectedHierarchy));

            Assert.That(true);
        }

        bool AssertNode(TestHierarchy.TestNode expectedNode)
        {
            if (!m_Strategy.Exists(expectedNode.NodeId))
                return false;

            var isExpectedToHaveChildren = expectedNode.Children.Count > 0;
            if (m_Strategy.HasChildren(expectedNode.NodeId) != isExpectedToHaveChildren)
                return false;

            if (!isExpectedToHaveChildren)
                return true;

            using (var children = m_Strategy.GetChildren(expectedNode.NodeId, Allocator.Temp))
            {
                if (children.Length != expectedNode.Children.Count)
                    return false;

                var orderedStrategyChildren = children.ToArray().OrderBy(x => x.Id).ToArray();
                var orderedExpectedChildren = expectedNode.Children.OrderBy(x => x.NodeId.Id).ToArray();

                for (var i = 0; i < orderedStrategyChildren.Length; i++)
                {
                    if (!orderedStrategyChildren[i].Equals(orderedExpectedChildren[i].NodeId))
                        return false;
                }
            }

            foreach (var expectedNodeChild in expectedNode.Children)
            {
                if (!AssertNode(expectedNodeChild))
                    return false;
            }

            return true;
        }

        // Asserts a hierarchy where we only know the expected Kind of each EntityHierarchyNodeId, but not the actual Id
        // Useful in integration tests where an external system assigns Entity Ids, during conversion
        public void AssertHierarchyByKind(TestHierarchy expectedHierarchy)
        {
            if (!AssertNodes(new []{expectedHierarchy.Root}, new []{EntityHierarchyNodeId.Root}))
                throw new AssertionException(GenerateTreeErrorMessage(expectedHierarchy));

            Assert.That(true);
        }

        // Breadth-first search, sorted by Kind
        bool AssertNodes(ICollection<TestHierarchy.TestNode> expectedNodes, ICollection<EntityHierarchyNodeId> foundNodes)
        {
            if (expectedNodes.Count != foundNodes.Count)
                return false;

            var sortedExpectedNodes = expectedNodes.OrderBy(node => node.NodeId.Kind).ToList();
            var sortedFoundNodes = foundNodes.OrderBy(node => node.Kind).ToList();

            var expectedChildren = new List<TestHierarchy.TestNode>();
            var foundChildren = new List<EntityHierarchyNodeId>();

            for (int i = 0; i < expectedNodes.Count; ++i)
            {
                var expectedNode = sortedExpectedNodes[i];
                var foundNode = sortedFoundNodes[i];

                if (expectedNode.NodeId.Kind != foundNode.Kind)
                    return false;

                expectedChildren.AddRange(expectedNode.Children);

                if (m_Strategy.HasChildren(foundNode))
                {
                    using (var foundNodeChildren = m_Strategy.GetChildren(foundNode, Allocator.Temp))
                        foundChildren.AddRange(foundNodeChildren);
                }
            }

            if (expectedChildren.Count == 0 && foundChildren.Count == 0)
                return true;

            return AssertNodes(expectedChildren, foundChildren);
        }

        string GenerateTreeErrorMessage(TestHierarchy expectedHierarchy)
        {
            var errorMessage = new StringBuilder();
            errorMessage.AppendLine("Expected hierarchy doesn't match actual strategy state.");
            errorMessage.AppendLine("Expected: ");
            expectedHierarchy.WriteTree(errorMessage, 0);

            errorMessage.AppendLine("But was: ");
            WriteActualStrategyTree(errorMessage, EntityHierarchyNodeId.Root, 0);

            return errorMessage.ToString();
        }

        internal void WriteActualStrategyTree(StringBuilder errorMessage, EntityHierarchyNodeId nodeId, int indent)
        {
            errorMessage.Append(' ', indent);
            errorMessage.Append("- ");
            errorMessage.AppendLine(nodeId.ToString());

            if (!m_Strategy.HasChildren(nodeId))
                return;
            indent++;

            var children = m_Strategy.GetChildren(nodeId, Allocator.Temp);
            foreach (var child in children.OrderBy(x => x.Id))
            {
                WriteActualStrategyTree(errorMessage, child, indent);
            }

            children.Dispose();
        }
    }
}
