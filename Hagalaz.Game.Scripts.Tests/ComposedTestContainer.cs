using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Items;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests;

internal class ComposedTestContainer : IInventoryContainer, IRewardContainer, IItemContainerStorageOwner
{
    private readonly ItemContainerStorage _storage;

    ItemContainerStorage IItemContainerStorageOwner.Storage => _storage;
    void IItemContainerStorageOwner.PublishChanges(HashSet<int>? changedSlots) => OnUpdate(changedSlots);

    public Action? OnUpdateAction { get; set; }
    public int UpdateCount { get; private set; }
    public bool FailTradeAdd { get; set; }
    public object MutationLock => _storage.MutationLock;
    public long MutationOrder => _storage.MutationOrder;

    public ComposedTestContainer(int capacity) : this(StorageType.Normal, capacity) { }
    public ComposedTestContainer(StorageType type, int capacity, int countToResetTo = -1) =>
        _storage = new ItemContainerStorage(type, capacity, countToResetTo);

    public static IItem CreateTestItem(int id, int count = 1)
    {
        var item = Substitute.For<IItem>();
        item.Id.Returns(id);
        item.Count.Returns(count);
        var definition = Substitute.For<IItemDefinition>();
        definition.Stackable.Returns(false);
        item.ItemDefinition.Returns(definition);
        item.Equals(Arg.Any<IItem>(), Arg.Any<bool>()).Returns(call =>
            ReferenceEquals(item, call.ArgAt<IItem>(0)));
        item.Clone().Returns(item);
        item.Clone(Arg.Any<int>()).Returns(item);
        return item;
    }

    public IItem? this[int index] => _storage[index];
    public int Capacity => _storage.Capacity;
    public StorageType Type => _storage.Type;
    public int FreeSlots => _storage.FreeSlots;
    public int TakenSlots => _storage.TakenSlots;
    public IEnumerator<IItem?> GetEnumerator() => _storage.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    public bool Add(IItem item)
    {
        if (!_storage.TryAdd(item, out var slots)) return false;
        OnUpdate(slots);
        return true;
    }
    public bool Add(int slot, IItem item)
    {
        if (!_storage.TryAdd(slot, item, out var slots)) return false;
        OnUpdate(slots);
        return true;
    }
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
    public bool AddRange(IEnumerable<IItem?> items)
    {
        if (!_storage.TryAddRange(items, out var slots)) return false;
        OnUpdate(slots);
        return true;
    }
    public bool AddRangeForTrade(IEnumerable<IItem?> items)
    {
        if (!TryAddRangeForTradeStorage(items, out var slots)) return false;
        OnUpdate(slots);
        return true;
    }
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
    public bool RemoveForTrade(IItem item, int preferredSlot = -1)
    {
        if (!TryRemoveForTradeStorage(item, preferredSlot, out var slots)) return false;
        OnUpdate(slots);
        return true;
    }
    public bool TryRemoveForTradeStorage(IItem item, int preferredSlot, out HashSet<int> changedSlots) =>
        _storage.TryRemoveExact(item, preferredSlot, out changedSlots);
    public bool DropItem(IItem item) => false;
    public int Claim(IItem item, int count) => 0;
    public void OnUpdate(HashSet<int>? changedSlots = null)
    {
        UpdateCount++;
        OnUpdateAction?.Invoke();
    }

    public bool TryAddRangeForTradeStorage(IEnumerable<IItem?> items, out HashSet<int> changedSlots)
    {
        if (FailTradeAdd)
        {
            FailTradeAdd = false;
            changedSlots = [];
            return false;
        }

        return _storage.TryAddRange(items, out changedSlots);
    }

    public void SetItem(int slot, IItem item) => _storage.Replace(slot, item);

}
