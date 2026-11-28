
namespace Hagalaz.Collections.Tests
{
    [TestClass]
    public class GenericPriorityQueueTests
    {
        [TestMethod]
        public void Dequeue_ReturnsLowestPriorityFirst()
        {
            var queue = new GenericPriorityQueue<TestNode, float>(10);
            var low = new TestNode { Id = 1 };
            var high = new TestNode { Id = 2 };
            queue.Enqueue(high, 2.0f);
            queue.Enqueue(low, 1.0f);

            Assert.AreEqual(2, queue.Count);
            Assert.AreEqual(low.Id, queue.Dequeue()?.Id);
            Assert.AreEqual(high.Id, queue.Dequeue()?.Id);
        }

        [TestMethod]
        public void Dequeue_WithEqualPriorities_ReturnsInInsertionOrder()
        {
            var queue = new GenericPriorityQueue<TestNode, float>(10);
            for (var id = 1; id <= 5; id++)
            {
                queue.Enqueue(new TestNode { Id = id }, 1.0f);
            }

            for (var id = 1; id <= 5; id++)
            {
                Assert.AreEqual(id, queue.Dequeue()?.Id);
            }
        }

        [TestMethod]
        public void UpdatePriority_ReordersNode()
        {
            var queue = new GenericPriorityQueue<TestNode, float>(10);
            var node1 = new TestNode { Id = 1 };
            var node2 = new TestNode { Id = 2 };
            queue.Enqueue(node1, 1.0f);
            queue.Enqueue(node2, 2.0f);

            queue.UpdatePriority(node2, 0.5f);

            Assert.AreEqual(node2.Id, queue.Dequeue()?.Id);
        }

        [TestMethod]
        public void Contains_TracksMembership()
        {
            var queue = new GenericPriorityQueue<TestNode, float>(10);
            var node = new TestNode { Id = 1 };

            Assert.IsFalse(queue.Contains(node));
            queue.Enqueue(node, 1.0f);
            Assert.IsTrue(queue.Contains(node));
            queue.Dequeue();
            Assert.IsFalse(queue.Contains(node));
        }

        [TestMethod]
        public void Remove_TakesNodeOutWithoutDisturbingOrder()
        {
            var queue = new GenericPriorityQueue<TestNode, float>(10);
            var first = new TestNode { Id = 1 };
            var removed = new TestNode { Id = 2 };
            var last = new TestNode { Id = 3 };
            queue.Enqueue(first, 1.0f);
            queue.Enqueue(removed, 2.0f);
            queue.Enqueue(last, 3.0f);

            queue.Remove(removed);

            Assert.AreEqual(2, queue.Count);
            Assert.IsFalse(queue.Contains(removed));
            Assert.AreEqual(first.Id, queue.Dequeue()?.Id);
            Assert.AreEqual(last.Id, queue.Dequeue()?.Id);
        }

        [TestMethod]
        public void Resize_KeepsQueuedNodesAndOrder()
        {
            var queue = new GenericPriorityQueue<TestNode, float>(2);
            queue.Enqueue(new TestNode { Id = 1 }, 1.0f);
            queue.Enqueue(new TestNode { Id = 2 }, 2.0f);

            queue.Resize(5);
            queue.Enqueue(new TestNode { Id = 3 }, 0.5f);

            Assert.AreEqual(5, queue.MaxSize);
            Assert.IsTrue(queue.IsValidQueue());
            Assert.AreEqual(3, queue.Dequeue()?.Id);
            Assert.AreEqual(1, queue.Dequeue()?.Id);
            Assert.AreEqual(2, queue.Dequeue()?.Id);
        }

        private sealed class TestNode : GenericPriorityQueueNode<float>
        {
            public int Id { get; set; }
        }
    }
}
