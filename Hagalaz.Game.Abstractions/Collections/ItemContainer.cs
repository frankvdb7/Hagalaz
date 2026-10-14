using System;
using System.Collections;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Implements the generic item-container contract over composed storage.</summary>
public sealed class ItemContainer : IItemContainer, IItemTransactionSource
{
    private readonly ItemContainerStorage _storage;
    private readonly ItemContainerSynchronization _synchronization;

    IReadOnlyList<ItemContainerSynchronization> IItemTransactionSource.Synchronizations => [_synchronization];

    internal int CountToResetTo => _storage.CountToResetTo;

    public StorageType Type => _storage.Type;
    public int Capacity => _storage.Capacity;
    public int FreeSlots => _storage.FreeSlots;
    public int TakenSlots => _storage.TakenSlots;
    public ItemContainerChangeTracker ChangeTracker => _synchronization.ChangeTracker;
    public IItem? this[int index] => _storage[index];

    public ItemContainer(StorageType type, int capacity, int countToResetTo = -1)
        : this(new ItemContainerSynchronization(new ItemContainerStorage(type, capacity, countToResetTo)))
    {
    }

    internal ItemContainer(ItemContainerSynchronization mutations)
    {
        ArgumentNullException.ThrowIfNull(mutations);
        _synchronization = mutations;
        _storage = mutations.Storage;
    }

    public ItemContainer(StorageType type, IEnumerable<IItem> items, int capacity, int countToResetTo = -1)
        : this(new ItemContainerSynchronization(new ItemContainerStorage(type, items, capacity, countToResetTo)))
    {
    }

    public IEnumerator<IItem?> GetEnumerator() => _storage.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool Add(IItem item)
    {
        using var mutation = _synchronization.BeginWrite();
        if (!_storage.TryAdd(item, out var changedSlots)) return false;
        mutation.RecordChanges(changedSlots);
        return true;
    }

    public bool Add(int slot, IItem item)
    {
        using var mutation = _synchronization.BeginWrite();
        if (!_storage.TryAdd(slot, item, out var changedSlots)) return false;
        mutation.RecordChanges(changedSlots);
        return true;
    }

    public IItem? GetById(int id) => _storage.GetById(id);

    public int Remove(IItem item, int preferredSlot = -1, bool publishChanges = true)
    {
        using var mutation = _synchronization.BeginWrite();
        var removed = _storage.Remove(item, preferredSlot, out var changedSlots);
        if (removed > 0 && publishChanges) mutation.RecordChanges(changedSlots);
        return removed;
    }

    public bool TryRemoveExact(IItem item, int preferredSlot = -1)
    {
        using var mutation = _synchronization.BeginWrite();
        if (!_storage.TryRemoveExact(item, preferredSlot, out var changedSlots)) return false;
        mutation.RecordChanges(changedSlots);
        return true;
    }

    public bool TryTransferTo(IItemContainer destination, IItem item, int count,
        int preferredSourceSlot = -1, int destinationSlot = -1, IItem? destinationItem = null)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(item);
        var destinationSynchronization = ItemContainerTransaction.ResolveSingleSynchronization(destination);
        return ItemContainerTransfer.TryTransferTo(_synchronization, destinationSynchronization, item, count,
            preferredSourceSlot, destinationSlot, destinationItem);
    }

    public void Replace(int slot, IItem item)
    {
        using var mutation = _synchronization.BeginWrite();
        _storage.Replace(slot, item);
        mutation.RecordChanges([slot]);
    }

    public void Swap(int fromSlot, int toSlot)
    {
        using var mutation = _synchronization.BeginWrite();
        if (_storage.Swap(fromSlot, toSlot)) mutation.RecordChanges([fromSlot, toSlot]);
    }

    public void Move(int fromSlot, int toSlot)
    {
        using var mutation = _synchronization.BeginWrite();
        if (_storage.Move(fromSlot, toSlot)) mutation.RecordChanges(null);
    }

    public bool AddRange(IEnumerable<IItem?> items)
    {
        using var mutation = _synchronization.BeginWrite();
        if (!_storage.TryAddRange(items, out var changedSlots)) return false;
        if (changedSlots.Count > 0) mutation.RecordChanges(changedSlots);
        return true;
    }

    internal void RestoreItems(IEnumerable<(int Slot, IItem Item)> items, bool allowZeroCount = false)
    {
        using var mutation = _synchronization.BeginWrite();
        _storage.RestoreItems(items, allowZeroCount);
    }

    internal IItem?[] SnapshotItems() => _storage.ToArray();

    internal void ReplaceState(IItem?[] items)
    {
        _synchronization.EnsureMutationScope();
        _storage.ReplaceState(items);
    }

    internal ItemContainerWriteScope BeginWrite() => _synchronization.BeginWrite();

    public bool Contains(int id, int count) => _storage.Contains(id, count);
    public bool Contains(int id) => _storage.Contains(id);
    public int GetCount(IItem item) => _storage.GetCount(item);
    public int GetCountById(int id) => _storage.GetCountById(id);
    public int GetInstanceSlot(IItem instance) => _storage.GetInstanceSlot(instance);

    public void Sort()
    {
        using var mutation = _synchronization.BeginWrite();
        if (_storage.Sort()) mutation.RecordChanges(null);
    }

    public int GetSlotByItem(IItem item, bool ignoreCount = true) => _storage.GetSlotByItem(item, ignoreCount);
    public bool HasSpaceFor(IItem item) => _storage.HasSpaceFor(item);
    public bool HasSpaceForRange(IEnumerable<IItem?> items) => _storage.HasSpaceForRange(items);

    public void Clear(bool publishChanges = true)
    {
        using var mutation = _synchronization.BeginWrite();
        if (_storage.Clear() && publishChanges) mutation.RecordChanges(null);
    }
}
