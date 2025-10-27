using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hagalaz.Collections;
using System.Linq;

namespace Hagalaz.Collections.Tests
{
    [TestClass]
    public class SynchronizedListTests
    {
        [TestMethod]
        public void TestAddAndCount()
        {
            var list = new SynchronizedList<int>();
            list.Add(1);
            list.Add(2);
            Assert.AreEqual(2, list.Count);
        }

        [TestMethod]
        public void TestRemove()
        {
            var list = new SynchronizedList<int>();
            list.Add(1);
            list.Remove(1);
            Assert.AreEqual(0, list.Count);
        }

        [TestMethod]
        public void TestIndexer()
        {
            var list = new SynchronizedList<int>();
            list.Add(1);
            Assert.AreEqual(1, list[0]);
        }

        [TestMethod]
        public void TestMultiThreaded()
        {
            var list = new SynchronizedList<int>();
            var tasks = new System.Threading.Tasks.Task[100];
            for (int i = 0; i < 100; i++)
            {
                int local = i;
                tasks[i] = System.Threading.Tasks.Task.Run(() => list.Add(local));
            }
            System.Threading.Tasks.Task.WaitAll(tasks);
            Assert.AreEqual(100, list.Count);
        }
    }
}
