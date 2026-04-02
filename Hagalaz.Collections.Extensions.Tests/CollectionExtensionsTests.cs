using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hagalaz.Collections.Extensions;
using Hagalaz.Collections;
using System.Collections.Generic;

namespace Hagalaz.Collections.Extensions.Tests
{
    [TestClass]
    public class CollectionExtensionsTests
    {
        [TestMethod]
        public void TestIndexOf_List()
        {
            var list = new List<int> { 1, 2, 3 };
            Assert.AreEqual(1, list.IndexOf(i => i == 2));
            Assert.AreEqual(-1, list.IndexOf(i => i == 4));
        }

        [TestMethod]
        public void TestIndexOf_ListHashSet()
        {
            var listHashSet = new ListHashSet<int> { 1, 2, 3 };
            Assert.AreEqual(1, listHashSet.IndexOf(i => i == 2));
            Assert.AreEqual(-1, listHashSet.IndexOf(i => i == 4));
        }

        [TestMethod]
        public void TestIndexOf_IEnumerable()
        {
            IEnumerable<int> YieldItems()
            {
                yield return 1;
                yield return 2;
                yield return 3;
            }
            Assert.AreEqual(1, YieldItems().IndexOf(i => i == 2));
            Assert.AreEqual(-1, YieldItems().IndexOf(i => i == 4));
        }

        [TestMethod]
        public void TestAddRange()
        {
            var set = new HashSet<int> { 1, 2 };
            var list = new List<int> { 3, 4 };
            var result = set.AddRange(list);
            Assert.IsTrue(result);
            Assert.AreEqual(4, set.Count);
        }

        [TestMethod]
        public void TestAddRange_WithDuplicates()
        {
            var set = new HashSet<int> { 1, 2 };
            var list = new List<int> { 2, 3 };
            var result = set.AddRange(list);
            Assert.IsFalse(result);
            Assert.AreEqual(3, set.Count);
            Assert.IsTrue(set.Contains(1));
            Assert.IsTrue(set.Contains(2));
            Assert.IsTrue(set.Contains(3));
        }

        [TestMethod]
        public void TestForEach()
        {
            var list = new List<int> { 1, 2, 3 };
            var sum = 0;
            CollectionExtensions.ForEach(list, i => sum += i);
            Assert.AreEqual(6, sum);
        }
    }
}
