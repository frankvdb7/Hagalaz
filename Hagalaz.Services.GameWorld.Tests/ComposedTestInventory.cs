using System.Collections;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Services.GameWorld.Tests;

internal sealed class ComposedTestInventory : IInventoryContainer, IItemContainerStorageOwner
{
    private readonly ItemContainerStorage _storage;

    ItemContainerStorage IItemContainerStorageOwner.Storage => _storage;
    void IItemContainerStorageOwner.PublishChanges(HashSet<int>? changedSlots) => OnUpdate(changedSlots);

    public Action? OnUpdateAction { get; set; }
    public ComposedTestInventory(int capacity) => _storage = new ItemContainerStorage(StorageType.Normal, capacity);
    public StorageType Type => _storage.Type;
    public int FreeSlots => _storage.FreeSlots;
    public int TakenSlots => _storage.TakenSlots;
    public IItem? this[int index] => _storage[index];
    public int Capacity => _storage.Capacity;
    public IEnumerator<IItem?> GetEnumerator() => _storage.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    public bool Add(IItem item) { if (!_storage.TryAdd(item, out var slots)) return false; OnUpdate(slots); return true; }
    public bool Add(int slot, IItem item) { if (!_storage.TryAdd(slot, item, out var slots)) return false; OnUpdate(slots); return true; }
    public void AddAndRemoveFrom(IItemContainer source) => ItemContainerTransfer.AddAndRemoveFrom(this, source);
    public IItem? GetById(int id) => _storage.GetById(id);
    public int Remove(IItem item, int preferredSlot = -1, bool update = true)
    {
        var removed = _storage.Remove(item, preferredSlot, out var slots);
        if (removed > 0 && update) OnUpdate(slots);
        return removed;
    }
    public void Replace(int slot, IItem item) { _storage.Replace(slot, item); OnUpdate([slot]); }
    public void Swap(int fromSlot, int toSlot) { if (_storage.Swap(fromSlot, toSlot)) OnUpdate([fromSlot, toSlot]); }
    public void Move(int fromSlot, int toSlot) { if (_storage.Move(fromSlot, toSlot)) OnUpdate(null); }
    public bool AddRange(IEnumerable<IItem?> items) { if (!_storage.TryAddRange(items, out var slots)) return false; OnUpdate(slots); return true; }
    public bool Contains(int id, int count) => _storage.Contains(id, count);
    public bool Contains(int id) => _storage.Contains(id);
    public int GetCount(IItem item) => _storage.GetCount(item);
    public int GetCountById(int id) => _storage.GetCountById(id);
    public int GetInstanceSlot(IItem instance) => _storage.GetInstanceSlot(instance);
    public void Sort() { _storage.Sort(); OnUpdate(null); }
    public int GetSlotByItem(IItem item, bool ignoreCount = true) => _storage.GetSlotByItem(item, ignoreCount);
    public bool HasSpaceFor(IItem item) => _storage.HasSpaceFor(item);
    public bool HasSpaceForRange(IEnumerable<IItem?> items) => _storage.HasSpaceForRange(items);
    public void Clear(bool update) { if (_storage.Clear() && update) OnUpdate(null); }
    public bool AddRangeForTrade(IEnumerable<IItem?> items) { if (!TryAddRangeForTradeStorage(items, out var slots)) return false; OnUpdate(slots); return true; }
    public bool TryAddRangeForTradeStorage(IEnumerable<IItem?> items, out HashSet<int> changedSlots) => _storage.TryAddRange(items, out changedSlots);
    public bool RemoveForTrade(IItem item, int preferredSlot = -1) { if (!TryRemoveForTradeStorage(item, preferredSlot, out var slots)) return false; OnUpdate(slots); return true; }
    public bool TryRemoveForTradeStorage(IItem item, int preferredSlot, out HashSet<int> changedSlots) => _storage.TryRemoveExact(item, preferredSlot, out changedSlots);
    public bool DropItem(IItem item) => false;
    public void OnUpdate(HashSet<int>? changedSlots = null) => OnUpdateAction?.Invoke();
}
