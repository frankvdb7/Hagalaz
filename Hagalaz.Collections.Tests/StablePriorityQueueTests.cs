using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hagalaz.Collections;

namespace Hagalaz.Collections.Tests
{
    [TestClass]
    public class StablePriorityQueueTests
    {
        [TestMethod]
        public void TestEnqueueAndDequeue()
        {
            var queue = new StablePriorityQueue<TestStableNode>(10);
            var node1 = new TestStableNode { Id = 1 };
            var node2 = new TestStableNode { Id = 2 };
            queue.Enqueue(node1, 1.0f);
            queue.Enqueue(node2, 2.0f);
            Assert.AreEqual(2, queue.Count);
            Assert.AreEqual(node1.Id, queue.Dequeue()?.Id);
            Assert.AreEqual(node2.Id, queue.Dequeue()?.Id);
        }

        [TestMethod]
        public void TestUpdatePriority()
        {
            var queue = new StablePriorityQueue<TestStableNode>(10);
            var node1 = new TestStableNode { Id = 1 };
            var node2 = new TestStableNode { Id = 2 };
            queue.Enqueue(node1, 1.0f);
            queue.Enqueue(node2, 2.0f);
            queue.UpdatePriority(node2, 0.5f);
            Assert.AreEqual(node2.Id, queue.Dequeue()?.Id);
        }
    }

    public class TestStableNode : StablePriorityQueueNode
    {
        public int Id { get; set; }
    }
}
