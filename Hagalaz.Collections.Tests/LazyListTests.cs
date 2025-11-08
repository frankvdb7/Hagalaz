using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Collections.Tests
{
    [TestClass]
    public class LazyListTests
    {
        [TestMethod]
        public void ConstructorWithCollection_ShouldInitializeCorrectly()
        {
            var source = new List<int> { 1, 2, 3 };
            var list = new LazyList<int>(source);
            Assert.AreEqual(3, list.Count());
            Assert.AreEqual(1, list.ElementAt(0));
        }

        [TestMethod]
        public void ElementAt_ShouldReturnCorrectElement()
        {
            var source = new List<int> { 10, 20, 30 };
            var list = new LazyList<int>(source);
            Assert.AreEqual(20, list.ElementAt(1));
        }

        [TestMethod]
        public void Count_ShouldReturnCorrectNumberOfElements()
        {
            var source = new List<int> { 1, 2, 3, 4, 5 };
            var list = new LazyList<int>(source);
            Assert.AreEqual(5, list.Count());
        }

        [TestMethod]
        public void Contains_ShouldReturnTrueForExistingElement()
        {
            var source = new List<int> { 1, 2, 3 };
            var list = new LazyList<int>(source);
            Assert.IsTrue(list.Contains(2));
        }

        [TestMethod]
        public void Contains_ShouldReturnFalseForNonExistentElement()
        {
            var source = new List<int> { 1, 2, 3 };
            var list = new LazyList<int>(source);
            Assert.IsFalse(list.Contains(4));
        }

        [TestMethod]
        public void Enumerator_ShouldIterateOverAllElements()
        {
            var source = new List<int> { 1, 2, 3 };
            var list = new LazyList<int>(source);
            var result = new List<int>();
            foreach (var item in list)
            {
                result.Add(item);
            }
            CollectionAssert.AreEqual(source, result);
        }

        [TestMethod]
        public void Contains_WithNullValue_ShouldWorkCorrectly()
        {
            var source = new List<string> { "a", null, "c" };
            var list = new LazyList<string>(source);
            Assert.IsTrue(list.Contains(null));
        }

        [TestMethod]
        public void Constructor_WithIEnumerable_ShouldNotEnumerateSource()
        {
            // Arrange
            var enumerationCount = 0;
            var source = new[] { 1, 2, 3 }.Select(x =>
            {
                enumerationCount++;
                return x;
            });

            // Act
            var list = new LazyList<int>(source);

            // Assert
            Assert.AreEqual(0, enumerationCount, "The source IEnumerable should not be enumerated upon LazyList creation.");
        }

        [TestMethod]
        public void ElementAccess_ShouldCacheElements()
        {
            // Arrange
            var enumerationCount = 0;
            var source = new[] { 1, 2, 3 }.Select(x =>
            {
                enumerationCount++;
                return x;
            });
            var list = new LazyList<int>(source);

            // Act
            var firstElement = list.ElementAt(0);
            var secondElement = list.ElementAt(1);

            // Assert
            Assert.AreEqual(2, enumerationCount, "The source should be enumerated up to the accessed element.");

            // Act again
            var firstElementAgain = list.ElementAt(0);

            // Assert again
            Assert.AreEqual(2, enumerationCount, "Accessing an already enumerated element should not re-enumerate the source.");
        }

        [TestMethod]
        public void PartialEnumeration_ShouldOnlyEnumerateAsNeeded()
        {
            // Arrange
            var enumerationCount = 0;
            var source = new[] { 1, 2, 3, 4, 5 }.Select(x =>
            {
                enumerationCount++;
                return x;
            });
            var list = new LazyList<int>(source);

            // Act
            var subset = list.Take(3).ToList();

            // Assert
            Assert.AreEqual(3, enumerationCount, "The source should only be enumerated as far as needed.");
            Assert.AreEqual(3, subset.Count);
        }

        [TestMethod]
        public void MultiThreaded_Enumeration_ShouldBeThreadSafe()
        {
            // Arrange
            var enumerationCount = 0;
            var source = Enumerable.Range(0, 100).Select(x =>
            {
                Interlocked.Increment(ref enumerationCount);
                return x;
            });
            var list = new LazyList<int>(source);
            var tasks = new List<Task>();
            var allElements = new System.Collections.Concurrent.ConcurrentBag<List<int>>();

            // Act
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    allElements.Add(list.ToList());
                }));
            }
            Task.WhenAll(tasks).Wait();

            // Assert
            Assert.AreEqual(100, enumerationCount, "The source should be enumerated exactly once, even with multiple threads.");
            Assert.AreEqual(10, allElements.Count, "All tasks should complete and add their element lists.");
            foreach (var elements in allElements)
            {
                Assert.AreEqual(100, elements.Count, "Each thread should get all the elements.");
                CollectionAssert.AreEqual(Enumerable.Range(0, 100).ToList(), elements, "The elements should be consistent across all threads.");
            }
        }

        [TestMethod]
        public void Dispose_ShouldDisposeSourceEnumerator()
        {
            // Arrange
            var enumerator = new TestEnumerator();
            var source = new TestEnumerable(enumerator);
            var list = new LazyList<int>(source);

            // Act
            list.Dispose();

            // Assert
            Assert.IsTrue(enumerator.IsDisposed, "The source enumerator should be disposed when the LazyList is disposed.");
        }

        private class TestEnumerable : IEnumerable<int>
        {
            private readonly TestEnumerator _enumerator;

            public TestEnumerable(TestEnumerator enumerator)
            {
                _enumerator = enumerator;
            }

            public IEnumerator<int> GetEnumerator() => _enumerator;

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private class TestEnumerator : IEnumerator<int>
        {
            public bool IsDisposed { get; private set; }

            public int Current => 0;

            object System.Collections.IEnumerator.Current => Current;

            public void Dispose()
            {
                IsDisposed = true;
            }

            public bool MoveNext()
            {
                return false;
            }

            public void Reset()
            {
            }
        }
    }
}
