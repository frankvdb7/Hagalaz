global using Hagalaz.TestSupport;

using System;
using System.Collections.Generic;
using System.Threading;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Items;
using NSubstitute;

namespace Hagalaz.TestSupport;

internal sealed class ComposedTestContainer : IInventoryContainer, IRewardContainer, IItemTransactionSource,
    IItemContainerCompletionOwner
{
    private readonly ItemContainer _items;
    public IItemContainer Items => _items;
    IReadOnlyItemContainer IInventoryContainer.Items => _items;
    IReadOnlyItemContainer IRewardContainer.Items => _items;
    IReadOnlyList<ItemContainerMutationBoundary> IItemTransactionSource.Boundaries =>
        ((IItemTransactionSource)_items).Boundaries;
    public Action? OnUpdateAction { get; set; }
    public bool RejectExactRemoval { get; set; }
    public int UpdateCount { get; private set; }

    public ComposedTestContainer(int capacity) : this(StorageType.Normal, capacity) { }
    public ComposedTestContainer(StorageType type, int capacity, int countToResetTo = -1) =>
        _items = new ItemContainer(type, capacity, countToResetTo, this);

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

    public bool CanAcquireMutationLockFromOtherThread()
    {
        var mutationLock = ((IItemTransactionSource)_items).Boundaries[0].MutationLock;
        var acquired = false;
        var acquisition = new Thread(() =>
        {
            try
            {
                acquired = Monitor.TryEnter(mutationLock);
                if (acquired) Monitor.Exit(mutationLock);
            }
            catch (SynchronizationLockException) { acquired = false; }
        }) { IsBackground = true };
        acquisition.Start();
        return acquisition.Join(TimeSpan.FromSeconds(5)) && acquired;
    }

    public bool DropItem(IItem item) => false;
    public int Claim(IItem item, int count) => 0;
    public bool AddItem(IItem item) => _items.Add(item);
    public bool AddItem(int slot, IItem item) => _items.Add(slot, item);
    public bool AddItems(IEnumerable<IItem?> items) => _items.AddRange(items);

    public int RemoveItem(IItem item, int preferredSlot = -1, bool publishChanges = true)
    {
        return _items.Remove(item, preferredSlot, publishChanges);
    }

    public bool TryRemoveItemExact(IItem item, int preferredSlot = -1) =>
        !RejectExactRemoval && _items.TryRemoveExact(item, preferredSlot);

    public bool TryTransferItemTo(IItemContainer destination, IItem item, int count,
        int preferredSourceSlot = -1, int destinationSlot = -1, IItem? destinationItem = null)
    {
        return _items.TryTransferTo(destination, item, count,
            preferredSourceSlot, destinationSlot, destinationItem);
    }

    public bool TryTransferItemFrom(IItemContainer source, IItem item, int count,
        int preferredSourceSlot = -1, int destinationSlot = -1, IItem? destinationItem = null)
    {
        return source.TryTransferTo(_items, item, count,
            preferredSourceSlot, destinationSlot, destinationItem);
    }

    public void ReplaceItem(int slot, IItem item) => _items.Replace(slot, item);
    public void SwapItems(int fromSlot, int toSlot) => _items.Swap(fromSlot, toSlot);
    public void ClearItems(bool publishChanges = true) => _items.Clear(publishChanges);

    void IItemContainerCompletionOwner.PublishCommittedChanges()
    {
        var slots = _items.ChangeTracker.GetSlots();
        if (slots is { Count: 0 }) return;
        UpdateCount++;
        OnUpdateAction?.Invoke();
    }

    public bool ReceiveFromTrade(IItemContainer source, IItem item, int count, int sourceSlot) =>
        source.TryTransferTo(_items, item, count, sourceSlot);

    public void SetItem(int slot, IItem item) => _items.Replace(slot, item);

    void IItemContainerCompletionOwner.DiscardPendingCompletion() { }
}
