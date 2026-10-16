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

            public TestableItemContainer(StorageType type, int capacity, int countToResetTo = -1)
            {
                Items = new ItemContainer(type, capacity, countToResetTo);
            }

            public TestableItemContainer(StorageType type, IEnumerable<IItem> items, int capacity)
            {
                Items = new ItemContainer(type, items, capacity);
            }

            public void ReplaceState(IItem?[] items)
            {
                using var write = Items.BeginWrite();
                Items.ReplaceState(items);
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
        public void StorageMutators_AreSynchronizationAgnostic()
        {
            var storage = new ItemContainerStorage(StorageType.Normal, 2);
            var item = CreateItem(1, 1);

            Assert.IsTrue(storage.TryAdd(item, out _));
            Assert.AreEqual(1, storage.Remove(item, -1, out _));
            storage.Replace(0, CreateItem(2, 1));
            Assert.AreEqual(2, storage[0]!.Id);
        }

        [TestMethod]
        public void Storage_DoesNotOwnSynchronizationOrTransactionState()
        {
            var storageType = typeof(ItemContainerStorage);
            var fields = storageType.GetFields(System.Reflection.BindingFlags.Instance |
                                               System.Reflection.BindingFlags.Static |
                                               System.Reflection.BindingFlags.Public |
                                               System.Reflection.BindingFlags.NonPublic);

            Assert.IsFalse(fields.Any(field => field.FieldType == typeof(ItemContainerTransaction) ||
                                               field.FieldType == typeof(ItemContainerSynchronization)));
            Assert.IsFalse(storageType.GetMembers(System.Reflection.BindingFlags.Instance |
                                                  System.Reflection.BindingFlags.Static |
                                                  System.Reflection.BindingFlags.Public |
                                                  System.Reflection.BindingFlags.NonPublic)
                .Any(member => member.Name.Contains("SynchronizationLock", StringComparison.Ordinal) ||
                               member.Name.Contains("LockOrder", StringComparison.Ordinal) ||
                               member.Name.Contains("Transaction", StringComparison.Ordinal) ||
                               member.Name.Contains("StandalonePublication", StringComparison.Ordinal)));
        }

        [TestMethod]
        public void RestoreSnapshotState_RestoresStorageStateWithoutSynchronization()
        {
            var storage = new ItemContainerStorage(StorageType.Normal, 2);
            var item = CreateItem(1, 1);
            Assert.IsTrue(storage.TryAdd(item, out _));
            var storedItem = storage[0];
            var revision = storage.MutationRevision;
            var snapshot = new IItem?[] { CreateItem(2, 1), null };
            var counts = new[] { 1, 0 };

            storage.RestoreSnapshotState(snapshot, counts, revision + 10);

            Assert.AreSame(snapshot[0], storage[0]);
            Assert.AreEqual(1, storedItem!.Count);
            Assert.AreEqual(revision + 10, storage.MutationRevision);
        }

                        [TestMethod]
        public void ItemContainerAdd_TransactionScopeRetainsSynchronizationLock()
        {
            var container = new ItemContainer(StorageType.Normal, 2);
            var storageLock = ItemContainerTransaction.ResolveSingleSynchronization(container).SynchronizationLock;
            using var transaction = ItemContainerTransaction.Begin(container);

            Assert.IsTrue(container.Add(CreateItem(1, 1)));
            Assert.IsTrue(Monitor.IsEntered(storageLock));

            transaction.Commit();
            Assert.IsFalse(Monitor.IsEntered(storageLock));
        }

        [TestMethod]
        public void BeginMutation_RejectsManuallyHeldLockWithoutReleasingIt()
        {
            var container = new ItemContainer(StorageType.Normal, 2);
            var storageLock = ItemContainerTransaction.ResolveSingleSynchronization(container).SynchronizationLock;

            lock (storageLock)
            {
                Assert.ThrowsExactly<InvalidOperationException>(() => container.BeginWrite());
                Assert.IsTrue(Monitor.IsEntered(storageLock));
            }
        }

                [TestMethod]
        public void ItemContainerAdd_FailedStandaloneMutationReleasesLockWithoutTrackingChanges()
        {
            var container = new ItemContainer(StorageType.Normal, 1);
            var storageLock = ItemContainerTransaction.ResolveSingleSynchronization(container).SynchronizationLock;
            Assert.IsTrue(container.Add(CreateItem(1, 1)));
            container.ChangeTracker.GetSlots();

            Assert.IsFalse(container.Add(CreateItem(2, 1)));

            var lockWasReleased = Task.Run(() =>
            {
                var acquired = Monitor.TryEnter(storageLock, 0);
                if (acquired) Monitor.Exit(storageLock);
                return acquired;
            }).GetAwaiter().GetResult();
            Assert.IsTrue(lockWasReleased);
            Assert.IsFalse(container.ChangeTracker.HasChanges);
        }

                [TestMethod]
        public void TryRemoveExact_InsufficientQuantityLeavesContainerUnchanged()
        {
            var container = new TestableItemContainer(StorageType.Normal, 2);
            var item = CreateItem(1, 2, stackable: true);
            Assert.IsTrue(container.Items.Add(0, item));
            container.Items.ChangeTracker.GetSlots();
            var revision = ItemContainerTransaction.ResolveSingleSynchronization(container.Items).Storage.MutationRevision;
            var enumerator = container.Items.GetEnumerator();

            Assert.IsFalse(container.Items.TryRemoveExact(CreateItem(1, 3, stackable: true), 0));

            Assert.AreSame(item, container.Items[0]);
            Assert.AreEqual(2, item.Count);
            Assert.AreEqual(revision, ItemContainerTransaction.ResolveSingleSynchronization(container.Items).Storage.MutationRevision);
            Assert.IsFalse(container.Items.ChangeTracker.HasChanges);
            Assert.IsTrue(enumerator.MoveNext());

            Assert.IsTrue(container.Items.TryRemoveExact(CreateItem(1, 2, stackable: true), 0));
            Assert.IsNull(container.Items[0]);
            CollectionAssert.AreEquivalent(new[] { 0 }, container.Items.ChangeTracker.GetSlots()!.ToArray());
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
            private readonly bool _customStacking;
            public Action? OnCanStackItem { get; set; }
            public long[] ExtraData => _extraData;

            public TestItem(int id, int count, bool stackable = false, bool noted = false, long[]? extraData = null,
                Action? onClone = null, bool customStacking = false)
            {
                Id = id;
                Count = count;
                _extraData = extraData ?? Array.Empty<long>();
                _onClone = onClone;
                _customStacking = customStacking;
                Name = $"TestItem_{id}";

                var itemDef = Substitute.For<IItemDefinition>();
                itemDef.Stackable.Returns(stackable);
                itemDef.Noted.Returns(noted);
                ItemDefinition = itemDef;

                var itemScript = Substitute.For<IItemScript>();
                itemScript.CanStackItem(Arg.Any<IItem>(), Arg.Any<IItem>(), Arg.Any<bool>()).Returns(info =>
                {
                    OnCanStackItem?.Invoke();
                    var arg1 = (IItem)info[0];
                    var arg2 = (IItem)info[1];
                    var alwaysStack = (bool)info[2];
                    return arg1.Id == arg2.Id &&
                           arg1.ExtraData.AsSpan().SequenceEqual(arg2.ExtraData) &&
                           (alwaysStack || arg1.ItemDefinition.Stackable || arg1.ItemDefinition.Noted || _customStacking);
                });
                ItemScript = itemScript;

                EquipmentDefinition = Substitute.For<IEquipmentDefinition>();
                EquipmentScript = Substitute.For<IEquipmentScript>();
            }

            public IItem Clone() => Clone(1);
            public IItem Clone(int newCount)
            {
                _onClone?.Invoke();
                return new TestItem(Id, newCount, ItemDefinition.Stackable, ItemDefinition.Noted,
                    (long[])_extraData.Clone(), _onClone, _customStacking);
            }

            public bool Equals(IItem other, bool ignoreCount = true) =>
                other is not null &&
                Id == other.Id &&
                _extraData.AsSpan().SequenceEqual(other.ExtraData) &&
                (ignoreCount || Count == other.Count);

            public string? SerializeExtraData() => _extraData.Length == 0 ? null : string.Join(",", _extraData);
        }

        private IItem CreateItem(int id, int count, bool stackable = false, bool noted = false, long[]? extraData = null,
            Action? onClone = null, bool customStacking = false)
        {
            return new TestItem(id, count, stackable, noted, extraData, onClone, customStacking);
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
        public void TryAdd_NonStackableCloneFailureLeavesStorageUnchanged()
        {
            var container = new ItemContainer(StorageType.Normal, 3);
            Assert.IsTrue(container.Add(CreateItem(1, 1)));
            var existing = container[0];
            container.ChangeTracker.GetSlots();

            var storage = ItemContainerTransaction.ResolveSingleSynchronization(container).Storage;
            var revision = storage.MutationRevision;
            var enumerator = container.GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreSame(existing, enumerator.Current);

            var cloneCount = 0;
            var cloneFailure = new InvalidOperationException("Clone failed.");
            var incoming = CreateItem(2, 2, onClone: () =>
            {
                if (++cloneCount == 2)
                {
                    throw cloneFailure;
                }
            });

            var exception = Assert.ThrowsExactly<InvalidOperationException>(() => container.Add(incoming));

            Assert.AreSame(cloneFailure, exception);
            Assert.AreSame(existing, container[0]);
            Assert.IsNull(container[1]);
            Assert.IsNull(container[2]);
            Assert.AreEqual(revision, storage.MutationRevision);
            Assert.IsFalse(container.ChangeTracker.HasChanges);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);
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
        public void Swap_SameSlot_LeavesContainerUnchanged()
        {
            AssertSameSlotMutationLeavesContainerUnchanged(items => items.Swap(0, 0));
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
        public void Move_SameSlot_LeavesContainerUnchanged()
        {
            AssertSameSlotMutationLeavesContainerUnchanged(items => items.Move(0, 0));
        }

        private void AssertSameSlotMutationLeavesContainerUnchanged(Action<ItemContainer> mutation)
        {
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var item1 = CreateItem(1, 1);
            var item2 = CreateItem(2, 1);
            container.Items.Replace(0, item1);
            container.Items.Replace(1, item2);
            container.Items.ChangeTracker.GetSlots();
            var revision = container.Items.ChangeTracker.Revision;
            var enumerator = container.Items.GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());

            mutation(container.Items);

            Assert.AreSame(item1, container.Items[0]);
            Assert.AreSame(item2, container.Items[1]);
            Assert.AreEqual(revision, container.Items.ChangeTracker.Revision);
            Assert.IsFalse(container.Items.ChangeTracker.HasChanges);
            Assert.AreEqual(0, container.Items.ChangeTracker.GetSlots()!.Count);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreSame(item2, enumerator.Current);
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
        public void Clear_EmptyAndZeroCapacityContainersPreserveRevisionAndTracker()
        {
            var empty = new ItemContainer(StorageType.Normal, 10);
            var emptyStorage = ItemContainerTransaction.ResolveSingleSynchronization(empty).Storage;
            var emptyRevision = emptyStorage.MutationRevision;

            empty.Clear();
            using (var transaction = ItemContainerTransaction.Begin(empty))
            {
                empty.Clear();
                transaction.Commit();
            }

            Assert.AreEqual(emptyRevision, emptyStorage.MutationRevision);
            Assert.IsFalse(empty.ChangeTracker.HasChanges);

            var zeroCapacity = new ItemContainer(StorageType.Normal, 0);
            var zeroCapacityStorage = ItemContainerTransaction.ResolveSingleSynchronization(zeroCapacity).Storage;
            var zeroCapacityRevision = zeroCapacityStorage.MutationRevision;

            zeroCapacity.Clear();

            Assert.AreEqual(zeroCapacityRevision, zeroCapacityStorage.MutationRevision);
            Assert.IsFalse(zeroCapacity.ChangeTracker.HasChanges);
        }

        [TestMethod]
        public void Clear_NonnullZeroCountSentinelIsTreatedAsOccupied()
        {
            var storage = new ItemContainerStorage(StorageType.Normal, 1, countToResetTo: 0);
            var sentinel = CreateItem(1, 0);
            storage.RestoreItems([(0, sentinel)], allowZeroCount: true);
            var revision = storage.MutationRevision;

            Assert.IsTrue(storage.Clear());

            Assert.IsNull(storage[0]);
            Assert.AreEqual(revision + 1, storage.MutationRevision);
        }

                [TestMethod]
        public void Clear_NonemptyTransactionRollsBackContentsAndRevision()
        {
            var container = new ItemContainer(StorageType.Normal, 2);
            var item = CreateItem(1, 1);
            Assert.IsTrue(container.Add(item));
            var storedItem = container[0];
            var storage = ItemContainerTransaction.ResolveSingleSynchronization(container).Storage;
            var revision = storage.MutationRevision;
            container.ChangeTracker.GetSlots();

            using (ItemContainerTransaction.Begin(container))
                container.Clear();

            Assert.AreSame(storedItem, container[0]);
            Assert.AreEqual(revision, storage.MutationRevision);
            Assert.IsFalse(container.ChangeTracker.HasChanges);
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
        public void Remove_MultipleMatchingStacks_RemovesRequestedTotalOnly()
        {
            var container = new TestableItemContainer(
                StorageType.Normal,
                [CreateItem(1, 3, stackable: true), CreateItem(1, 4, stackable: true)],
                10);

            var removedCount = container.Items.Remove(CreateItem(1, 2, stackable: true));

            Assert.AreEqual(2, removedCount);
            Assert.AreEqual(1, container.Items[0].Count);
            Assert.AreEqual(4, container.Items[1].Count);
        }

        [TestMethod]
        public void Remove_ThreeMatchingStacks_RemovesRequestedTotalOnly()
        {
            var container = new TestableItemContainer(StorageType.Normal,
                [CreateItem(1, 2, stackable: true), CreateItem(1, 3, stackable: true), CreateItem(1, 4, stackable: true)], 4);

            Assert.AreEqual(5, container.Items.Remove(CreateItem(1, 5, stackable: true)));

            Assert.IsNull(container.Items[0]);
            Assert.IsNull(container.Items[1]);
            Assert.AreEqual(4, container.Items[2]!.Count);
        }

        [TestMethod]
        public void Remove_InsufficientMatchingStacksRemovesAvailableTotal()
        {
            var container = new TestableItemContainer(StorageType.Normal,
                [CreateItem(1, 3, stackable: true), CreateItem(1, 4, stackable: true)], 3);

            Assert.AreEqual(7, container.Items.Remove(CreateItem(1, 9, stackable: true)));

            Assert.AreEqual(0, container.Items.TakenSlots);
        }

        [TestMethod]
        public void Remove_ZeroCountSentinelDoesNotCountAsAvailableQuantity()
        {
            var container = new TestableItemContainer(StorageType.Normal, 2, countToResetTo: 0);
            var sentinel = CreateItem(1, 0, stackable: true);
            container.Items.RestoreItems([(0, sentinel)], allowZeroCount: true);
            var revision = ItemContainerTransaction.ResolveSingleSynchronization(container.Items).Storage.MutationRevision;

            Assert.AreEqual(0, container.Items.Remove(CreateItem(1, 1, stackable: true)));

            Assert.AreSame(sentinel, container.Items[0]);
            Assert.AreEqual(0, sentinel.Count);
            Assert.AreEqual(revision, ItemContainerTransaction.ResolveSingleSynchronization(container.Items).Storage.MutationRevision);
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
            HashSet<int> changedSlots;

            {
                Assert.IsTrue(storage.TryAdd(existing, out _));
                Assert.IsTrue(storage.TryAddRange([CreateItem(1, 3, stackable: true), newStack], out changedSlots));
            }

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

            bool result;
            HashSet<int> changedSlots;
             result = storage.TryAddRange(incoming, out changedSlots);

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

            HashSet<int> changedSlots;

                Assert.IsTrue(storage.TryAddRange([CreateItem(1, 1)], out changedSlots));

            Assert.IsNotEmpty(changedSlots);
            Assert.AreEqual(revision + 1, storage.MutationRevision);
            Assert.ThrowsExactly<InvalidOperationException>(() => enumerator.MoveNext());
        }

                [TestMethod]
        public void ItemContainer_AddRangeEmptyRangeDoesNotTrackChangesOrInvalidateEnumerator()
        {
            var container = new ItemContainer(StorageType.Normal, 2);
            var existing = CreateItem(1, 3, stackable: true);
            Assert.IsTrue(container.Add(existing));
            container.ChangeTracker.GetSlots();
            var enumerator = container.GetEnumerator();

            var result = container.AddRange(Array.Empty<IItem?>());

            Assert.IsTrue(result);
            Assert.IsFalse(container.ChangeTracker.HasChanges);
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

            HashSet<int> changedSlots;

                Assert.IsTrue(storage.TryAddRange([incoming], out changedSlots));

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

            bool result;
            HashSet<int> changedSlots;
             result = storage.TryAddRange([incoming], out changedSlots);

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
        public void GetCountById_WithSeparateLargeStacks_SaturatesAtIntMax()
        {
            var container = new TestableItemContainer(StorageType.Normal, 2);
            var firstStack = CreateItem(1, 1_500_000_000, stackable: true, extraData: [17]);
            var secondStack = CreateItem(1, 1_500_000_000, stackable: true, extraData: [29]);

            Assert.IsTrue(container.Items.Add(firstStack));
            Assert.IsTrue(container.Items.Add(secondStack));

            Assert.AreEqual(int.MaxValue, container.Items.GetCountById(1));
            Assert.AreEqual(1_500_000_000, container.Items.GetCount(firstStack));
            Assert.AreEqual(1_500_000_000, container.Items.GetCount(secondStack));
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
        public void AddToSlot_NonStackableMultipleUnits_RequiresMultipleSlots()
        {
            var container = new TestableItemContainer(StorageType.Normal, 1);
            var item = CreateItem(1, 2);

            Assert.IsFalse(container.Items.Add(0, item));
            Assert.IsFalse(container.Items.Add(item));
            Assert.IsNull(container.Items[0]);
        }

        [TestMethod]
        public void AddToSlot_AlwaysStackAllowsMultipleNonStackableUnits()
        {
            var container = new TestableItemContainer(StorageType.AlwaysStack, 1);
            var item = CreateItem(1, 2);

            Assert.IsTrue(container.Items.Add(0, item));
            Assert.AreEqual(2, container.Items[0]!.Count);
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
        public void NormalMutation_UsesStorageSynchronizationLock()
        {
            var container = new TestableItemContainer(StorageType.Normal, 10);
            using var started = new ManualResetEventSlim();
            using var finished = new ManualResetEventSlim();

            var startedInTime = false;
            var blockedOnSynchronizationLock = false;
            var finishedWhileLocked = false;
            var boundary = ItemContainerTransaction.ResolveSingleSynchronization(container.Items);
            Exception? mutationFailure = null;
            var mutation = new Thread(() =>
            {
                started.Set();
                try
                {
                    container.Items.Add(CreateItem(1, 1));
                }
                catch (Exception exception)
                {
                    mutationFailure = exception;
                }
                finally
                {
                    finished.Set();
                }
            }) { IsBackground = true };

            lock (boundary.SynchronizationLock)
            {
                mutation.Start();
                startedInTime = started.Wait(TimeSpan.FromSeconds(5));
                if (startedInTime)
                {
                    blockedOnSynchronizationLock = SpinWait.SpinUntil(
                        () => (mutation.ThreadState & ThreadState.WaitSleepJoin) != 0,
                        TimeSpan.FromSeconds(5));
                    finishedWhileLocked = finished.IsSet;
                }
            }

            Assert.IsTrue(startedInTime);
            Assert.IsTrue(blockedOnSynchronizationLock, "The mutation must wait on the held boundary lock.");
            Assert.IsFalse(finishedWhileLocked);
            Assert.IsTrue(mutation.Join(TimeSpan.FromSeconds(5)));
            if (mutationFailure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(mutationFailure).Throw();
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
        public void Sort_AlreadyCompactContainerDoesNotReviseOrTrackChanges()
        {
            var container = new TestableItemContainer(StorageType.Normal, 3);
            container.Items.Add(CreateItem(1, 1));
            container.Items.Add(CreateItem(2, 1));
            container.Items.ChangeTracker.GetSlots();
            var first = container.Items[0];
            var second = container.Items[1];
            var revision = ItemContainerTransaction.ResolveSingleSynchronization(container.Items).Storage.MutationRevision;

            container.Items.Sort();

            Assert.AreSame(first, container.Items[0]);
            Assert.AreSame(second, container.Items[1]);
            Assert.AreEqual(revision, ItemContainerTransaction.ResolveSingleSynchronization(container.Items).Storage.MutationRevision);
            Assert.IsFalse(container.Items.ChangeTracker.HasChanges);
        }

        [TestMethod]
        public void Add_MisbehavingStackCheckRejectsContendedStorageWithoutDeadlock()
        {
            var source = new TestableItemContainer(StorageType.Normal, 2);
            var destination = new TestableItemContainer(StorageType.Normal, 1);
            var existing = (TestItem)CreateItem(1, 1, stackable: true);
            Assert.IsTrue(source.Items.Add(existing));
            var sourceBoundary = ItemContainerTransaction.ResolveSingleSynchronization(source.Items);
            var destinationBoundary = ItemContainerTransaction.ResolveSingleSynchronization(destination.Items);
            using var destinationLockHeld = new ManualResetEventSlim();
            using var releaseDestination = new ManualResetEventSlim();
            Exception? lockOwnerFailure = null;
            var lockOwner = new Thread(() =>
            {
                try
                {
                    lock (destinationBoundary.SynchronizationLock)
                    {
                        destinationLockHeld.Set();
                        Assert.IsTrue(releaseDestination.Wait(TimeSpan.FromSeconds(5)));
                    }
                }
                catch (Exception exception) { lockOwnerFailure = exception; }
            }) { IsBackground = true };
            lockOwner.Start();
            Assert.IsTrue(destinationLockHeld.Wait(TimeSpan.FromSeconds(5)));
            existing.OnCanStackItem = () =>
            {
                Assert.IsTrue(ItemContainerSynchronization.CurrentThreadOwnsStorage);
                Assert.IsTrue(Monitor.IsEntered(sourceBoundary.SynchronizationLock));
                destination.Items.Add(CreateItem(2, 1));
            };

            try
            {
                Assert.ThrowsExactly<InvalidOperationException>(() => source.Items.Add(CreateItem(1, 1, stackable: true)));
            }
            finally { releaseDestination.Set(); }

            Assert.IsTrue(lockOwner.Join(TimeSpan.FromSeconds(5)));
            if (lockOwnerFailure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(lockOwnerFailure).Throw();
            Assert.AreEqual(1, existing.Count);
            Assert.AreEqual(1, source.Items.TakenSlots);
            Assert.AreEqual(0, destination.Items.TakenSlots);
            Assert.IsFalse(ItemContainerSynchronization.CurrentThreadOwnsStorage);
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
            container.ReplaceState(newItems);

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

            container.ReplaceState(replacement);

            Assert.ThrowsExactly<InvalidOperationException>(() => enumerator.MoveNext());
            Assert.AreEqual(2, container.Items[1]!.Id);
        }

        [TestMethod]
        public void ReplaceState_WithInvalidLength_RejectsInputAndKeepsCapacitySizedStorage()
        {
            var container = new TestableItemContainer(StorageType.Normal, 10);
            container.Items.Add(CreateItem(1, 1));

            Assert.ThrowsExactly<ArgumentException>(() => container.ReplaceState([CreateItem(2, 1)]));

            Assert.AreEqual(container.Items.Capacity, container.Items.SnapshotItems().Length);
            Assert.AreEqual(1, container.Items[0]!.Id);
        }

        [TestMethod]
        public void ReplaceState_DoesNotRetainCallerOwnedArray()
        {
            var container = new TestableItemContainer(StorageType.Normal, 10);
            var items = new IItem[container.Items.Capacity];
            items[5] = CreateItem(10, 1);
            container.ReplaceState(items);

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
            source.Items.ChangeTracker.GetSlots();
            destination.Items.ChangeTracker.GetSlots();
            var sourceEnumerator = source.Items.GetEnumerator();
            var destinationEnumerator = destination.Items.GetEnumerator();

            Assert.IsFalse(Transfer(source.Items, destination.Items, sourceItem!, 1));

            Assert.AreSame(sourceItem, source.Items[0]);
            Assert.AreSame(destinationItem, destination.Items[0]);
            Assert.IsFalse(source.Items.ChangeTracker.HasChanges);
            Assert.IsFalse(destination.Items.ChangeTracker.HasChanges);
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
            source.Items.ChangeTracker.GetSlots();
            var sourceEnumerator = source.Items.GetEnumerator();

            Assert.IsFalse(Transfer(source.Items, destination.Items, CreateItem(1, 3, stackable: true), 3));

            Assert.AreSame(sourceItem, source.Items[0]);
            Assert.AreEqual(2, source.Items[0]!.Count);
            Assert.IsNull(destination.Items[0]);
            Assert.IsFalse(source.Items.ChangeTracker.HasChanges);
            Assert.IsFalse(destination.Items.ChangeTracker.HasChanges);
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
        public void TryTransfer_StackableQuantity_TransfersExactCountAndTracksAfterCommit()
        {
            var source = new TestableItemContainer(StorageType.Normal, 2);
            source.Items.Add(CreateItem(1, 10, stackable: true));
            var destination = new TestableItemContainer(StorageType.Normal, 2);
            destination.Items.Add(CreateItem(1, 4, stackable: true));
            var sourceItem = source.Items[0];
            var destinationItem = destination.Items[0];
            source.Items.ChangeTracker.GetSlots();
            destination.Items.ChangeTracker.GetSlots();

            Assert.IsTrue(Transfer(source.Items, destination.Items, sourceItem!, 3));

            Assert.AreSame(sourceItem, source.Items[0]);
            Assert.AreSame(destinationItem, destination.Items[0]);
            Assert.AreEqual(7, source.Items[0]!.Count);
            Assert.AreEqual(7, destination.Items[0]!.Count);
            CollectionAssert.AreEquivalent(new[] { 0 }, source.Items.ChangeTracker.GetSlots()!.ToArray());
            CollectionAssert.AreEquivalent(new[] { 0 }, destination.Items.ChangeTracker.GetSlots()!.ToArray());
        }

        [TestMethod]
        public void TryTransfer_CustomNonStackableItemUsesItemScriptStacking()
        {
            // PvPToken permits same-ID, same-extra-data stacks even when its item definition is non-stackable.
            var source = new TestableItemContainer(StorageType.Normal, 2);
            var sourceItem = CreateItem(1815, 1, extraData: [17], customStacking: true);
            Assert.IsTrue(source.Items.Add(sourceItem));
            Assert.IsTrue(source.Items.Add(CreateItem(1815, 1, extraData: [17], customStacking: true)));
            Assert.AreEqual(2, source.Items[0]!.Count);
            Assert.IsFalse(source.Items[0]!.ItemDefinition.Stackable);

            var destination = new TestableItemContainer(StorageType.Normal, 1);
            Assert.IsTrue(Transfer(source.Items, destination.Items, source.Items[0]!, 2));

            Assert.IsNull(source.Items[0]);
            Assert.AreEqual(1815, destination.Items[0]!.Id);
            Assert.AreEqual(2, destination.Items[0]!.Count);
            CollectionAssert.AreEqual(new long[] { 17 }, destination.Items[0]!.ExtraData);
            Assert.IsFalse(destination.Items[0]!.ItemDefinition.Stackable);
            Assert.AreEqual(1, destination.Items.TakenSlots);
        }

        [TestMethod]
        public void TryTransfer_NonStackableQuantity_PreservesMovedInstancesAndExtraData()
        {
            var source = new TestableItemContainer(StorageType.Normal, 2);
            source.ReplaceState(
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
            source.ReplaceState(
            [
                CreateItem(1, 1, extraData: [17]),
                CreateItem(1, 1, extraData: [29])
            ]);
            var first = source.Items[0];
            var second = source.Items[1];
            var destination = new TestableItemContainer(StorageType.Normal, 2);

            Assert.IsFalse(Transfer(source.Items, destination.Items, first!, 2));

            Assert.AreSame(first, source.Items[0]);
            Assert.AreSame(second, source.Items[1]);
            Assert.AreEqual(1, source.Items[0]!.Count);
            Assert.AreEqual(1, source.Items[1]!.Count);
            Assert.AreEqual(0, destination.Items.TakenSlots);
            Assert.IsFalse(source.Items.ChangeTracker.HasChanges);
            Assert.IsFalse(destination.Items.ChangeTracker.HasChanges);
        }

        [TestMethod]
        public void TryTransfer_PreferredSlots_UsesSourceFirstAndExplicitDestinationSlot()
        {
            var source = new TestableItemContainer(StorageType.Normal, 2);
            source.ReplaceState(
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
            container.Items.ChangeTracker.GetSlots();
            var destination = new TestableItemContainer(StorageType.Normal, 1);

            Assert.IsFalse(Transfer(container.Items, container.Items, item!, 1));
            using (ItemContainerTransaction.Begin(container.Items, destination.Items))
            {
                Assert.IsFalse(container.Items.TryTransferTo(destination.Items, item!, 0));
                Assert.IsFalse(container.Items.TryTransferTo(destination.Items, item!, -1));
            }

            Assert.AreSame(item, container.Items[0]);
            Assert.AreEqual(2, container.Items[0]!.Count);
            Assert.IsFalse(container.Items.ChangeTracker.HasChanges);
            Assert.IsFalse(destination.Items.ChangeTracker.HasChanges);
        }

                        [TestMethod]
        public void TryTransfer_HugeNonStackableQuantityRejectsBeforePerUnitCloning()
        {
            var cloneCount = 0;
            var source = new TestableItemContainer(StorageType.AlwaysStack, 1);
            var sourceItem = CreateItem(1, int.MaxValue, onClone: () => cloneCount++);
            Assert.IsTrue(source.Items.Add(sourceItem));
            var destination = new TestableItemContainer(StorageType.Normal, 4);
            source.Items.ChangeTracker.GetSlots();

            Assert.IsFalse(Transfer(source.Items, destination.Items, sourceItem, int.MaxValue));

            Assert.AreEqual(0, cloneCount);
            Assert.AreSame(sourceItem, source.Items[0]);
            Assert.AreEqual(int.MaxValue, source.Items[0]!.Count);
            Assert.AreEqual(0, destination.Items.TakenSlots);
            Assert.IsFalse(source.Items.ChangeTracker.HasChanges);
            Assert.IsFalse(destination.Items.ChangeTracker.HasChanges);
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
                using var transaction = ItemContainerTransaction.Begin(left.Items, right.Items);
                entered.Set();
                Assert.IsTrue(release.Wait(TimeSpan.FromSeconds(5)));
                Assert.IsTrue(left.Items.TryTransferTo(right.Items, leftItem, 1));
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
            using var transaction = ItemContainerTransaction.Begin(source, destination);
            if (!source.TryTransferTo(destination, item, count, preferredSourceSlot, destinationSlot)) return false;
            transaction.Commit();
            return true;
        }

                [TestMethod]
        public void AddRange_DestinationFull_LeavesContentsRevisionAndTrackerUnchanged()
        {
            var container = new TestableItemContainer(StorageType.Normal, 1);
            container.Items.Add(CreateItem(1, 1));
            var original = container.Items[0];
            container.Items.ChangeTracker.GetSlots();
            var revision = ItemContainerTransaction.ResolveSingleSynchronization(container.Items).Storage.MutationRevision;
            var enumerator = container.Items.GetEnumerator();

            Assert.IsFalse(container.Items.AddRange([CreateItem(2, 1)]));

            Assert.AreSame(original, container.Items[0]);
            Assert.AreEqual(1, container.Items[0]!.Id);
            Assert.AreEqual(revision, ItemContainerTransaction.ResolveSingleSynchronization(container.Items).Storage.MutationRevision);
            Assert.IsFalse(container.Items.ChangeTracker.HasChanges);
            Assert.IsTrue(enumerator.MoveNext());
        }

    }
}
