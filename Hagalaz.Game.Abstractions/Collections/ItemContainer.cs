using System;
using System.Collections;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Implements the generic item-container contract over composed storage.</summary>
public sealed class ItemContainer : IItemContainer, IItemContainerStorageOwner
{
    private readonly ItemContainerStorage _storage;
    private readonly Action<HashSet<int>?>? _publishChanges;

    ItemContainerStorage IItemContainerStorageOwner.Storage => _storage;

    public StorageType Type => _storage.Type;
    public int Capacity => _storage.Capacity;
    public int FreeSlots => _storage.FreeSlots;
    public int TakenSlots => _storage.TakenSlots;
    public IItem? this[int index] => _storage[index];

    public ItemContainer(StorageType type, int capacity, Action<HashSet<int>?>? publishChanges = null,
        int countToResetTo = -1)
    {
        _storage = new ItemContainerStorage(type, capacity, countToResetTo);
        _publishChanges = publishChanges;
    }

    public ItemContainer(StorageType type, IEnumerable<IItem> items, int capacity,
        Action<HashSet<int>?>? publishChanges = null, int countToResetTo = -1)
    {
        _storage = new ItemContainerStorage(type, items, capacity, countToResetTo);
        _publishChanges = publishChanges;
    }

    public IEnumerator<IItem?> GetEnumerator() => _storage.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool Add(IItem item)
    {
        if (!_storage.TryAdd(item, out var changedSlots)) return false;
        PublishChanges(changedSlots);
        return true;
    }

    public bool Add(int slot, IItem item)
    {
        if (!_storage.TryAdd(slot, item, out var changedSlots)) return false;
        PublishChanges(changedSlots);
        return true;
    }

    public void AddAndRemoveFrom(IItemContainer source) => ItemContainerTransfer.AddAndRemoveFrom(this, source);
    public IItem? GetById(int id) => _storage.GetById(id);

    public int Remove(IItem item, int preferredSlot = -1, bool update = true)
    {
        var removed = _storage.Remove(item, preferredSlot, out var changedSlots);
        if (removed > 0 && update) PublishChanges(changedSlots);
        return removed;
    }

    public bool TryRemoveExact(IItem item, int preferredSlot = -1)
    {
        if (!_storage.TryRemoveExact(item, preferredSlot, out var changedSlots)) return false;
        PublishChanges(changedSlots);
        return true;
    }

    public void Replace(int slot, IItem item)
    {
        _storage.Replace(slot, item);
        PublishChanges([slot]);
    }

    public void Swap(int fromSlot, int toSlot)
    {
        if (_storage.Swap(fromSlot, toSlot)) PublishChanges([fromSlot, toSlot]);
    }

    public void Move(int fromSlot, int toSlot)
    {
        if (_storage.Move(fromSlot, toSlot)) PublishChanges(null);
    }

    public bool AddRange(IEnumerable<IItem?> items)
    {
        if (!_storage.TryAddRange(items, out var changedSlots)) return false;
        PublishChanges(changedSlots);
        return true;
    }

    public bool Contains(int id, int count) => _storage.Contains(id, count);
    public bool Contains(int id) => _storage.Contains(id);
    public int GetCount(IItem item) => _storage.GetCount(item);
    public int GetCountById(int id) => _storage.GetCountById(id);
    public int GetInstanceSlot(IItem instance) => _storage.GetInstanceSlot(instance);

    public void Sort()
    {
        _storage.Sort();
        PublishChanges(null);
    }

    public int GetSlotByItem(IItem item, bool ignoreCount = true) => _storage.GetSlotByItem(item, ignoreCount);
    public bool HasSpaceFor(IItem item) => _storage.HasSpaceFor(item);
    public bool HasSpaceForRange(IEnumerable<IItem?> items) => _storage.HasSpaceForRange(items);

    public void Clear(bool update)
    {
        if (_storage.Clear() && update) PublishChanges(null);
    }

    void IItemContainerStorageOwner.PublishChanges(HashSet<int>? changedSlots) => PublishChanges(changedSlots);

    private void PublishChanges(HashSet<int>? changedSlots) => _publishChanges?.Invoke(changedSlots);
}
