using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hagalaz.Collections;
using System.Linq;
using System.Collections.Generic;

namespace Hagalaz.Collections.Tests
{
    [TestClass]
    public class ConcurrentStoreTests
    {
        [TestMethod]
        public void TestAddAndGet()
        {
            var store = new ConcurrentStore<string, int>();
            store.TryAdd("a", 1);
            Assert.AreEqual(1, store["a"]);
        }

        [TestMethod]
        public void TestUpdate()
        {
            var store = new ConcurrentStore<string, int>();
            store.TryAdd("a", 1);
            store["a"] = 2;
            Assert.AreEqual(2, store["a"]);
        }

        [TestMethod]
        public void TestCount()
        {
            var store = new ConcurrentStore<string, int>();
            store.TryAdd("a", 1);
            store.TryAdd("b", 2);
            Assert.AreEqual(2, store.Count);
        }

        [TestMethod]
        public void TestTryRemove()
        {
            var store = new ConcurrentStore<string, int>();
            store.TryAdd("a", 1);
            Assert.IsTrue(store.TryRemove("a"));
            Assert.IsFalse(store.ContainsKey("a"));
        }

        [TestMethod]
        public void TestTryGetValue()
        {
            var store = new ConcurrentStore<string, int>();
            store.TryAdd("a", 1);
            int value;
            Assert.IsTrue(store.TryGetValue("a", out value));
            Assert.AreEqual(1, value);
        }

        [TestMethod]
        public void TestGetOrAdd()
        {
            var store = new ConcurrentStore<string, int>();
            store.GetOrAdd("a", (k) => 1);
            Assert.AreEqual(1, store["a"]);
        }

        [TestMethod]
        public void TestGetOrDefault()
        {
            var store = new ConcurrentStore<string, int>();
            store.TryAdd("a", 1);
            Assert.AreEqual(1, store.GetOrDefault("a"));
            Assert.AreEqual(0, store.GetOrDefault("b"));
        }

        [TestMethod]
        public void TestContainsKey()
        {
            var store = new ConcurrentStore<string, int>();
            store.TryAdd("a", 1);
            Assert.IsTrue(store.ContainsKey("a"));
            Assert.IsFalse(store.ContainsKey("b"));
        }

        [TestMethod]
        public void TestGetEnumerator()
        {
            var store = new ConcurrentStore<string, int>();
            store.TryAdd("a", 1);
            store.TryAdd("b", 2);
            var list = store.ToList();
            Assert.AreEqual(2, list.Count);
        }

        [TestMethod]
        public void TestMultiThreaded()
        {
            var store = new ConcurrentStore<int, int>();
            var tasks = new System.Threading.Tasks.Task[100];
            for (int i = 0; i < 100; i++)
            {
                int local = i;
                tasks[i] = System.Threading.Tasks.Task.Run(() => store.TryAdd(local, local));
            }
            System.Threading.Tasks.Task.WaitAll(tasks);
            Assert.AreEqual(100, store.Count);
        }
    }
}
