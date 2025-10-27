using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hagalaz.Collections.Extensions;
using System.Collections.Generic;

namespace Hagalaz.Collections.Extensions.Tests
{
    [TestClass]
    public class CollectionExtensionsTests
    {
        [TestMethod]
        public void TestIndexOf()
        {
            var list = new List<int> { 1, 2, 3 };
            Assert.AreEqual(1, list.IndexOf(i => i == 2));
        }

        [TestMethod]
        public void TestAddRange()
        {
            var set = new HashSet<int> { 1, 2 };
            var list = new List<int> { 3, 4 };
            set.AddRange(list);
            Assert.AreEqual(4, set.Count);
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
