global using Hagalaz.TestSupport;

using System;
using System.Collections.Generic;
using System.Threading;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Items;
using NSubstitute;

namespace Hagalaz.TestSupport;

internal sealed class ComposedTestContainer : IInventoryContainer, IRewardContainer, IItemTransactionSource
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
        _items = new ItemContainer(type, capacity, countToResetTo);

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
    public bool AddItem(IItem item) => Mutate(() => _items.Add(item));
    public bool AddItem(int slot, IItem item) => Mutate(() => _items.Add(slot, item));
    public bool AddItems(IEnumerable<IItem?> items) => Mutate(() => _items.AddRange(items));

    public int RemoveItem(IItem item, int preferredSlot = -1, bool publishChanges = true)
    {
        using var transaction = BeginMutation();
        var removed = _items.Remove(item, preferredSlot, publishChanges);
        if (removed > 0) Commit(transaction);
        return removed;
    }

    public bool TryRemoveItemExact(IItem item, int preferredSlot = -1) =>
        !RejectExactRemoval && Mutate(() => _items.TryRemoveExact(item, preferredSlot));

    public bool TryTransferItemTo(IItemContainer destination, IItem item, int count,
        int preferredSourceSlot = -1, int destinationSlot = -1, IItem? destinationItem = null)
    {
        using var transaction = BeginMutation(destination);
        var transferred = _items.TryTransferTo(destination, item, count,
            preferredSourceSlot, destinationSlot, destinationItem);
        if (transferred) Commit(transaction);
        return transferred;
    }

    public bool TryTransferItemFrom(IItemContainer source, IItem item, int count,
        int preferredSourceSlot = -1, int destinationSlot = -1, IItem? destinationItem = null)
    {
        using var transaction = BeginMutation(source);
        var transferred = source.TryTransferTo(_items, item, count,
            preferredSourceSlot, destinationSlot, destinationItem);
        if (transferred) Commit(transaction);
        return transferred;
    }

    public void ReplaceItem(int slot, IItem item)
    {
        using var transaction = BeginMutation();
        _items.Replace(slot, item);
        Commit(transaction);
    }

    public void SwapItems(int fromSlot, int toSlot)
    {
        using var transaction = BeginMutation();
        _items.Swap(fromSlot, toSlot);
        Commit(transaction);
    }

    public void ClearItems(bool publishChanges = true)
    {
        using var transaction = BeginMutation();
        _items.Clear(publishChanges);
        Commit(transaction);
    }

    public void PublishChanges()
    {
        var slots = _items.ChangeTracker.GetSlots();
        if (slots is { Count: 0 }) return;
        UpdateCount++;
        OnUpdateAction?.Invoke();
    }

    public bool ReceiveFromTrade(IItemContainer source, IItem item, int count, int sourceSlot) =>
        source.TryTransferTo(_items, item, count, sourceSlot);

    public void SetItem(int slot, IItem item) => _items.Replace(slot, item);

    private bool Mutate(Func<bool> mutation)
    {
        using var transaction = BeginMutation();
        if (!mutation()) return false;
        Commit(transaction);
        return true;
    }

    private ItemContainerTransaction? BeginMutation(IItemTransactional? participant = null)
    {
        if (_items.CurrentThreadTransaction != null) return null;
        return participant == null
            ? ItemContainerTransaction.Begin(this)
            : ItemContainerTransaction.Begin(this, participant);
    }

    private void Commit(ItemContainerTransaction? transaction)
    {
        if (transaction == null) return;
        transaction.Commit();
        PublishChanges();
    }
}
