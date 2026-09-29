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
