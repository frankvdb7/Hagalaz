using System.Collections;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters;

public partial class RewardContainer
{
    public IItem? this[int index] => _storage[index];
    public int Capacity => _storage.Capacity;
    public StorageType Type => _storage.Type;
    public int FreeSlots => _storage.FreeSlots;
    public int TakenSlots => _storage.TakenSlots;
    public bool Add(IItem item) { if (!_storage.TryAdd(item, out var s)) return false; OnUpdate(s); return true; }
    public bool Add(int slot, IItem item) { if (!_storage.TryAdd(slot, item, out var s)) return false; OnUpdate(s); return true; }
    public void AddAndRemoveFrom(IItemContainer container) => ItemContainerTransfer.AddAndRemoveFrom(this, container);
    public IItem? GetById(int id) => _storage.GetById(id);
    public int Remove(IItem item, int preferredSlot = -1, bool update = true) { var n = _storage.Remove(item, preferredSlot, out var s); if (n > 0 && update) OnUpdate(s); return n; }
    public void Replace(int slot, IItem item) { _storage.Replace(slot, item); OnUpdate([slot]); }
    public void Swap(int fromSlot, int toSlot) { if (_storage.Swap(fromSlot, toSlot)) OnUpdate([fromSlot, toSlot]); }
    public void Move(int fromSlot, int toSlot) { if (_storage.Move(fromSlot, toSlot)) OnUpdate((HashSet<int>?)null); }
    public bool AddRange(IEnumerable<IItem?> items) { if (!_storage.TryAddRange(items, out var s)) return false; OnUpdate(s); return true; }
    public bool Contains(int id, int count) => _storage.Contains(id, count);
    public bool Contains(int id) => _storage.Contains(id);
    public int GetCount(IItem item) => _storage.GetCount(item);
    public int GetCountById(int id) => _storage.GetCountById(id);
    public int GetInstanceSlot(IItem item) => _storage.GetInstanceSlot(item);
    public void Sort() { _storage.Sort(); OnUpdate((HashSet<int>?)null); }
    public int GetSlotByItem(IItem item, bool ignoreCount = true) => _storage.GetSlotByItem(item, ignoreCount);
    public bool HasSpaceFor(IItem item) => _storage.HasSpaceFor(item);
    public bool HasSpaceForRange(IEnumerable<IItem?> items) => _storage.HasSpaceForRange(items);
    public void Clear(bool update) { if (_storage.Clear() && update) OnUpdate(); }
    public IEnumerator<IItem?> GetEnumerator() => _storage.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public bool AddRangeForTrade(IEnumerable<IItem?> items) { if (!TryAddRangeForTradeStorage(items, out var s)) return false; OnUpdate(s); return true; }
    public bool TryAddRangeForTradeStorage(IEnumerable<IItem?> items, out HashSet<int> changedSlots) => _storage.TryAddRange(items, out changedSlots);
    public bool RemoveForTrade(IItem item, int preferredSlot = -1) { if (!TryRemoveForTradeStorage(item, preferredSlot, out var s)) return false; OnUpdate(s); return true; }
    public bool TryRemoveForTradeStorage(IItem item, int preferredSlot, out HashSet<int> changedSlots) => _storage.TryRemoveExact(item, item.Count, preferredSlot, out changedSlots);
}

public partial class FamiliarInventoryContainer
{
    public IItem? this[int index] => _storage[index];
    public int Capacity => _storage.Capacity;
    public StorageType Type => _storage.Type;
    public int FreeSlots => _storage.FreeSlots;
    public int TakenSlots => _storage.TakenSlots;
    public bool Add(IItem item) { if (!_storage.TryAdd(item, out var s)) return false; OnUpdate(s); return true; }
    public bool Add(int slot, IItem item) { if (!_storage.TryAdd(slot, item, out var s)) return false; OnUpdate(s); return true; }
    public void AddAndRemoveFrom(IItemContainer container) => ItemContainerTransfer.AddAndRemoveFrom(this, container);
    public IItem? GetById(int id) => _storage.GetById(id);
    public int Remove(IItem item, int preferredSlot = -1, bool update = true) { var n = _storage.Remove(item, preferredSlot, out var s); if (n > 0 && update) OnUpdate(s); return n; }
    public void Replace(int slot, IItem item) { _storage.Replace(slot, item); OnUpdate([slot]); }
    public void Swap(int fromSlot, int toSlot) { if (_storage.Swap(fromSlot, toSlot)) OnUpdate([fromSlot, toSlot]); }
    public void Move(int fromSlot, int toSlot) { if (_storage.Move(fromSlot, toSlot)) OnUpdate(); }
    public bool AddRange(IEnumerable<IItem?> items) { if (!_storage.TryAddRange(items, out var s)) return false; OnUpdate(s); return true; }
    public bool Contains(int id, int count) => _storage.Contains(id, count);
    public bool Contains(int id) => _storage.Contains(id);
    public int GetCount(IItem item) => _storage.GetCount(item);
    public int GetCountById(int id) => _storage.GetCountById(id);
    public int GetInstanceSlot(IItem item) => _storage.GetInstanceSlot(item);
    public void Sort() { _storage.Sort(); OnUpdate(); }
    public int GetSlotByItem(IItem item, bool ignoreCount = true) => _storage.GetSlotByItem(item, ignoreCount);
    public bool HasSpaceFor(IItem item) => _storage.HasSpaceFor(item);
    public bool HasSpaceForRange(IEnumerable<IItem?> items) => _storage.HasSpaceForRange(items);
    public void Clear(bool update) { if (_storage.Clear() && update) OnUpdate(); }
    public IEnumerator<IItem?> GetEnumerator() => _storage.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public partial class BankContainer
{
    public IItem? this[int index] => _storage[index];
    public int Capacity => _storage.Capacity;
    public StorageType Type => _storage.Type;
    public int FreeSlots => _storage.FreeSlots;
    public int TakenSlots => _storage.TakenSlots;
    public bool Add(IItem item) { if (!_storage.TryAdd(item, out var s)) return false; OnUpdate(s); return true; }
    public bool Add(int slot, IItem item) { if (!_storage.TryAdd(slot, item, out var s)) return false; OnUpdate(s); return true; }
    public void AddAndRemoveFrom(IItemContainer container) => ItemContainerTransfer.AddAndRemoveFrom(this, container);
    public IItem? GetById(int id) => _storage.GetById(id);
    public int Remove(IItem item, int preferredSlot = -1, bool update = true) { var n = _storage.Remove(item, preferredSlot, out var s); if (n > 0 && update) OnUpdate(s); return n; }
    public void Replace(int slot, IItem item) { _storage.Replace(slot, item); OnUpdate([slot]); }
    public void Swap(int fromSlot, int toSlot) { if (_storage.Swap(fromSlot, toSlot)) OnUpdate([fromSlot, toSlot]); }
    public void Move(int fromSlot, int toSlot) { if (_storage.Move(fromSlot, toSlot)) OnUpdate(); }
    public bool AddRange(IEnumerable<IItem?> items) { if (!_storage.TryAddRange(items, out var s)) return false; OnUpdate(s); return true; }
    public bool Contains(int id, int count) => _storage.Contains(id, count);
    public bool Contains(int id) => _storage.Contains(id);
    public int GetCount(IItem item) => _storage.GetCount(item);
    public int GetCountById(int id) => _storage.GetCountById(id);
    public int GetInstanceSlot(IItem item) => _storage.GetInstanceSlot(item);
    public void Sort() { _storage.Sort(); OnUpdate(); }
    public int GetSlotByItem(IItem item, bool ignoreCount = true) => _storage.GetSlotByItem(item, ignoreCount);
    public bool HasSpaceFor(IItem item) => _storage.HasSpaceFor(item);
    public bool HasSpaceForRange(IEnumerable<IItem?> items) => _storage.HasSpaceForRange(items);
    public void Clear(bool update) { if (_storage.Clear() && update) OnUpdate(); }
    public IEnumerator<IItem?> GetEnumerator() => _storage.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public bool AddRangeForTrade(IEnumerable<IItem?> items) { if (!TryAddRangeForTradeStorage(items, out var s)) return false; OnUpdate(s); return true; }
    public bool TryAddRangeForTradeStorage(IEnumerable<IItem?> items, out HashSet<int> changedSlots) => _storage.TryAddRange(items, out changedSlots);
    public bool RemoveForTrade(IItem item, int preferredSlot = -1) { if (!TryRemoveForTradeStorage(item, preferredSlot, out var s)) return false; OnUpdate(s); return true; }
    public bool TryRemoveForTradeStorage(IItem item, int preferredSlot, out HashSet<int> changedSlots) => _storage.TryRemoveExact(item, item.Count, preferredSlot, out changedSlots);
}

public partial class MoneyPouchContainer
{
    public IItem? this[int index] => _storage[index];
    public int Capacity => _storage.Capacity;
    public StorageType Type => _storage.Type;
    public int FreeSlots => _storage.FreeSlots;
    public int TakenSlots => _storage.TakenSlots;
    public bool Add(IItem item) { if (!_storage.TryAdd(item, out var s)) return false; OnUpdate(s); return true; }
    public bool Add(int slot, IItem item) { if (!_storage.TryAdd(slot, item, out var s)) return false; OnUpdate(s); return true; }
    public void AddAndRemoveFrom(IItemContainer container) => ItemContainerTransfer.AddAndRemoveFrom(this, container);
    public IItem? GetById(int id) => _storage.GetById(id);
    public int Remove(IItem item, int preferredSlot = -1, bool update = true) { var n = _storage.Remove(item, preferredSlot, out var s); if (n > 0 && update) OnUpdate(s); return n; }
    public void Replace(int slot, IItem item) { _storage.Replace(slot, item); OnUpdate([slot]); }
    public void Swap(int fromSlot, int toSlot) { if (_storage.Swap(fromSlot, toSlot)) OnUpdate([fromSlot, toSlot]); }
    public void Move(int fromSlot, int toSlot) { if (_storage.Move(fromSlot, toSlot)) OnUpdate(); }
    public bool AddRange(IEnumerable<IItem?> items) { if (!_storage.TryAddRange(items, out var s)) return false; OnUpdate(s); return true; }
    public bool Contains(int id) => _storage.Contains(id);
    public int GetCount(IItem item) => _storage.GetCount(item);
    public int GetCountById(int id) => _storage.GetCountById(id);
    public int GetInstanceSlot(IItem item) => _storage.GetInstanceSlot(item);
    public void Sort() { _storage.Sort(); OnUpdate(); }
    public int GetSlotByItem(IItem item, bool ignoreCount = true) => _storage.GetSlotByItem(item, ignoreCount);
    public bool HasSpaceFor(IItem item) => _storage.HasSpaceFor(item);
    public bool HasSpaceForRange(IEnumerable<IItem?> items) => _storage.HasSpaceForRange(items);
    public void Clear(bool update) { if (_storage.Clear() && update) OnUpdate(); }
    public IEnumerator<IItem?> GetEnumerator() => _storage.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public partial class EquipmentContainer
{
    IItem? IContainer<IItem?>.this[int index] => _storage[index];
    int IItemContainer.GetInstanceSlot(IItem instance) => _storage.GetInstanceSlot(instance);
    public int Capacity => _storage.Capacity;
    public StorageType Type => _storage.Type;
    public int FreeSlots => _storage.FreeSlots;
    public int TakenSlots => _storage.TakenSlots;
    public bool Add(IItem item) { if (!_storage.TryAdd(item, out var s)) return false; OnUpdate(s); return true; }
    public bool Add(int slot, IItem item) { if (!_storage.TryAdd(slot, item, out var s)) return false; OnUpdate(s); return true; }
    public void AddAndRemoveFrom(IItemContainer container) => ItemContainerTransfer.AddAndRemoveFrom(this, container);
    public IItem? GetById(int id) => _storage.GetById(id);
    public int Remove(IItem item, int preferredSlot = -1, bool update = true) => Remove(item, (EquipmentSlot)preferredSlot, update);
    public void Replace(int slot, IItem item) => Replace((EquipmentSlot)slot, item);
    public void Swap(int fromSlot, int toSlot) { if (_storage.Swap(fromSlot, toSlot)) OnUpdate([fromSlot, toSlot]); }
    public void Move(int fromSlot, int toSlot) { if (_storage.Move(fromSlot, toSlot)) OnUpdate((HashSet<int>?)null); }
    public bool AddRange(IEnumerable<IItem?> items) { if (!_storage.TryAddRange(items, out var s)) return false; OnUpdate(s); return true; }
    public bool Contains(int id, int count) => _storage.Contains(id, count);
    public bool Contains(int id) => _storage.Contains(id);
    public int GetCount(IItem item) => _storage.GetCount(item);
    public int GetCountById(int id) => _storage.GetCountById(id);
    public void Sort() { _storage.Sort(); OnUpdate((HashSet<int>?)null); }
    public int GetSlotByItem(IItem item, bool ignoreCount = true) => _storage.GetSlotByItem(item, ignoreCount);
    public bool HasSpaceFor(IItem item) => _storage.HasSpaceFor(item);
    public bool HasSpaceForRange(IEnumerable<IItem?> items) => _storage.HasSpaceForRange(items);
    public IEnumerator<IItem?> GetEnumerator() => _storage.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
