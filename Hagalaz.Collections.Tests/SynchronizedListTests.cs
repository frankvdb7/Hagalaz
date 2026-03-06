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

        [TestMethod]
        public void TestEnumerationLocksCollection()
        {
            var list = new SynchronizedList<int>();
            list.Add(1);

            var enumerator = ((System.Collections.Generic.IEnumerable<int>)list).GetEnumerator();
            try
            {
                // The enumerator should hold the lock now.
                // We try to acquire the lock from another thread.
                bool lockAcquired = false;
                var task = System.Threading.Tasks.Task.Factory.StartNew(() =>
                {
                    // Use Monitor.TryEnter to check if the lock is available.
                    // It should fail because the enumerator thread holds it.
                    lockAcquired = System.Threading.Monitor.TryEnter(list.SyncRoot, 1000);
                    if (lockAcquired)
                    {
                        System.Threading.Monitor.Exit(list.SyncRoot);
                    }
                }, System.Threading.Tasks.TaskCreationOptions.LongRunning);

                task.Wait();

                Assert.IsFalse(lockAcquired, "Lock should be held by the enumerator and not acquirable by others.");

                // Ensure MoveNext still works
                Assert.IsTrue(enumerator.MoveNext());
                Assert.AreEqual(1, enumerator.Current);
            }
            finally
            {
                enumerator.Dispose();
            }
        }
    }
}
