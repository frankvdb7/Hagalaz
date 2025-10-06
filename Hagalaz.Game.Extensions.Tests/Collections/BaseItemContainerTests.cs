using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Items;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Collections.Generic;

namespace Hagalaz.Game.Extensions.Tests.Collections
{
    [TestClass]
    public class BaseItemContainerTests
    {
        private class TestItemContainer : BaseItemContainer
        {
            public TestItemContainer(StorageType type, int capacity) : base(type, capacity) { }

            public override void OnUpdate(HashSet<int> slots = null)
            {
                // No-op for testing
            }
        }

        private IItem CreateMockItem(int id, int count, bool stackable = false, bool noted = false)
        {
            var item = Substitute.For<IItem>();
            var definition = Substitute.For<IItemDefinition>();
            var script = Substitute.For<IItemScript>();

            item.Id.Returns(id);
            var currentCount = count;
            item.Count.Returns(ci => currentCount);
            item.When(i => i.Count = Arg.Any<int>()).Do(ci => currentCount = ci.Arg<int>());


            item.ItemDefinition.Returns(definition);
            item.ItemScript.Returns(script);

            definition.Stackable.Returns(stackable);
            definition.Noted.Returns(noted);

            script.CanStackItem(Arg.Any<IItem>(), Arg.Any<IItem>(), Arg.Any<bool>())
                .Returns(args => {
                    var item1 = (IItem)args[0];
                    var item2 = (IItem)args[1];
                    var alwaysStack = (bool)args[2];
                    if (item1.Id != item2.Id) return false;
                    return alwaysStack || item1.ItemDefinition.Stackable || item1.ItemDefinition.Noted || item2.ItemDefinition.Stackable || item2.ItemDefinition.Noted;
                });

            item.Equals(Arg.Any<IItem>()).Returns(args => {
                var other = (IItem)args[0];
                return other != null && other.Id == id;
            });

            item.Clone().Returns(ci => {
                 var newItem = CreateMockItem(id, currentCount, stackable, noted);
                 return newItem;
            });

            return item;
        }

        [TestMethod]
        public void Add_SingleNonStackableItem_ShouldSucceed()
        {
            // Arrange
            var container = new TestItemContainer(StorageType.Normal, 10);
            var item = CreateMockItem(1, 1);

            // Act
            var result = container.Add(item);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(1, container.TakenSlots);
            Assert.AreEqual(1, container.GetCountById(1));
        }

        [TestMethod]
        public void Add_StackableItemToEmptyContainer_ShouldSucceed()
        {
            // Arrange
            var container = new TestItemContainer(StorageType.Normal, 10);
            var item = CreateMockItem(1, 10, stackable: true);

            // Act
            var result = container.Add(item);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(1, container.TakenSlots);
            Assert.AreEqual(10, container.GetCountById(1));
        }

        [TestMethod]
        public void Add_StackableItemToExistingStack_ShouldSucceed()
        {
            // Arrange
            var container = new TestItemContainer(StorageType.Normal, 10);
            var existingItem = CreateMockItem(1, 10, stackable: true);
            container.Add(existingItem);

            var newItem = CreateMockItem(1, 5, stackable: true);

            // Act
            var result = container.Add(newItem);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(1, container.TakenSlots);
            Assert.AreEqual(15, existingItem.Count);
        }

        [TestMethod]
        public void Add_ItemToFullContainer_ShouldFail()
        {
            // Arrange
            var container = new TestItemContainer(StorageType.Normal, 1);
            var existingItem = CreateMockItem(1, 1);
            container.Add(existingItem);

            var newItem = CreateMockItem(2, 1);

            // Act
            var result = container.Add(newItem);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Remove_ExistingItem_ShouldSucceed()
        {
            // Arrange
            var container = new TestItemContainer(StorageType.Normal, 10);
            var item = CreateMockItem(1, 5, stackable: true);
            container.Add(item);

            // Act
            var removedCount = container.Remove(item);

            // Assert
            Assert.AreEqual(5, removedCount);
            Assert.AreEqual(0, container.TakenSlots);
        }

        [TestMethod]
        public void HasSpaceFor_WithStackableItemAndExistingStack_ShouldReturnTrue()
        {
            // Arrange
            var container = new TestItemContainer(StorageType.Normal, 10);
            var existingItem = CreateMockItem(1, 10, stackable: true);
            container.Add(existingItem);
            var newItem = CreateMockItem(1, 5, stackable: true);

            // Act
            var result = container.HasSpaceFor(newItem);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void Swap_TwoItems_ShouldSwapTheirSlots()
        {
            // Arrange
            var container = new TestItemContainer(StorageType.Normal, 10);
            var item1 = CreateMockItem(1, 1);
            var item2 = CreateMockItem(2, 1);
            container.Add(0, item1);
            container.Add(1, item2);

            // Act
            container.Swap(0, 1);

            // Assert
            Assert.AreSame(item2, container[0]);
            Assert.AreSame(item1, container[1]);
        }
    }
}