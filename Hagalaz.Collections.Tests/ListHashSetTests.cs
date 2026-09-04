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
        private sealed class EqualityProbe : IEquatable<EqualityProbe>
        {
            private readonly Action _onEquals;

            public EqualityProbe(int value, Action onEquals)
            {
                Value = value;
                _onEquals = onEquals;
            }

            public int Value { get; }

            public bool Equals(EqualityProbe? other)
            {
                _onEquals();
                return other is not null && Value == other.Value;
            }

            public override bool Equals(object? obj) => Equals(obj as EqualityProbe);

            public override int GetHashCode() => Value;
        }

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
            Assert.HasCount(3, list);
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

        [TestMethod]
        public void Contains_UsesHashSetLookup()
        {
            // Arrange
            const int itemCount = 10000;
            var equalityCount = 0;
            var items = Enumerable.Range(0, itemCount)
                .Select(value => new EqualityProbe(value, () => equalityCount++))
                .ToList();
            var listHashSet = items.ToListHashSet();
            var lookup = new EqualityProbe(itemCount - 1, () => equalityCount++);

            // Act
            Assert.IsTrue(items.Contains(lookup));
            var listEqualityCount = equalityCount;

            equalityCount = 0;
            Assert.IsTrue(listHashSet.Contains(lookup));
            var listHashSetEqualityCount = equalityCount;

            // Assert
            Assert.AreEqual(itemCount, listEqualityCount);
            Assert.AreEqual(1, listHashSetEqualityCount);
        }
    }
}
