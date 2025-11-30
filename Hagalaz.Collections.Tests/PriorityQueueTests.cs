using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Collections.Tests
{
    [TestClass]
    public class PriorityQueueTests
    {
        [TestMethod]
        public void Count_ShouldBeZeroForNewQueue()
        {
            var queue = new PriorityQueue<string, int>();
            Assert.AreEqual(0, queue.Count);
        }

        [TestMethod]
        public void Count_ShouldBeOneAfterEnqueue()
        {
            var queue = new PriorityQueue<string, int>();
            queue.Enqueue("test", 1);
            Assert.AreEqual(1, queue.Count);
        }

        [TestMethod]
        public void Clear_ShouldMakeQueueEmpty()
        {
            var queue = new PriorityQueue<string, int>();
            queue.Enqueue("test", 1);
            queue.Clear();
            Assert.AreEqual(0, queue.Count);
        }

        [TestMethod]
        public void Enqueue_And_Dequeue_SingleItem_ShouldWork()
        {
            var queue = new PriorityQueue<string, int>();
            queue.Enqueue("test", 1);
            Assert.AreEqual("test", queue.Dequeue());
            Assert.AreEqual(0, queue.Count);
        }

        [TestMethod]
        public void Dequeue_ShouldReturnItemsInPriorityOrder()
        {
            var queue = new PriorityQueue<string, int>();
            queue.Enqueue("low", 3);
            queue.Enqueue("high", 1);
            queue.Enqueue("medium", 2);

            Assert.AreEqual("high", queue.Dequeue());
            Assert.AreEqual("medium", queue.Dequeue());
            Assert.AreEqual("low", queue.Dequeue());
        }

        [TestMethod]
        public void Dequeue_WithDuplicatePriorities_ShouldReturnItemsInFifoOrder()
        {
            var queue = new PriorityQueue<string, int>();
            queue.Enqueue("first", 1);
            queue.Enqueue("second", 1);
            queue.Enqueue("third", 1);

            Assert.AreEqual("first", queue.Dequeue());
            Assert.AreEqual("second", queue.Dequeue());
            Assert.AreEqual("third", queue.Dequeue());
        }

        [TestMethod]
        public void First_ShouldReturnHighestPriorityItem_WithoutRemovingIt()
        {
            var queue = new PriorityQueue<string, int>();
            queue.Enqueue("low", 3);
            queue.Enqueue("high", 1);

            Assert.AreEqual("high", queue.First);
            Assert.AreEqual(2, queue.Count);
            Assert.AreEqual("high", queue.Dequeue());
        }

        [TestMethod]
        public void Remove_ShouldRemoveCorrectItem()
        {
            var queue = new PriorityQueue<string, int>();
            queue.Enqueue("one", 1);
            queue.Enqueue("two", 2);
            queue.Enqueue("three", 3);

            queue.Remove("two");
            Assert.AreEqual("one", queue.Dequeue());
            Assert.AreEqual("three", queue.Dequeue());
        }

        [TestMethod]
        public void Remove_NonExistentItem_ShouldThrowException()
        {
            var queue = new PriorityQueue<string, int>();
            queue.Enqueue("one", 1);
            Assert.ThrowsExactly<InvalidOperationException>(() => queue.Remove("two"));
        }

        [TestMethod]
        public void Dequeue_FromEmptyQueue_ShouldThrowException()
        {
            var queue = new PriorityQueue<string, int>();
            Assert.ThrowsExactly<InvalidOperationException>(() => queue.Dequeue());
        }

        [TestMethod]
        public void First_FromEmptyQueue_ShouldThrowException()
        {
            var queue = new PriorityQueue<string, int>();
            Assert.ThrowsExactly<InvalidOperationException>(() => queue.First);
        }
    }
}
