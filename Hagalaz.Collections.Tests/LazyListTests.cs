using System.Collections.Generic;
using System.Linq;
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
    }
}
