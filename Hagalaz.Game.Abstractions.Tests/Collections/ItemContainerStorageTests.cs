using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Cache.Abstractions.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Tests.Collections
{
    [TestClass]
    public class ItemContainerStorageTests
    {
        private class TestableItemContainer
        {
            public ItemContainer Items { get; }
            public int UpdateCount { get; private set; }
            public bool ThrowOnPublication { get; set; }
            public Action<HashSet<int>?>? PublicationHandler { get; set; }

            public TestableItemContainer(StorageType type, int capacity, int countToResetTo = -1)
            {
                Items = new ItemContainer(type, capacity, OnUpdate, countToResetTo);
            }

            public TestableItemContainer(StorageType type, IEnumerable<IItem> items, int capacity)
            {
                Items = new ItemContainer(type, items, capacity);
            }
            public void SetItems(IItem[] items, bool update) { Items.ReplaceState(items); if (update) OnUpdate(); }
            public void OnUpdate(HashSet<int>? slots = null)
            {
                UpdateCount++;
                PublicationHandler?.Invoke(slots);
                if (ThrowOnPublication)
                {
                    throw new InvalidOperationException("Controlled publication failure.");
                }
            }
        }

        [TestMethod]
        public void TryRemoveExact_NullItem_ThrowsArgumentNullException()
        {
            var container = new TestableItemContainer(StorageType.Normal, 2);

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                container.Items.TryRemoveExact(null!, -1));
        }

        [TestMethod]
        public void TryRemoveExact_InsufficientQuantityLeavesContainerUnchanged()
        {
            var container = new TestableItemContainer(StorageType.Normal, 2);
            var item = CreateItem(1, 2, stackable: true);
            Assert.IsTrue(container.Items.Add(0, item));
            var updates = container.UpdateCount;
            var enumerator = container.Items.GetEnumerator();

            Assert.IsFalse(container.Items.TryRemoveExact(CreateItem(1, 3, stackable: true), 0));

            Assert.AreSame(item, container.Items[0]);
            Assert.AreEqual(2, item.Count);
            Assert.AreEqual(updates, container.UpdateCount);
            Assert.IsTrue(enumerator.MoveNext());

            container.PublicationHandler = _ => Assert.IsNull(container.Items[0]);
            Assert.IsTrue(container.Items.TryRemoveExact(CreateItem(1, 2, stackable: true), 0));
            Assert.IsNull(container.Items[0]);
            Assert.AreEqual(updates + 1, container.UpdateCount);
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
            private readonly long[] _extraData;
            private readonly Action? _onClone;
            public long[] ExtraData => _extraData;

            public TestItem(int id, int count, bool stackable = false, bool noted = false, long[]? extraData = null, Action? onClone = null)
            {
                Id = id;
                Count = count;
                _extraData = extraData ?? Array.Empty<long>();
                _onClone = onClone;
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
                    return arg1.Id == arg2.Id &&
                           arg1.ExtraData.AsSpan().SequenceEqual(arg2.ExtraData) &&
                           (alwaysStack || arg1.ItemDefinition.Stackable || arg1.ItemDefinition.Noted);
                });
                ItemScript = itemScript;

                EquipmentDefinition = Substitute.For<IEquipmentDefinition>();
                EquipmentScript = Substitute.For<IEquipmentScript>();
            }

            public IItem Clone() => Clone(1);
            public IItem Clone(int newCount)
            {
                _onClone?.Invoke();
                return new TestItem(Id, newCount, ItemDefinition.Stackable, ItemDefinition.Noted, (long[])_extraData.Clone(), _onClone);
            }

            public bool Equals(IItem other, bool ignoreCount = true) =>
                other is not null &&
                Id == other.Id &&
                _extraData.AsSpan().SequenceEqual(other.ExtraData) &&
                (ignoreCount || Count == other.Count);

            public string? SerializeExtraData() => _extraData.Length == 0 ? null : string.Join(",", _extraData);
        }

        private IItem CreateItem(int id, int count, bool stackable = false, bool noted = false, long[]? extraData = null, Action? onClone = null)
        {
            return new TestItem(id, count, stackable, noted, extraData, onClone);
        }

        [TestMethod]
        public void Add_SingleItem_Success()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var item = CreateItem(1, 1);

            // Act
            var result = container.Items.Add(item);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(1, container.Items.TakenSlots);
            Assert.AreEqual(9, container.Items.FreeSlots);
            Assert.IsNotNull(container.Items[0]);
            Assert.IsTrue(item.Equals(container.Items[0]));
        }

        [TestMethod]
        public void Add_StackableItem_StacksWithExisting()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var existingItem = CreateItem(1, 5, stackable: true);
            container.Items.Add(existingItem);

            var newItem = CreateItem(1, 3, stackable: true);

            // Act
            var result = container.Items.Add(newItem);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(1, container.Items.TakenSlots);
            Assert.AreEqual(9, container.Items.FreeSlots);
            Assert.AreEqual(8, container.Items[0].Count);
        }

        [TestMethod]
        public void Add_FullContainer_Fails()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 1);
            var existingItem = CreateItem(1, 1);
            container.Items.Add(existingItem);

            var newItem = CreateItem(2, 1);

            // Act
            var result = container.Items.Add(newItem);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(1, container.Items.TakenSlots);
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
                container.Items.Add(item);
            }

            // Assert
            Assert.AreEqual(3, container.Items.TakenSlots);
            Assert.AreEqual(7, container.Items.FreeSlots);
        }

        [TestMethod]
        public void Remove_SingleItem_Success()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var item = CreateItem(1, 1);
            container.Items.Add(item);

            // Act
            var removedCount = container.Items.Remove(item);

            // Assert
            Assert.AreEqual(1, removedCount);
            Assert.AreEqual(0, container.Items.TakenSlots);
            Assert.AreEqual(10, container.Items.FreeSlots);
        }

        [TestMethod]
        public void Remove_PartialStack_Success()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var existingItem = CreateItem(1, 10, stackable: true);
            container.Items.Add(existingItem);

            var itemToRemove = CreateItem(1, 3, stackable: true);

            // Act
            var removedCount = container.Items.Remove(itemToRemove);

            // Assert
            Assert.AreEqual(3, removedCount);
            Assert.AreEqual(1, container.Items.TakenSlots);
            Assert.AreEqual(7, container.Items[0].Count);
        }

        [TestMethod]
        public void Remove_FullStack_Success()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var item = CreateItem(1, 5, stackable: true);
            container.Items.Add(item);

            // Act
            var removedCount = container.Items.Remove(item);

            // Assert
            Assert.AreEqual(5, removedCount);
            Assert.AreEqual(0, container.Items.TakenSlots);
        }

        [TestMethod]
        public void Swap_TwoItems_Success()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var item1 = CreateItem(1, 1);
            var item2 = CreateItem(2, 1);
            container.Items.Add(item1);
            container.Items.Add(item2);

            // Act
            container.Items.Swap(0, 1);

            // Assert
            Assert.IsNotNull(container.Items[0]);
            Assert.IsNotNull(container.Items[1]);
            Assert.IsTrue(item2.Equals(container.Items[0]));
            Assert.IsTrue(item1.Equals(container.Items[1]));
        }

        [TestMethod]
        public void Move_Item_Success()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var item1 = CreateItem(1, 1);
            var item2 = CreateItem(2, 1);
            container.Items.Add(item1);
            container.Items.Add(item2);

            // Act
            container.Items.Move(0, 5);

            // Assert
            Assert.IsNull(container.Items[0]);
            Assert.IsNotNull(container.Items[5]);
            Assert.IsTrue(item1.Equals(container.Items[5]));
        }

        [TestMethod]
        public void Remove_PartialStack_UpdatesOnlyTheRequestedQuantity()
        {
            // Arrange
            var source = new TestableItemContainer(StorageType.Normal, 5);
            var destination = new TestableItemContainer(StorageType.Normal, 5);
            var item = CreateItem(1, 10, stackable: true);
            source.Items.Add(item);

            var itemToTransfer = CreateItem(1, 3, stackable: true);

            // Act
            destination.Items.Add(itemToTransfer);
            source.Items.Remove(itemToTransfer, 0);

            // Assert
            Assert.AreEqual(1, source.Items.TakenSlots);
            Assert.AreEqual(7, source.Items[0].Count);
            Assert.AreEqual(1, destination.Items.TakenSlots);
            Assert.AreEqual(3, destination.Items[0].Count);
        }

        [TestMethod]
        public void Remove_NonStackable_RemovesOneInstance()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var item = CreateItem(1, 1, stackable: false); // Not stackable
            container.Items.Add(item);
            container.Items.Add(item.Clone());

            // Act
            var removedCount = container.Items.Remove(item);

            // Assert
            Assert.AreEqual(1, removedCount);
            Assert.AreEqual(1, container.Items.TakenSlots);
        }

        [TestMethod]
        public void Clear_RemovesAllItems()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            container.Items.Add(CreateItem(1, 1));
            container.Items.Add(CreateItem(2, 5, stackable: true));
            container.Items.Add(CreateItem(3, 1));

            // Act
            container.Items.Clear(true);

            // Assert
            Assert.AreEqual(0, container.Items.TakenSlots);
            Assert.AreEqual(10, container.Items.FreeSlots);
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
            var result = container.Items.AddRange(items);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(3, container.Items.TakenSlots);
            Assert.AreEqual(7, container.Items.FreeSlots);
        }

        [TestMethod]
        public void AddRange_WhenNotEnoughSpace_PreservesExistingItemsAndInsertsNone()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 2);
            var existingItem = CreateItem(10, 1);
            Assert.IsTrue(container.Items.Add(existingItem));
            var existingStoredItem = container.Items[0]!;
            var items = new[] { CreateItem(1, 1), CreateItem(2, 1) };

            // Act
            var result = container.Items.AddRange(items);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(1, container.Items.TakenSlots);
            Assert.AreSame(existingStoredItem, container.Items[0]);
            Assert.AreEqual(1, existingStoredItem.Count);
            Assert.AreEqual(0, container.Items.GetCountById(1));
            Assert.AreEqual(0, container.Items.GetCountById(2));
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
            var result = container.Items.HasSpaceForRange(items);

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
            var result = container.Items.HasSpaceForRange(items);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Remove_MoreThanInStack_RemovesAll()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var item = CreateItem(1, 5, stackable: true);
            container.Items.Add(item);
            var itemToRemove = CreateItem(1, 10, stackable: true);

            // Act
            var removedCount = container.Items.Remove(itemToRemove);

            // Assert
            Assert.AreEqual(5, removedCount);
            Assert.AreEqual(0, container.Items.TakenSlots);
        }

        [TestMethod]
        public void Remove_MultipleMatchingStacks_PreservesLegacyPerStackBehavior()
        {
            var container = new TestableItemContainer(
                StorageType.Normal,
                [CreateItem(1, 3, stackable: true), CreateItem(1, 4, stackable: true)],
                10);

            var removedCount = container.Items.Remove(CreateItem(1, 2, stackable: true));

            Assert.AreEqual(2, removedCount);
            Assert.AreEqual(1, container.Items[0].Count);
            Assert.AreEqual(2, container.Items[1].Count);
        }

        [TestMethod]
        public void Remove_ItemNotInContainer_ReturnsZero()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var item = CreateItem(1, 1);
            container.Items.Add(item);
            var itemToRemove = CreateItem(2, 1);

            // Act
            var removedCount = container.Items.Remove(itemToRemove);

            // Assert
            Assert.AreEqual(0, removedCount);
            Assert.AreEqual(1, container.Items.TakenSlots);
        }

        [TestMethod]
        public void AddRange_WithStackableItems_StacksCorrectly()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 5);
            container.Items.Add(CreateItem(1, 5, stackable: true));
            var itemsToAdd = new[] { CreateItem(1, 3, stackable: true), CreateItem(2, 2, stackable: true) };

            // Act
            var result = container.Items.AddRange(itemsToAdd);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(2, container.Items.TakenSlots);
            Assert.AreEqual(8, container.Items[0].Count);
            Assert.AreEqual(2, container.Items[1].Count);
        }

        [TestMethod]
        public void AddRange_NonStackableCountNeedsMoreSlots_FailsWithoutMutation()
        {
            var container = new TestableItemContainer(StorageType.Normal, 2);
            var existing = CreateItem(1, 1);
            container.Items.Add(existing);

            var result = container.Items.AddRange([CreateItem(2, 2)]);

            Assert.IsFalse(result);
            Assert.IsFalse(container.Items.HasSpaceForRange([CreateItem(2, 2)]));
            Assert.AreEqual(1, container.Items.TakenSlots);
            Assert.AreEqual(existing.Id, container.Items[0].Id);
            Assert.AreEqual(1, container.Items[0].Count);
        }

        [TestMethod]
        public void AddRange_NonStackableCountWithEnoughSlots_UsesOneSlotPerInstance()
        {
            var container = new TestableItemContainer(StorageType.Normal, 2);

            var result = container.Items.AddRange([CreateItem(1, 2)]);

            Assert.IsTrue(result);
            Assert.AreEqual(2, container.Items.TakenSlots);
            Assert.AreEqual(1, container.Items[0].Count);
            Assert.AreEqual(1, container.Items[1].Count);
        }

        [TestMethod]
        public void AddRange_StackableItemsWithNoExistingStack_ShareOneCreatedStack()
        {
            var container = new TestableItemContainer(StorageType.Normal, 1);
            var first = CreateItem(1, 3, stackable: true);
            var second = CreateItem(1, 4, stackable: true);
            var items = new[] { first, second };

            Assert.IsTrue(container.Items.HasSpaceForRange(items));
            Assert.IsTrue(container.Items.AddRange(items));
            Assert.AreEqual(1, container.Items.TakenSlots);
            Assert.AreSame(first, container.Items[0]);
            Assert.AreEqual(7, container.Items[0].Count);
            Assert.AreEqual(4, second.Count);
        }

        [TestMethod]
        public void AddRange_PreservesExistingStackAndNewStackIdentitiesAndChangedSlots()
        {
            var storage = new ItemContainerStorage(StorageType.AlwaysStack, 2);
            var existing = CreateItem(1, 5, stackable: true);
            var newStack = CreateItem(2, 4, stackable: true);
            Assert.IsTrue(storage.TryAdd(existing, out _));

            Assert.IsTrue(storage.TryAddRange([CreateItem(1, 3, stackable: true), newStack], out var changedSlots));

            Assert.AreSame(existing, storage[0]);
            Assert.AreEqual(8, existing.Count);
            Assert.AreSame(newStack, storage[1]);
            Assert.AreEqual(4, newStack.Count);
            CollectionAssert.AreEquivalent(new[] { 0, 1 }, changedSlots.ToArray());
        }

        [TestMethod]
        public void TryAddRange_EmptyRangePreservesEnumeratorAndStorage()
        {
            AssertNoOpAddRangePreservesStorage(Array.Empty<IItem?>());
        }

        [TestMethod]
        public void TryAddRange_AllNullItems_DoesNotAdvanceRevision()
        {
            AssertNoOpAddRangePreservesStorage(new IItem?[] { null, null });
        }

        private void AssertNoOpAddRangePreservesStorage(IItem?[] incoming)
        {
            var storage = new ItemContainerStorage(StorageType.Normal, 2);
            var existing = CreateItem(1, 3, stackable: true);
            Assert.IsTrue(storage.TryAdd(existing, out _));
            var enumerator = storage.GetEnumerator();
            var revision = storage.MutationRevision;

            var result = storage.TryAddRange(incoming, out var changedSlots);

            Assert.IsTrue(result);
            Assert.IsEmpty(changedSlots);
            Assert.AreEqual(revision, storage.MutationRevision);
            Assert.AreSame(existing, storage[0]);
            Assert.AreEqual(3, existing.Count);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreSame(existing, enumerator.Current);
        }

        [TestMethod]
        public void TryAddRange_RealMutationAdvancesRevisionAndInvalidatesEnumerator()
        {
            var storage = new ItemContainerStorage(StorageType.Normal, 2);
            var enumerator = storage.GetEnumerator();
            var revision = storage.MutationRevision;

            Assert.IsTrue(storage.TryAddRange([CreateItem(1, 1)], out var changedSlots));

            Assert.IsNotEmpty(changedSlots);
            Assert.AreEqual(revision + 1, storage.MutationRevision);
            Assert.ThrowsExactly<InvalidOperationException>(() => enumerator.MoveNext());
        }

        [TestMethod]
        public void ItemContainer_AddRangeEmptyRangeDoesNotPublishOrInvalidateEnumerator()
        {
            var publications = 0;
            var container = new ItemContainer(StorageType.Normal, 2, _ => publications++);
            var existing = CreateItem(1, 3, stackable: true);
            Assert.IsTrue(container.Add(existing));
            publications = 0;
            var enumerator = container.GetEnumerator();

            var result = container.AddRange(Array.Empty<IItem?>());

            Assert.IsTrue(result);
            Assert.AreEqual(0, publications);
            Assert.AreSame(existing, container[0]);
            Assert.AreEqual(3, existing.Count);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreSame(existing, enumerator.Current);
        }

        [TestMethod]
        public void AddRange_NonStackableItems_ClonesEachInstanceIntoOneCountSlots()
        {
            var storage = new ItemContainerStorage(StorageType.Normal, 2);
            var incoming = CreateItem(1, 2);

            Assert.IsTrue(storage.TryAddRange([incoming], out var changedSlots));

            Assert.AreEqual(1, storage[0]!.Count);
            Assert.AreEqual(1, storage[1]!.Count);
            Assert.AreNotSame(incoming, storage[0]);
            Assert.AreNotSame(incoming, storage[1]);
            Assert.AreNotSame(storage[0], storage[1]);
            Assert.AreEqual(2, incoming.Count);
            CollectionAssert.AreEquivalent(new[] { 0, 1 }, changedSlots.ToArray());
        }

        [TestMethod]
        public void AddRange_CombinedStackOverflow_FailsWithoutMutation()
        {
            var storage = new ItemContainerStorage(StorageType.Normal, 1);
            var existing = CreateItem(1, int.MaxValue - 1, stackable: true);
            Assert.IsTrue(storage.TryAdd(existing, out _));
            var enumerator = storage.GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());
            var incoming = CreateItem(1, 2, stackable: true);

            var result = storage.TryAddRange([incoming], out var changedSlots);

            Assert.IsFalse(result);
            Assert.IsFalse(storage.HasSpaceForRange([incoming]));
            Assert.IsEmpty(changedSlots);
            Assert.AreSame(existing, storage[0]);
            Assert.AreEqual(int.MaxValue - 1, existing.Count);
            Assert.AreEqual(2, incoming.Count);
            Assert.IsFalse(enumerator.MoveNext());
        }

        [TestMethod]
        public void AddRange_LaterItemDoesNotFit_FailsWithoutRetainingEarlierItems()
        {
            var container = new TestableItemContainer(StorageType.Normal, 2);
            var items = new[] { CreateItem(1, 1), CreateItem(2, 2) };

            Assert.IsFalse(container.Items.HasSpaceForRange(items));
            Assert.IsFalse(container.Items.AddRange(items));
            Assert.AreEqual(0, container.Items.TakenSlots);
            Assert.IsNull(container.Items[0]);
            Assert.IsNull(container.Items[1]);
        }

        [TestMethod]
        public void AddRange_AtomicOperation_FailsIfOneItemDoesNotFit()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 2);
            container.Items.Add(CreateItem(1, 1));
            var itemsToAdd = new[] { CreateItem(2, 1), CreateItem(3, 1) }; // Not enough space for item 3

            // Act
            var result = container.Items.AddRange(itemsToAdd);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(1, container.Items.TakenSlots); // Should not have added any items
            Assert.AreEqual(1, container.Items[0].Id);
        }

        [TestMethod]
        public void HasSpaceFor_StackableItemWithNoExistingStack_ReturnsTrueIfFreeSlotAvailable()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 1);
            var item = CreateItem(1, 1, stackable: true);

            // Act
            var result = container.Items.HasSpaceFor(item);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void HasSpaceFor_NonStackableItems_ReturnsFalseWhenNotEnoughSlots()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 2);
            container.Items.Add(CreateItem(1, 1));
            var item = CreateItem(2, 2, stackable: false); // Requires 2 slots

            // Act
            var result = container.Items.HasSpaceFor(item);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Remove_NonStackableItemsFromMultipleSlots_Success()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var item = CreateItem(1, 1, stackable: false);
            container.Items.Add(item.Clone());
            container.Items.Add(item.Clone());
            container.Items.Add(item.Clone());
            var itemToRemove = CreateItem(1, 2, stackable: false);

            // Act
            var removedCount = container.Items.Remove(itemToRemove);

            // Assert
            Assert.AreEqual(2, removedCount);
            Assert.AreEqual(1, container.Items.TakenSlots);
        }

        [TestMethod]
        public void GetCountById_WithMultipleItems_ReturnsCorrectCount()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            container.Items.Add(CreateItem(1, 5, stackable: true));
            container.Items.Add(CreateItem(1, 1, stackable: false));
            container.Items.Add(CreateItem(1, 1, stackable: false));
            container.Items.Add(CreateItem(2, 3, stackable: true));

            // Act
            var count = container.Items.GetCountById(1);

            // Assert
            Assert.AreEqual(7, count);
        }

        [TestMethod]
        public void AddToSlot_WithEmptySlot_Success()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var item = CreateItem(1, 1);

            // Act
            var result = container.Items.Add(5, item);

            // Assert
            Assert.IsTrue(result);
            Assert.IsNotNull(container.Items[5]);
            Assert.IsTrue(item.Equals(container.Items[5]));
        }

        [TestMethod]
        public void AddToSlot_WithOccupiedSlot_Overwrites()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var existingItem = CreateItem(1, 1);
            container.Items.Add(5, existingItem);

            var newItem = CreateItem(2, 1);

            // Act
            var result = container.Items.Add(5, newItem);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Add_AlwaysStack_StacksNonStackableItems()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.AlwaysStack, 10);
            var item1 = CreateItem(1, 1, stackable: false);
            var item2 = CreateItem(1, 1, stackable: false);

            // Act
            container.Items.Add(item1);
            var result = container.Items.Add(item2);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(1, container.Items.TakenSlots);
            Assert.AreEqual(2, container.Items[0].Count);
        }

        [TestMethod]
        public void Remove_AlwaysStack_RemovesFromStackedNonStackableItems()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.AlwaysStack, 10);
            var item1 = CreateItem(1, 1, stackable: false);
            var item2 = CreateItem(1, 1, stackable: false);
            container.Items.Add(item1);
            container.Items.Add(item2);

            var itemToRemove = CreateItem(1, 1, stackable: false);

            // Act
            var removedCount = container.Items.Remove(itemToRemove);

            // Assert
            Assert.AreEqual(1, removedCount);
            Assert.AreEqual(1, container.Items.TakenSlots);
            Assert.AreEqual(1, container.Items[0].Count);
        }

        [TestMethod]
        public void CanStackItem_WithNotedItems_Stacks()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var item1 = CreateItem(1, 1, noted: true);
            var item2 = CreateItem(1, 1, noted: true);

            // Act
            container.Items.Add(item1);
            var result = container.Items.Add(item2);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(1, container.Items.TakenSlots);
            Assert.AreEqual(2, container.Items[0].Count);
        }

        [TestMethod]
        public void CanStackItem_WithDifferentIds_DoesNotStack()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var item1 = CreateItem(1, 1, stackable: true);
            var item2 = CreateItem(2, 1, stackable: true);

            // Act
            container.Items.Add(item1);
            container.Items.Add(item2);

            // Assert
            Assert.AreEqual(2, container.Items.TakenSlots);
        }

        [TestMethod]
        public void AddRange_WhenPublicationThrows_PropagatesTheFailureForNormalMutation()
        {
            var container = new TestableItemContainer(StorageType.Normal, 10)
            {
                ThrowOnPublication = true
            };

            Assert.ThrowsExactly<InvalidOperationException>(() => container.Items.AddRange([CreateItem(1, 1)]));
            Assert.AreEqual(1, container.Items.TakenSlots);
        }

        [TestMethod]
        public void NormalMutation_UsesTradeSynchronizationBoundary()
        {
            var container = new TestableItemContainer(StorageType.Normal, 10);
            using var started = new ManualResetEventSlim();
            using var proceed = new ManualResetEventSlim();
            using var finished = new ManualResetEventSlim();

            Task mutation = Task.CompletedTask;
            var startedInTime = false;
            var finishedWhileLocked = false;
            container.Items.ExecuteUnderMutationLock(() =>
            {
                mutation = Task.Run(() =>
                {
                    started.Set();
                    proceed.Wait();
                    try
                    {
                        container.Items.Add(CreateItem(1, 1));
                    }
                    finally
                    {
                        finished.Set();
                    }
                });

                startedInTime = started.Wait(TimeSpan.FromSeconds(1));
                proceed.Set();
                finishedWhileLocked = finished.Wait(TimeSpan.FromMilliseconds(100));
            });

            Assert.IsTrue(startedInTime);
            Assert.IsFalse(finishedWhileLocked);
            Assert.IsTrue(finished.Wait(TimeSpan.FromSeconds(1)));
            mutation.GetAwaiter().GetResult();
            Assert.AreEqual(1, container.Items.TakenSlots);
        }

        [TestMethod]
        public void Sort_WithEmptySlots_RemovesGaps()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            container.Items.Add(0, CreateItem(1, 1));
            container.Items.Add(2, CreateItem(2, 1));
            container.Items.Add(5, CreateItem(3, 1));

            // Act
            container.Items.Sort();

            // Assert
            Assert.IsNotNull(container.Items[0]);
            Assert.IsNotNull(container.Items[1]);
            Assert.IsNotNull(container.Items[2]);
            Assert.IsNull(container.Items[3]);
            Assert.AreEqual(3, container.Items.TakenSlots);
        }

        [TestMethod]
        public void Enumerator_CollectionModified_ThrowsException()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            container.Items.Add(CreateItem(1, 1));

            // Act & Assert
            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                foreach (var _ in container.Items)
                {
                    container.Items.Add(CreateItem(2, 1));
                }
            });
        }

        [TestMethod]
        public void Remove_WithCountToResetTo_ResetsItemCount()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10, 1);
            var item = CreateItem(1, 5, stackable: true);
            container.Items.Add(item);

            // Act
            container.Items.Remove(CreateItem(1, 2, stackable: true));

            // Assert
            Assert.AreEqual(1, container.Items.TakenSlots);
            Assert.AreEqual(3, container.Items[0]!.Count);

            // Act
            container.Items.Remove(CreateItem(1, 3, stackable: true));

            // Assert
            Assert.AreEqual(1, container.Items.TakenSlots);
            Assert.AreEqual(1, container.Items[0]!.Count);
        }

        [TestMethod]
        public void Replace_WithNewItem_OverwritesSlot()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            container.Items.Add(CreateItem(1, 1));
            var newItem = CreateItem(2, 5);

            // Act
            container.Items.Replace(0, newItem);

            // Assert
            Assert.IsNotNull(container.Items[0]);
            Assert.AreEqual(2, container.Items[0]!.Id);
            Assert.AreEqual(5, container.Items[0]!.Count);
        }

        [TestMethod]
        public void ReplaceState_WithNewArray_ReplacesAllItems()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            container.Items.Add(CreateItem(1, 1));
            var newItems = new IItem[container.Items.Capacity];
            newItems[0] = CreateItem(10, 1);
            newItems[1] = CreateItem(11, 1);

            // Act
            container.Items.ReplaceState(newItems);

            // Assert
            Assert.AreEqual(2, container.Items.TakenSlots);
            Assert.AreEqual(10, container.Items[0]!.Id);
            Assert.AreEqual(11, container.Items[1]!.Id);
        }

        [TestMethod]
        public void ReplaceState_InvalidatesExistingEnumerator()
        {
            var container = new TestableItemContainer(StorageType.Normal, 2);
            container.Items.Add(CreateItem(1, 1));
            var enumerator = container.Items.GetEnumerator();
            var replacement = new IItem?[container.Items.Capacity];
            replacement[1] = CreateItem(2, 1);

            container.Items.ReplaceState(replacement);

            Assert.ThrowsExactly<InvalidOperationException>(() => enumerator.MoveNext());
            Assert.AreEqual(2, container.Items[1]!.Id);
        }

        [TestMethod]
        public void ReplaceState_WithInvalidLength_RejectsInputAndKeepsCapacitySizedStorage()
        {
            var container = new TestableItemContainer(StorageType.Normal, 10);
            container.Items.Add(CreateItem(1, 1));

            Assert.ThrowsExactly<ArgumentException>(() => container.Items.ReplaceState([CreateItem(2, 1)]));

            Assert.AreEqual(container.Items.Capacity, container.Items.SnapshotItems().Length);
            Assert.AreEqual(1, container.Items[0]!.Id);
        }

        [TestMethod]
        public void ReplaceState_DoesNotRetainCallerOwnedArray()
        {
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var items = new IItem[container.Items.Capacity];
            items[5] = CreateItem(10, 1);
            container.Items.ReplaceState(items);

            items[5] = CreateItem(11, 1);
            items[2] = CreateItem(12, 1);

            Assert.AreEqual(10, container.Items[5]!.Id);
            Assert.IsNull(container.Items[2]);
            Assert.AreEqual(container.Items.Capacity, container.Items.SnapshotItems().Length);
        }

                                        [TestMethod]
        public void Constructor_WithIEnumerable_InitializesCorrectly()
        {
            // Arrange
            var items = new List<IItem> { CreateItem(1, 1), CreateItem(2, 1) };

            // Act
            var container = new TestableItemContainer(StorageType.Normal, items, 10);

            // Assert
            Assert.AreEqual(2, container.Items.TakenSlots);
            Assert.AreEqual(8, container.Items.FreeSlots);
        }

        [TestMethod]
        public void GetById_ItemExists_ReturnsItem()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var item = CreateItem(123, 1);
            container.Items.Add(item);

            // Act
            var foundItem = container.Items.GetById(123);

            // Assert
            Assert.IsNotNull(foundItem);
            Assert.AreEqual(123, foundItem.Id);
        }

        [TestMethod]
        public void GetById_ItemDoesNotExist_ReturnsNull()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);

            // Act
            var foundItem = container.Items.GetById(123);

            // Assert
            Assert.IsNull(foundItem);
        }

        [TestMethod]
        public void GetCount_ItemExists_ReturnsCorrectCount()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            container.Items.Add(CreateItem(1, 5, stackable: true));
            container.Items.Add(CreateItem(1, 1, stackable: false)); // Different item, same ID

            // Act
            var count = container.Items.GetCount(CreateItem(1, 1, stackable: true));

            // Assert
            Assert.AreEqual(6, count);
        }

        [TestMethod]
        public void Contains_ItemExists_ReturnsTrue()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            container.Items.Add(CreateItem(1, 1));

            // Act
            var result = container.Items.Contains(1);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void GetInstanceSlot_ItemExists_ReturnsCorrectSlot()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var item = CreateItem(1, 1);
            container.Items.Add(0, item);

            // Act
            var slot = container.Items.GetInstanceSlot(item);

            // Assert
            Assert.AreEqual(0, slot);
        }

        [TestMethod]
        public void ToArray_ReturnsCopyOfItems()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var item = CreateItem(1, 1);
            container.Items.Add(item);

            // Act
            var array = container.Items.SnapshotItems();

            // Assert
            Assert.HasCount(10, array);
            Assert.IsNotNull(array[0]);
            Assert.AreEqual(1, array[0].Id);
        }

        [TestMethod]
        public void Clear_EmptyContainer_DoesNothing()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);

            // Act
            container.Items.Clear(true);

            // Assert
            Assert.AreEqual(0, container.Items.TakenSlots);
        }

        [TestMethod]
        public void Move_InvalidSlot_DoesNothing()
        {
            // Arrange
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var item = CreateItem(1, 1);
            container.Items.Add(item);

            // Act
            container.Items.Move(0, 10); // 10 is out of bounds

            // Assert
            Assert.IsNotNull(container.Items[0]);
        }

        [TestMethod]
        public void TryTransfer_DestinationFull_LeavesBothContainersUnchanged()
        {
            var source = new TestableItemContainer(StorageType.Normal, 1);
            source.Items.Add(CreateItem(1, 1));
            var destination = new TestableItemContainer(StorageType.Normal, 1);
            destination.Items.Add(CreateItem(2, 1));
            var sourceItem = source.Items[0];
            var destinationItem = destination.Items[0];
            var sourceUpdates = source.UpdateCount;
            var destinationUpdates = destination.UpdateCount;
            var sourceEnumerator = source.Items.GetEnumerator();
            var destinationEnumerator = destination.Items.GetEnumerator();

            Assert.IsFalse(Transfer(source.Items, destination.Items, sourceItem!, 1));

            Assert.AreSame(sourceItem, source.Items[0]);
            Assert.AreSame(destinationItem, destination.Items[0]);
            Assert.AreEqual(sourceUpdates, source.UpdateCount);
            Assert.AreEqual(destinationUpdates, destination.UpdateCount);
            Assert.IsTrue(sourceEnumerator.MoveNext());
            Assert.IsTrue(destinationEnumerator.MoveNext());
        }

        [TestMethod]
        public void TryTransfer_InsufficientSource_LeavesBothContainersUnchanged()
        {
            var source = new TestableItemContainer(StorageType.Normal, 2);
            source.Items.Add(CreateItem(1, 2, stackable: true));
            var destination = new TestableItemContainer(StorageType.Normal, 2);
            var sourceItem = source.Items[0];
            var sourceEnumerator = source.Items.GetEnumerator();
            var sourceUpdates = source.UpdateCount;

            Assert.IsFalse(Transfer(source.Items, destination.Items, CreateItem(1, 3, stackable: true), 3));

            Assert.AreSame(sourceItem, source.Items[0]);
            Assert.AreEqual(2, source.Items[0]!.Count);
            Assert.IsNull(destination.Items[0]);
            Assert.AreEqual(sourceUpdates, source.UpdateCount);
            Assert.AreEqual(0, destination.UpdateCount);
            Assert.IsTrue(sourceEnumerator.MoveNext());
        }

        [TestMethod]
        public void TryTransfer_StackOverflow_LeavesBothContainersUnchanged()
        {
            var source = new TestableItemContainer(StorageType.Normal, 2);
            source.Items.Add(CreateItem(1, 3, stackable: true));
            var destination = new TestableItemContainer(StorageType.Normal, 2);
            destination.Items.Add(CreateItem(1, int.MaxValue - 2, stackable: true));
            var sourceItem = source.Items[0];
            var destinationItem = destination.Items[0];
            var sourceEnumerator = source.Items.GetEnumerator();
            var destinationEnumerator = destination.Items.GetEnumerator();

            Assert.IsFalse(Transfer(source.Items, destination.Items, sourceItem!, 3));

            Assert.AreSame(sourceItem, source.Items[0]);
            Assert.AreSame(destinationItem, destination.Items[0]);
            Assert.AreEqual(3, source.Items[0]!.Count);
            Assert.AreEqual(int.MaxValue - 2, destination.Items[0]!.Count);
            Assert.IsTrue(sourceEnumerator.MoveNext());
            Assert.IsTrue(destinationEnumerator.MoveNext());
        }

        [TestMethod]
        public void TryTransfer_StackableQuantity_TransfersExactCountAndNotifiesAfterCommit()
        {
            var source = new TestableItemContainer(StorageType.Normal, 2);
            source.Items.Add(CreateItem(1, 10, stackable: true));
            var destination = new TestableItemContainer(StorageType.Normal, 2);
            destination.Items.Add(CreateItem(1, 4, stackable: true));
            var sourceItem = source.Items[0];
            var destinationItem = destination.Items[0];
            var sourceObservedCommit = false;
            var destinationObservedCommit = false;
            source.PublicationHandler = _ =>
            {
                Assert.AreEqual(7, destination.Items[0]!.Count);
                Assert.AreEqual(7, source.Items.GetCountById(1));
                sourceObservedCommit = true;
            };
            destination.PublicationHandler = _ =>
            {
                Assert.AreEqual(7, destination.Items[0]!.Count);
                Assert.AreEqual(7, source.Items.GetCountById(1));
                destinationObservedCommit = true;
            };

            Assert.IsTrue(Transfer(source.Items, destination.Items, sourceItem!, 3));

            Assert.AreSame(sourceItem, source.Items[0]);
            Assert.AreSame(destinationItem, destination.Items[0]);
            Assert.AreEqual(7, source.Items[0]!.Count);
            Assert.AreEqual(7, destination.Items[0]!.Count);
            Assert.IsTrue(sourceObservedCommit);
            Assert.IsTrue(destinationObservedCommit);
        }

        [TestMethod]
        public void TryTransfer_NonStackableQuantity_PreservesMovedInstancesAndExtraData()
        {
            var source = new TestableItemContainer(StorageType.Normal, 2);
            source.Items.ReplaceState(
            [
                CreateItem(1, 1, extraData: [17]),
                CreateItem(1, 1, extraData: [17])
            ]);
            var first = source.Items[0];
            var second = source.Items[1];
            var destination = new TestableItemContainer(StorageType.Normal, 2);

            Assert.IsTrue(Transfer(source.Items, destination.Items, first!, 2));

            Assert.IsNull(source.Items[0]);
            Assert.IsNull(source.Items[1]);
            Assert.AreSame(first, destination.Items[0]);
            Assert.AreSame(second, destination.Items[1]);
            CollectionAssert.AreEqual(new long[] { 17 }, destination.Items[0]!.ExtraData);
            CollectionAssert.AreEqual(new long[] { 17 }, destination.Items[1]!.ExtraData);
        }

        [TestMethod]
        public void TryTransfer_SameIdDifferentExtraDataCannotSatisfyExactQuantity()
        {
            var source = new TestableItemContainer(StorageType.Normal, 2);
            source.Items.ReplaceState(
            [
                CreateItem(1, 1, extraData: [17]),
                CreateItem(1, 1, extraData: [29])
            ]);
            var first = source.Items[0];
            var second = source.Items[1];
            var sourceUpdates = source.UpdateCount;
            var destination = new TestableItemContainer(StorageType.Normal, 2);
            var destinationUpdates = destination.UpdateCount;

            Assert.IsFalse(Transfer(source.Items, destination.Items, first!, 2));

            Assert.AreSame(first, source.Items[0]);
            Assert.AreSame(second, source.Items[1]);
            Assert.AreEqual(1, source.Items[0]!.Count);
            Assert.AreEqual(1, source.Items[1]!.Count);
            Assert.AreEqual(0, destination.Items.TakenSlots);
            Assert.AreEqual(sourceUpdates, source.UpdateCount);
            Assert.AreEqual(destinationUpdates, destination.UpdateCount);
        }

        [TestMethod]
        public void TryTransfer_PreferredSlots_UsesSourceFirstAndExplicitDestinationSlot()
        {
            var source = new TestableItemContainer(StorageType.Normal, 2);
            source.Items.ReplaceState(
            [
                CreateItem(1, 3, stackable: true),
                CreateItem(1, 5, stackable: true)
            ]);
            var destination = new TestableItemContainer(StorageType.Normal, 2);

            Assert.IsTrue(Transfer(source.Items, destination.Items, source.Items[0]!, 4, preferredSourceSlot: 1, destinationSlot: 1));

            Assert.AreEqual(3, source.Items[0]!.Count);
            Assert.AreEqual(1, source.Items[1]!.Count);
            Assert.IsNull(destination.Items[0]);
            Assert.AreEqual(4, destination.Items[1]!.Count);
        }

        [TestMethod]
        public void TryTransfer_DrainedSentinelSlot_RetainsZeroCountSourceAndCopiesItem()
        {
            var source = new TestableItemContainer(StorageType.Normal, 1, 0);
            source.Items.Add(CreateItem(1, 5, stackable: true));
            var sourceItem = source.Items[0];
            var destination = new TestableItemContainer(StorageType.Normal, 1);

            Assert.IsTrue(Transfer(source.Items, destination.Items, sourceItem!, 5));

            Assert.AreSame(sourceItem, source.Items[0]);
            Assert.AreEqual(0, source.Items[0]!.Count);
            Assert.AreNotSame(sourceItem, destination.Items[0]);
            Assert.AreEqual(5, destination.Items[0]!.Count);
        }

        [TestMethod]
        public void TryTransfer_SameContainerOrNonPositiveCount_DoesNotMutate()
        {
            var container = new TestableItemContainer(StorageType.Normal, 1);
            container.Items.Add(CreateItem(1, 2, stackable: true));
            var item = container.Items[0];
            var updates = container.UpdateCount;
            var destination = new TestableItemContainer(StorageType.Normal, 1);

            Assert.IsFalse(Transfer(container.Items, container.Items, item!, 1));
            using (ItemContainerTransaction.Begin(container.Items.Mutations, destination.Items.Mutations))
            {
                Assert.IsFalse(container.Items.Mutations.TryTransferTo(destination.Items.Mutations, item!, 0));
                Assert.IsFalse(container.Items.Mutations.TryTransferTo(destination.Items.Mutations, item!, -1));
            }

            Assert.AreSame(item, container.Items[0]);
            Assert.AreEqual(2, container.Items[0]!.Count);
            Assert.AreEqual(updates, container.UpdateCount);
        }

        [TestMethod]
        public void TryTransfer_SourcePublicationThrowsAfterCommit_LeavesCommittedStorageInPlace()
        {
            var source = new TestableItemContainer(StorageType.Normal, 1);
            source.Items.Add(CreateItem(1, 1));
            source.ThrowOnPublication = true;
            var destination = new TestableItemContainer(StorageType.Normal, 1);
            var item = source.Items[0];

            Assert.ThrowsExactly<InvalidOperationException>(
                () => Transfer(source.Items, destination.Items, item!, 1));

            Assert.IsNull(source.Items[0]);
            Assert.AreSame(item, destination.Items[0]);
        }

        [TestMethod]
        public void TryTransfer_HugeNonStackableQuantityRejectsBeforePerUnitCloning()
        {
            var cloneCount = 0;
            var source = new TestableItemContainer(StorageType.AlwaysStack, 1);
            var sourceItem = CreateItem(1, int.MaxValue, onClone: () => cloneCount++);
            Assert.IsTrue(source.Items.Add(sourceItem));
            var destination = new TestableItemContainer(StorageType.Normal, 4);
            var sourceUpdates = source.UpdateCount;

            Assert.IsFalse(Transfer(source.Items, destination.Items, sourceItem, int.MaxValue));

            Assert.AreEqual(0, cloneCount);
            Assert.AreSame(sourceItem, source.Items[0]);
            Assert.AreEqual(int.MaxValue, source.Items[0]!.Count);
            Assert.AreEqual(0, destination.Items.TakenSlots);
            Assert.AreEqual(sourceUpdates, source.UpdateCount);
            Assert.AreEqual(0, destination.UpdateCount);
        }

        [TestMethod]
        public async Task TryTransfer_OppositeDirectionWaitsForForeignActiveScopeAndSucceedsAfterRelease()
        {
            var left = new TestableItemContainer(StorageType.Normal, 2);
            var right = new TestableItemContainer(StorageType.Normal, 2);
            Assert.IsTrue(left.Items.Add(CreateItem(1, 1)));
            Assert.IsTrue(right.Items.Add(CreateItem(2, 1)));
            var leftItem = left.Items[0]!;
            var rightItem = right.Items[0]!;
            using var entered = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            using var attempted = new ManualResetEventSlim();
            var leftToRight = Task.Run(() =>
            {
                using var transaction = ItemContainerTransaction.Begin(left.Items.Mutations, right.Items.Mutations);
                entered.Set();
                Assert.IsTrue(release.Wait(TimeSpan.FromSeconds(5)));
                Assert.IsTrue(left.Items.Mutations.TryTransferTo(right.Items.Mutations, leftItem, 1));
                transaction.Commit();
            });
            Task<bool>? rightToLeft = null;
            try
            {
                Assert.IsTrue(entered.Wait(TimeSpan.FromSeconds(5)));
                rightToLeft = Task.Run(() =>
                {
                    attempted.Set();
                    return Transfer(right.Items, left.Items, rightItem, 1);
                });
                Assert.IsTrue(attempted.Wait(TimeSpan.FromSeconds(5)));
                Assert.IsFalse(((IAsyncResult)rightToLeft).AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(100)),
                    "An independent helper must wait for the foreign scope.");
            }
            finally { release.Set(); }
            await leftToRight.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.IsNotNull(rightToLeft);
            Assert.IsTrue(await rightToLeft.WaitAsync(TimeSpan.FromSeconds(5)));
            Assert.AreEqual(1, left.Items.GetCountById(2));
            Assert.AreEqual(1, right.Items.GetCountById(1));
            Assert.AreEqual(2, left.Items.TakenSlots + right.Items.TakenSlots);
        }

        private static bool Transfer(ItemContainer source, ItemContainer destination, IItem item, int count, int preferredSourceSlot = -1, int destinationSlot = -1)
        {
            using var transaction = ItemContainerTransaction.Begin(source.Mutations, destination.Mutations);
            if (!source.Mutations.TryTransferTo(destination.Mutations, item, count, preferredSourceSlot, destinationSlot)) return false;
            transaction.Commit();
            return true;
        }

        [TestMethod]
        public void AddRange_DestinationFull_LeavesContentsRevisionAndNotificationsUnchanged()
        {
            var container = new TestableItemContainer(StorageType.Normal, 1);
            container.Items.Add(CreateItem(1, 1));
            var original = container.Items[0];
            var updates = container.UpdateCount;
            var enumerator = container.Items.GetEnumerator();

            Assert.IsFalse(container.Items.AddRange([CreateItem(2, 1)]));

            Assert.AreSame(original, container.Items[0]);
            Assert.AreEqual(1, container.Items[0]!.Id);
            Assert.AreEqual(updates, container.UpdateCount);
            Assert.IsTrue(enumerator.MoveNext());
        }
    }
}
