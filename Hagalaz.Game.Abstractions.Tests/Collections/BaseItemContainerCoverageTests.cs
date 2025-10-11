using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Cache.Abstractions.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Collections.Generic;
using System;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Tests.Collections
{
    [TestClass]
    public class BaseItemContainerCoverageTests
    {
        private class TestableItemContainer : BaseItemContainer
        {
            public TestableItemContainer(StorageType type, int capacity) : base(type, capacity)
            {
            }

            public override void OnUpdate(HashSet<int>? slots = null)
            {
                // No-op for testing
            }
        }

        private class TestItem : IItem
        {
            public int Id { get; set; }
            public int Count { get; set; }
            public string Name { get; }
            public IItemDefinition ItemDefinition { get; }
            public IEquipmentDefinition EquipmentDefinition { get; }
            public IItemScript ItemScript { get; }
            public IEquipmentScript EquipmentScript { get; }
            public long[] ExtraData => Array.Empty<long>();

            public TestItem(int id, int count, bool stackable = false, bool noted = false)
            {
                Id = id;
                Count = count;
                Name = $"TestItem_{id}";

                var itemDef = Substitute.For<IItemDefinition>();
                itemDef.Stackable.Returns(stackable);
                itemDef.Noted.Returns(noted);
                ItemDefinition = itemDef;

                var itemScript = Substitute.For<IItemScript>();
                itemScript.CanStackItem(Arg.Any<IItem>(), Arg.Any<IItem>(), Arg.Any<bool>()).Returns(info =>
                {
                    var arg1 = (IItem)info[0];
                    var arg2 = (IItem)info[1];
                    var alwaysStack = (bool)info[2];
                    if (alwaysStack) return arg1.Id == arg2.Id;
                    return (arg1.ItemDefinition.Stackable || arg1.ItemDefinition.Noted) && arg1.Id == arg2.Id;
                });
                ItemScript = itemScript;

                EquipmentDefinition = Substitute.For<IEquipmentDefinition>();
                EquipmentScript = Substitute.For<IEquipmentScript>();
            }

            public IItem Clone() => new TestItem(Id, 1, ItemDefinition.Stackable, ItemDefinition.Noted);
            public IItem Clone(int newCount) => new TestItem(Id, newCount, ItemDefinition.Stackable, ItemDefinition.Noted);

            public bool Equals(IItem other, bool ignoreCount = false)
            {
                if (other is null) return false;
                if (ignoreCount)
                {
                    return Id == other.Id;
                }
                return Id == other.Id && Count == other.Count;
            }

            public string? SerializeExtraData() => null;
        }

        private IItem CreateItem(int id, int count, bool stackable = false, bool noted = false)
        {
            return new TestItem(id, count, stackable, noted);
        }

        [TestMethod]
        public void Add_StackableItemToFullContainer_Fails()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 1);
            var existingItem = CreateItem(1, 1, stackable: true);
            container.Add(existingItem);
            var newItem = CreateItem(2, 1, stackable: true);

            // Act
            var result = container.Add(newItem);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Add_ItemToSpecificSlot_Success()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var item = CreateItem(1, 1);

            // Act
            var result = container.Add(5, item);

            // Assert
            Assert.IsTrue(result);
            Assert.IsNotNull(container[5]);
            Assert.IsTrue(item.Equals(container[5]));
        }

        [TestMethod]
        public void Add_ItemToOccupiedSlot_Fails()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var existingItem = CreateItem(1, 1);
            container.Add(0, existingItem);
            var newItem = CreateItem(2, 1);

            // Act
            var result = container.Add(0, newItem);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Remove_NonExistentItem_ReturnsZero()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var item = CreateItem(1, 1);

            // Act
            var removedCount = container.Remove(item);

            // Assert
            Assert.AreEqual(0, removedCount);
        }

        [TestMethod]
        public void Remove_FromSpecificSlot_Success()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var item = CreateItem(1, 1);
            container.Add(5, item);

            // Act
            var removedCount = container.Remove(item, 5);

            // Assert
            Assert.AreEqual(1, removedCount);
            Assert.IsNull(container[5]);
        }

        [TestMethod]
        public void Move_ToSameSlot_NoChange()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var item = CreateItem(1, 1);
            container.Add(0, item);

            // Act
            container.Move(0, 0);

            // Assert
            Assert.IsNotNull(container[0]);
            Assert.IsTrue(item.Equals(container[0]));
        }

        [TestMethod]
        public void Swap_WithEmptySlot_Success()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var item = CreateItem(1, 1);
            container.Add(0, item);

            // Act
            container.Swap(0, 1);

            // Assert
            Assert.IsNull(container[0]);
            Assert.IsNotNull(container[1]);
            Assert.IsTrue(item.Equals(container[1]));
        }

        [TestMethod]
        public void GetById_ItemExists_ReturnsItem()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var item = CreateItem(1, 1);
            container.Add(item);

            // Act
            var result = container.GetById(1);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(item.Equals(result));
        }

        [TestMethod]
        public void GetCountById_ItemExists_ReturnsCorrectCount()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            container.Add(CreateItem(1, 5, stackable: true));
            container.Add(CreateItem(1, 3, stackable: true));

            // Act
            var result = container.GetCountById(1);

            // Assert
            Assert.AreEqual(8, result);
        }

        [TestMethod]
        public void Contains_ItemExists_ReturnsTrue()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var item = CreateItem(1, 1);
            container.Add(item);

            // Act
            var result = container.Contains(1);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void Clear_Container_Success()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            container.Add(CreateItem(1, 1));
            container.Add(CreateItem(2, 1));

            // Act
            container.Clear(true);

            // Assert
            Assert.AreEqual(0, container.TakenSlots);
        }
    }
}