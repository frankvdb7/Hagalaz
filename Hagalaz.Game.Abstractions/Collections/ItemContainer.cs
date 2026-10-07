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
        using var mutation = _mutations.BeginMutation();
        if (!_storage.TryAdd(item, out var changedSlots)) return false;
        mutation.RecordChanges(changedSlots);
        return true;
    }

    public bool Add(int slot, IItem item)
    {
        using var mutation = _mutations.BeginMutation();
        if (!_storage.TryAdd(slot, item, out var changedSlots)) return false;
        mutation.RecordChanges(changedSlots);
        return true;
    }

    public IItem? GetById(int id) => _storage.GetById(id);

    public int Remove(IItem item, int preferredSlot = -1, bool publishChanges = true)
    {
        using var mutation = _mutations.BeginMutation();
        var removed = _storage.Remove(item, preferredSlot, out var changedSlots);
        if (removed > 0 && publishChanges) mutation.RecordChanges(changedSlots);
        return removed;
    }

    public bool TryRemoveExact(IItem item, int preferredSlot = -1)
    {
        using var mutation = _mutations.BeginMutation();
        if (!_storage.TryRemoveExact(item, preferredSlot, out var changedSlots)) return false;
        mutation.RecordChanges(changedSlots);
        return true;
    }

    public bool TryTransferTo(IItemContainer destination, IItem item, int count,
        int preferredSourceSlot = -1, int destinationSlot = -1, IItem? destinationItem = null)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(item);
        var destinationBoundary = ItemContainerTransaction.ResolveSingleBoundary(destination);
        var sourceLockHeld = _mutations.IsMutationLockHeldByCurrentThread;
        var destinationLockHeld = destinationBoundary.IsMutationLockHeldByCurrentThread;
        if (sourceLockHeld != destinationLockHeld)
            throw new InvalidOperationException("A transfer cannot partially participate in an item-container transaction.");

        if (sourceLockHeld)
            return _mutations.TryTransferTo(destinationBoundary, item, count,
                preferredSourceSlot, destinationSlot, destinationItem);

        using var transaction = ItemContainerTransaction.Begin(this, destination);
        if (!_mutations.TryTransferTo(destinationBoundary, item, count,
                preferredSourceSlot, destinationSlot, destinationItem)) return false;

        transaction.Commit();
        return true;
    }

    public void Replace(int slot, IItem item)
    {
        using var mutation = _mutations.BeginMutation();
        _storage.Replace(slot, item);
        mutation.RecordChanges([slot]);
    }

    public void Swap(int fromSlot, int toSlot)
    {
        using var mutation = _mutations.BeginMutation();
        if (_storage.Swap(fromSlot, toSlot)) mutation.RecordChanges([fromSlot, toSlot]);
    }

    public void Move(int fromSlot, int toSlot)
    {
        using var mutation = _mutations.BeginMutation();
        if (_storage.Move(fromSlot, toSlot)) mutation.RecordChanges(null);
    }

    public bool AddRange(IEnumerable<IItem?> items)
    {
        using var mutation = _mutations.BeginMutation();
        if (!_storage.TryAddRange(items, out var changedSlots)) return false;
        if (changedSlots.Count > 0) mutation.RecordChanges(changedSlots);
        return true;
    }

    internal void RestoreItems(IEnumerable<(int Slot, IItem Item)> items, bool allowZeroCount = false)
    {
        using var mutation = _mutations.BeginMutation();
        _storage.RestoreItems(items, allowZeroCount);
    }

    internal IItem?[] SnapshotItems() => _storage.ToArray();

    internal void ReplaceState(IItem?[] items) => _storage.ReplaceState(items);

    internal ItemContainerMutationBoundary.MutationScope BeginMutation() => _mutations.BeginMutation();

    public bool Contains(int id, int count) => _storage.Contains(id, count);
    public bool Contains(int id) => _storage.Contains(id);
    public int GetCount(IItem item) => _storage.GetCount(item);
    public int GetCountById(int id) => _storage.GetCountById(id);
    public int GetInstanceSlot(IItem instance) => _storage.GetInstanceSlot(instance);

    public void Sort()
    {
        using var mutation = _mutations.BeginMutation();
        _storage.Sort();
        mutation.RecordChanges(null);
    }

    public int GetSlotByItem(IItem item, bool ignoreCount = true) => _storage.GetSlotByItem(item, ignoreCount);
    public bool HasSpaceFor(IItem item) => _storage.HasSpaceFor(item);
    public bool HasSpaceForRange(IEnumerable<IItem?> items) => _storage.HasSpaceForRange(items);

    public void Clear(bool publishChanges)
    {
        using var mutation = _mutations.BeginMutation();
        if (_storage.Clear() && publishChanges) mutation.RecordChanges(null);
    }
}
