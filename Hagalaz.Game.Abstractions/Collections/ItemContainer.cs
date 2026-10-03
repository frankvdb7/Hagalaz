using System;
using System.Collections;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Implements the generic item-container contract over composed storage.</summary>
public sealed class ItemContainer : IItemContainer
{
    private readonly ItemContainerStorage _storage;
    private readonly ItemContainerMutationBoundary _mutations;

    public IItemContainerMutationBoundary Mutations => _mutations;

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
        _mutations.EnsureOutsideTransaction();
        if (!_storage.TryAdd(item, out var changedSlots)) return false;
        PublishChanges(changedSlots);
        return true;
    }

    public bool Add(int slot, IItem item)
    {
        _mutations.EnsureOutsideTransaction();
        if (!_storage.TryAdd(slot, item, out var changedSlots)) return false;
        PublishChanges(changedSlots);
        return true;
    }

    public IItem? GetById(int id) => _storage.GetById(id);

    public int Remove(IItem item, int preferredSlot = -1, bool update = true)
    {
        _mutations.EnsureOutsideTransaction();
        var removed = _storage.Remove(item, preferredSlot, out var changedSlots);
        if (removed > 0 && update) PublishChanges(changedSlots);
        return removed;
    }

    public bool TryRemoveExact(IItem item, int preferredSlot = -1)
    {
        _mutations.EnsureOutsideTransaction();
        if (!_storage.TryRemoveExact(item, preferredSlot, out var changedSlots)) return false;
        PublishChanges(changedSlots);
        return true;
    }

    public void Replace(int slot, IItem item)
    {
        _mutations.EnsureOutsideTransaction();
        _storage.Replace(slot, item);
        PublishChanges([slot]);
    }

    public void Swap(int fromSlot, int toSlot)
    {
        _mutations.EnsureOutsideTransaction();
        if (_storage.Swap(fromSlot, toSlot)) PublishChanges([fromSlot, toSlot]);
    }

    public void Move(int fromSlot, int toSlot)
    {
        _mutations.EnsureOutsideTransaction();
        if (_storage.Move(fromSlot, toSlot)) PublishChanges(null);
    }

    public bool AddRange(IEnumerable<IItem?> items)
    {
        _mutations.EnsureOutsideTransaction();
        if (!_storage.TryAddRange(items, out var changedSlots)) return false;
        if (changedSlots.Count > 0) PublishChanges(changedSlots);
        return true;
    }

    internal void RestoreItems(IEnumerable<(int Slot, IItem Item)> items, bool allowZeroCount = false) =>
        _storage.RestoreItems(items, allowZeroCount);

    internal IItem?[] SnapshotItems() => _storage.ToArray();

    internal void ReplaceState(IItem?[] items) => _storage.ReplaceState(items);

    internal void ExecuteUnderMutationLock(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        _storage.EnsureMutationAccess();
        lock (_storage.MutationLock)
        {
            action();
        }
    }

    public bool Contains(int id, int count) => _storage.Contains(id, count);
    public bool Contains(int id) => _storage.Contains(id);
    public int GetCount(IItem item) => _storage.GetCount(item);
    public int GetCountById(int id) => _storage.GetCountById(id);
    public int GetInstanceSlot(IItem instance) => _storage.GetInstanceSlot(instance);

    public void Sort()
    {
        _mutations.EnsureOutsideTransaction();
        _storage.Sort();
        PublishChanges(null);
    }

    public int GetSlotByItem(IItem item, bool ignoreCount = true) => _storage.GetSlotByItem(item, ignoreCount);
    public bool HasSpaceFor(IItem item) => _storage.HasSpaceFor(item);
    public bool HasSpaceForRange(IEnumerable<IItem?> items) => _storage.HasSpaceForRange(items);

    public void Clear(bool update)
    {
        _mutations.EnsureOutsideTransaction();
        if (_storage.Clear() && update) PublishChanges(null);
    }

    private void PublishChanges(HashSet<int>? changedSlots) => _mutations.NotifyChanges(changedSlots);
}
