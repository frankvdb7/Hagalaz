using Hagalaz.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hagalaz.Collections.Tests
{
    [TestClass]
    public class ListHashSetTests
    {
        [TestMethod]
        public void Add_IncreasesCount()
        {
            var collection = new ListHashSet<int>();
            collection.Add(1);
            collection.Add(2);
            Assert.AreEqual(2, collection.Count);
        }

        [TestMethod]
        public void Add_IgnoresDuplicates()
        {
            var collection = new ListHashSet<int>();
            collection.Add(1);
            collection.Add(1);
            Assert.AreEqual(1, collection.Count);
        }

        [TestMethod]
        public void Contains_ReturnsCorrectValue()
        {
            var collection = new ListHashSet<int>();
            collection.Add(1);
            Assert.IsTrue(collection.Contains(1));
            Assert.IsFalse(collection.Contains(2));
        }

        [TestMethod]
        public void Indexer_ReturnsCorrectItem()
        {
            var collection = new ListHashSet<int>();
            collection.Add(10);
            collection.Add(20);
            Assert.AreEqual(10, collection[0]);
            Assert.AreEqual(20, collection[1]);
        }

        [TestMethod]
        public void Remove_DecreasesCount()
        {
            var collection = new ListHashSet<int>();
            collection.Add(1);
            collection.Add(2);
            collection.Remove(1);
            Assert.AreEqual(1, collection.Count);
            Assert.IsFalse(collection.Contains(1));
            Assert.AreEqual(2, collection[0]);
        }

        [TestMethod]
        public void Clear_EmptiesCollection()
        {
            var collection = new ListHashSet<int>();
            collection.Add(1);
            collection.Clear();
            Assert.AreEqual(0, collection.Count);
            Assert.IsFalse(collection.Contains(1));
        }

        [TestMethod]
        public void Enumeration_ReturnsAllItemsInOrder()
        {
            var collection = new ListHashSet<int>();
            collection.Add(1);
            collection.Add(2);
            collection.Add(3);

            var list = collection.ToList();
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(1, list[0]);
            Assert.AreEqual(2, list[1]);
            Assert.AreEqual(3, list[2]);
        }

        [TestMethod]
        public void ToListHashSet_PreservesOrder()
        {
            var source = new List<int> { 3, 1, 2 };
            var listHashSet = source.ToListHashSet();

            Assert.AreEqual(3, listHashSet.Count);
            Assert.AreEqual(3, listHashSet[0]);
            Assert.AreEqual(1, listHashSet[1]);
            Assert.AreEqual(2, listHashSet[2]);
        }
    }
}
