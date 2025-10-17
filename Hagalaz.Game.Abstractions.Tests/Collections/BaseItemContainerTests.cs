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

        [TestMethod]
        public void AddAndRemoveFrom_TransferAll_Success()
        {
            // Arrange
            var source = new TestableItemContainer(StorageType.Normal, 5);
            var destination = new TestableItemContainer(StorageType.Normal, 5);
            var item = CreateItem(1, 1);
            source.Add(item);

            // Act
            destination.AddAndRemoveFrom(source);

            // Assert
            Assert.AreEqual(0, source.TakenSlots);
            Assert.AreEqual(1, destination.TakenSlots);
            Assert.IsTrue(item.Equals(destination[0]));
        }

        [TestMethod]
        public void AddAndRemoveFrom_TransferPartialStack_Success()
        {
            // Arrange
            var source = new TestableItemContainer(StorageType.Normal, 5);
            var destination = new TestableItemContainer(StorageType.Normal, 5);
            var item = CreateItem(1, 10, stackable: true);
            source.Add(item);

            var itemToTransfer = CreateItem(1, 3, stackable: true);

            // Act
            destination.Add(itemToTransfer);
            source.Remove(itemToTransfer, 0);

            // Assert
            Assert.AreEqual(1, source.TakenSlots);
            Assert.AreEqual(7, source[0].Count);
            Assert.AreEqual(1, destination.TakenSlots);
            Assert.AreEqual(3, destination[0].Count);
        }

        [TestMethod]
        public void Remove_NonStackable_RemovesOneInstance()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var item = CreateItem(1, 1, stackable: false); // Not stackable
            container.Add(item);
            container.Add(item.Clone());

            // Act
            var removedCount = container.Remove(item);

            // Assert
            Assert.AreEqual(1, removedCount);
            Assert.AreEqual(1, container.TakenSlots);
        }

        [TestMethod]
        public void Clear_RemovesAllItems()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            container.Add(CreateItem(1, 1));
            container.Add(CreateItem(2, 5, stackable: true));
            container.Add(CreateItem(3, 1));

            // Act
            container.Clear(true);

            // Assert
            Assert.AreEqual(0, container.TakenSlots);
            Assert.AreEqual(10, container.FreeSlots);
        }

        [TestMethod]
        public void AddRange_WithMultipleItems_Success()
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
            var result = container.AddRange(items);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(3, container.TakenSlots);
            Assert.AreEqual(7, container.FreeSlots);
        }

        [TestMethod]
        public void AddRange_FullContainer_Fails()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 2);
            var items = new[]
            {
                CreateItem(1, 1),
                CreateItem(2, 1),
                CreateItem(3, 1)
            };

            // Act
            var result = container.AddRange(items);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(0, container.TakenSlots);
        }

        [TestMethod]
        public void HasSpaceForRange_WithEnoughSpace_ReturnsTrue()
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
            var result = container.HasSpaceForRange(items);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void HasSpaceForRange_WithNotEnoughSpace_ReturnsFalse()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 2);
            var items = new[]
            {
                CreateItem(1, 1),
                CreateItem(2, 1),
                CreateItem(3, 1)
            };

            // Act
            var result = container.HasSpaceForRange(items);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Remove_MoreThanInStack_RemovesAll()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var item = CreateItem(1, 5, stackable: true);
            container.Add(item);
            var itemToRemove = CreateItem(1, 10, stackable: true);

            // Act
            var removedCount = container.Remove(itemToRemove);

            // Assert
            Assert.AreEqual(5, removedCount);
            Assert.AreEqual(0, container.TakenSlots);
        }

        [TestMethod]
        public void Remove_ItemNotInContainer_ReturnsZero()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var item = CreateItem(1, 1);
            container.Add(item);
            var itemToRemove = CreateItem(2, 1);

            // Act
            var removedCount = container.Remove(itemToRemove);

            // Assert
            Assert.AreEqual(0, removedCount);
            Assert.AreEqual(1, container.TakenSlots);
        }

        [TestMethod]
        public void AddRange_WithStackableItems_StacksCorrectly()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 5);
            container.Add(CreateItem(1, 5, stackable: true));
            var itemsToAdd = new[] { CreateItem(1, 3, stackable: true), CreateItem(2, 2, stackable: true) };

            // Act
            var result = container.AddRange(itemsToAdd);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(2, container.TakenSlots);
            Assert.AreEqual(8, container[0].Count);
            Assert.AreEqual(2, container[1].Count);
        }

        [TestMethod]
        public void AddRange_AtomicOperation_FailsIfOneItemDoesNotFit()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 2);
            container.Add(CreateItem(1, 1));
            var itemsToAdd = new[] { CreateItem(2, 1), CreateItem(3, 1) }; // Not enough space for item 3

            // Act
            var result = container.AddRange(itemsToAdd);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(1, container.TakenSlots); // Should not have added any items
            Assert.AreEqual(1, container[0].Id);
        }

        [TestMethod]
        public void HasSpaceFor_StackableItemWithNoExistingStack_ReturnsTrueIfFreeSlotAvailable()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 1);
            var item = CreateItem(1, 1, stackable: true);

            // Act
            var result = container.HasSpaceFor(item);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void HasSpaceFor_NonStackableItems_ReturnsFalseWhenNotEnoughSlots()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 2);
            container.Add(CreateItem(1, 1));
            var item = CreateItem(2, 2, stackable: false); // Requires 2 slots

            // Act
            var result = container.HasSpaceFor(item);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Remove_NonStackableItemsFromMultipleSlots_Success()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var item = CreateItem(1, 1, stackable: false);
            container.Add(item.Clone());
            container.Add(item.Clone());
            container.Add(item.Clone());
            var itemToRemove = CreateItem(1, 2, stackable: false);

            // Act
            var removedCount = container.Remove(itemToRemove);

            // Assert
            Assert.AreEqual(2, removedCount);
            Assert.AreEqual(1, container.TakenSlots);
        }
    }
}