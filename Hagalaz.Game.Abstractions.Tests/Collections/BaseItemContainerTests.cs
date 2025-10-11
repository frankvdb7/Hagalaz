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
    public class BaseItemContainerTests
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
        public void Add_SingleItem_Success()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var item = CreateItem(1, 1);

            // Act
            var result = container.Add(item);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(1, container.TakenSlots);
            Assert.AreEqual(9, container.FreeSlots);
            Assert.IsNotNull(container[0]);
            Assert.IsTrue(item.Equals(container[0]));
        }

        [TestMethod]
        public void Add_StackableItem_StacksWithExisting()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var existingItem = CreateItem(1, 5, stackable: true);
            container.Add(existingItem);

            var newItem = CreateItem(1, 3, stackable: true);

            // Act
            var result = container.Add(newItem);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(1, container.TakenSlots);
            Assert.AreEqual(9, container.FreeSlots);
            Assert.AreEqual(8, container[0].Count);
        }

        [TestMethod]
        public void Add_FullContainer_Fails()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 1);
            var existingItem = CreateItem(1, 1);
            container.Add(existingItem);

            var newItem = CreateItem(2, 1);

            // Act
            var result = container.Add(newItem);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(1, container.TakenSlots);
        }

        [TestMethod]
        public void Add_MultipleItems_Success()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var items = new[]
            {
                CreateItem(1, 1),
                CreateItem(2, 1),
                CreateItem(3, 1)
            };

            // Act
            foreach (var item in items)
            {
                container.Add(item);
            }

            // Assert
            Assert.AreEqual(3, container.TakenSlots);
            Assert.AreEqual(7, container.FreeSlots);
        }

        [TestMethod]
        public void Remove_SingleItem_Success()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var item = CreateItem(1, 1);
            container.Add(item);

            // Act
            var removedCount = container.Remove(item);

            // Assert
            Assert.AreEqual(1, removedCount);
            Assert.AreEqual(0, container.TakenSlots);
            Assert.AreEqual(10, container.FreeSlots);
        }

        [TestMethod]
        public void Remove_PartialStack_Success()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var existingItem = CreateItem(1, 10, stackable: true);
            container.Add(existingItem);

            var itemToRemove = CreateItem(1, 3, stackable: true);

            // Act
            var removedCount = container.Remove(itemToRemove);

            // Assert
            Assert.AreEqual(3, removedCount);
            Assert.AreEqual(1, container.TakenSlots);
            Assert.AreEqual(7, container[0].Count);
        }

        [TestMethod]
        public void Remove_FullStack_Success()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var item = CreateItem(1, 5, stackable: true);
            container.Add(item);

            // Act
            var removedCount = container.Remove(item);

            // Assert
            Assert.AreEqual(5, removedCount);
            Assert.AreEqual(0, container.TakenSlots);
        }

        [TestMethod]
        public void Swap_TwoItems_Success()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var item1 = CreateItem(1, 1);
            var item2 = CreateItem(2, 1);
            container.Add(item1);
            container.Add(item2);

            // Act
            container.Swap(0, 1);

            // Assert
            Assert.IsNotNull(container[0]);
            Assert.IsNotNull(container[1]);
            Assert.IsTrue(item2.Equals(container[0]));
            Assert.IsTrue(item1.Equals(container[1]));
        }

        [TestMethod]
        public void Move_Item_Success()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var item1 = CreateItem(1, 1);
            var item2 = CreateItem(2, 1);
            container.Add(item1);
            container.Add(item2);

            // Act
            container.Move(0, 5);

            // Assert
            Assert.IsNull(container[0]);
            Assert.IsNotNull(container[5]);
            Assert.IsTrue(item1.Equals(container[5]));
        }
    }
}