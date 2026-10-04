using System;
using System.Collections;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Implements the generic item-container contract over composed storage.</summary>
public sealed class ItemContainer : IItemContainer, IItemTransactionSource
{
    private readonly ItemContainerStorage _storage;
    private readonly ItemContainerMutationBoundary _mutations;

    IReadOnlyList<ItemContainerMutationBoundary> IItemTransactionSource.Boundaries => [_mutations];

    internal int CountToResetTo => _storage.CountToResetTo;

    public StorageType Type => _storage.Type;
    public int Capacity => _storage.Capacity;
    public int FreeSlots => _storage.FreeSlots;
    public int TakenSlots => _storage.TakenSlots;
    public IItem? this[int index] => _storage[index];

    public ItemContainer(StorageType type, int capacity, Action<HashSet<int>?>? publishChanges = null,
        int countToResetTo = -1)
    {
        _storage = new ItemContainerStorage(type, capacity, countToResetTo);
        _mutations = new ItemContainerMutationBoundary(_storage, publishChanges);
    }

    public ItemContainer(StorageType type, IEnumerable<IItem> items, int capacity,
        Action<HashSet<int>?>? publishChanges = null, int countToResetTo = -1)
    {
        _storage = new ItemContainerStorage(type, items, capacity, countToResetTo);
        _mutations = new ItemContainerMutationBoundary(_storage, publishChanges);
    }

    public IEnumerator<IItem?> GetEnumerator() => _storage.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool Add(IItem item)
    {
        if (!_storage.TryAdd(item, out var changedSlots, out var transaction)) return false;
        PublishChanges(transaction, changedSlots);
        return true;
    }

    public bool Add(int slot, IItem item)
    {
        if (!_storage.TryAdd(slot, item, out var changedSlots, out var transaction)) return false;
        PublishChanges(transaction, changedSlots);
        return true;
    }

    public IItem? GetById(int id) => _storage.GetById(id);

    public int Remove(IItem item, int preferredSlot = -1, bool update = true)
    {
        var removed = _storage.Remove(item, preferredSlot, out var changedSlots, out var transaction);
        if (removed > 0 && update) PublishChanges(transaction, changedSlots);
        return removed;
    }

    public bool TryRemoveExact(IItem item, int preferredSlot = -1)
    {
        if (!_storage.TryRemoveExact(item, preferredSlot, out var changedSlots, out var transaction)) return false;
        PublishChanges(transaction, changedSlots);
        return true;
    }

    public bool TryTransferTo(IItemContainer destination, IItem item, int count,
        int preferredSourceSlot = -1, int destinationSlot = -1, IItem? destinationItem = null)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(item);
        var destinationBoundary = ItemContainerTransaction.ResolveSingleBoundary(destination);
        return _mutations.TryTransferTo(destinationBoundary, item, count,
            preferredSourceSlot, destinationSlot, destinationItem);
    }

    public void Replace(int slot, IItem item)
    {
        _storage.Replace(slot, item, out var transaction);
        PublishChanges(transaction, [slot]);
    }

    public void Swap(int fromSlot, int toSlot)
    {
        if (_storage.Swap(fromSlot, toSlot, out var transaction)) PublishChanges(transaction, [fromSlot, toSlot]);
    }

    public void Move(int fromSlot, int toSlot)
    {
        if (_storage.Move(fromSlot, toSlot, out var transaction)) PublishChanges(transaction, null);
    }

    public bool AddRange(IEnumerable<IItem?> items)
    {
        if (!_storage.TryAddRange(items, out var changedSlots, out var transaction)) return false;
        if (changedSlots.Count > 0) PublishChanges(transaction, changedSlots);
        return true;
    }

    internal void RestoreItems(IEnumerable<(int Slot, IItem Item)> items, bool allowZeroCount = false) =>
        _storage.RestoreItems(items, allowZeroCount);

    internal IItem?[] SnapshotItems() => _storage.ToArray();

    internal void ReplaceState(IItem?[] items) => _storage.ReplaceState(items);

    internal ItemContainerTransaction? ExecuteUnderMutationLock(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        lock (_storage.MutationLock)
        {
            var transaction = _storage.CaptureMutationTransaction();
            action();
            return transaction;
        }
    }

    public bool Contains(int id, int count) => _storage.Contains(id, count);
    public bool Contains(int id) => _storage.Contains(id);
    public int GetCount(IItem item) => _storage.GetCount(item);
    public int GetCountById(int id) => _storage.GetCountById(id);
    public int GetInstanceSlot(IItem instance) => _storage.GetInstanceSlot(instance);

    public void Sort()
    {
        _storage.Sort(out var transaction);
        PublishChanges(transaction, null);
    }

    public int GetSlotByItem(IItem item, bool ignoreCount = true) => _storage.GetSlotByItem(item, ignoreCount);
    public bool HasSpaceFor(IItem item) => _storage.HasSpaceFor(item);
    public bool HasSpaceForRange(IEnumerable<IItem?> items) => _storage.HasSpaceForRange(items);

    public void Clear(bool update)
    {
        if (_storage.Clear(out var transaction) && update) PublishChanges(transaction, null);
    }

    private void PublishChanges(ItemContainerTransaction? transaction, HashSet<int>? changedSlots) =>
        _mutations.NotifyChanges(transaction, changedSlots);
}
