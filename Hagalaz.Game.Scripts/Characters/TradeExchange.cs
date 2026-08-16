using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Characters;

/// <summary>
/// Applies checked trade transfers using the existing character containers.
/// </summary>
internal static class TradeExchange
{
    private const int CoinsItemId = 995;

    public static bool TryExchange(
        ICharacter first,
        IItemContainer firstOffer,
        ICharacter second,
        IItemContainer secondOffer,
        IItemBuilder itemBuilder) =>
        TryTransfer(first, firstOffer, second, secondOffer, itemBuilder);

    public static bool TryRefund(
        ICharacter first,
        IItemContainer firstOffer,
        ICharacter second,
        IItemContainer secondOffer,
        IItemBuilder itemBuilder) =>
        TryTransfer(first, firstOffer, second, secondOffer, itemBuilder, firstReceivesSecondOffer: false);

    internal static int RemoveMoney(
        ICharacter character,
        int requestedCount,
        out MoneyDelta delta)
    {
        var before = CaptureMoneyBalance(character);
        int removed;
        try
        {
            removed = character.MoneyPouch.Remove(requestedCount);
        }
        catch (InvalidOperationException)
        {
            delta = CaptureRemovedMoney(before, character);
            return 0;
        }

        delta = CaptureRemovedMoney(before, character);
        return removed;
    }

    internal static bool AddMoney(
        ICharacter character,
        int count,
        out MoneyDelta delta)
    {
        var before = CaptureMoneyBalance(character);
        bool added;
        try
        {
            added = character.MoneyPouch.Add(count);
        }
        catch (InvalidOperationException)
        {
            delta = CaptureAddedMoney(before, character);
            return false;
        }

        delta = CaptureAddedMoney(before, character);
        return added;
    }

    internal static bool RestoreRemovedMoney(
        ICharacter character,
        MoneyDelta delta,
        IItemBuilder itemBuilder)
    {
        var restored = true;
        if (delta.PouchCount > 0)
        {
            restored &= character.MoneyPouch.Add(delta.PouchCount);
        }

        if (delta.InventoryCount > 0)
        {
            var coins = itemBuilder.Create().WithId(CoinsItemId).WithCount(delta.InventoryCount).Build();
            restored &= character.Inventory.Add(coins);
        }

        return restored;
    }

    internal static bool RemoveAddedMoney(
        ICharacter character,
        MoneyDelta delta,
        IItemBuilder itemBuilder)
    {
        var removed = true;
        if (delta.InventoryCount > 0)
        {
            var coins = itemBuilder.Create().WithId(CoinsItemId).WithCount(delta.InventoryCount).Build();
            removed &= character.Inventory.Remove(coins) == delta.InventoryCount;
        }

        if (delta.PouchCount > 0)
        {
            removed &= character.MoneyPouch.Remove(delta.PouchCount) == delta.PouchCount;
        }

        return removed;
    }

    internal static bool RemoveExact(IItemContainer container, IItem item)
    {
        try
        {
            return container.Remove(item.Clone()) == item.Count;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    internal static bool RollbackItemAdd(IItemContainer container, IItem item, int countBefore)
    {
        var added = container.GetCount(item) - countBefore;
        if (added <= 0)
        {
            return true;
        }

        return RemoveExact(container, item.Clone(Math.Min(added, item.Count)));
    }

    private static bool TryTransfer(
        ICharacter first,
        IItemContainer firstOffer,
        ICharacter second,
        IItemContainer secondOffer,
        IItemBuilder itemBuilder,
        bool firstReceivesSecondOffer = true)
    {
        TransferReceipt? firstReceipt = null;
        TransferReceipt? secondReceipt = null;

        try
        {
            var firstItems = SnapshotItems(firstOffer);
            var secondItems = SnapshotItems(secondOffer);
            var firstReceived = firstReceivesSecondOffer ? secondItems : firstItems;
            var secondReceived = firstReceivesSecondOffer ? firstItems : secondItems;

            if (!CanReceive(first, firstReceived, itemBuilder) ||
                !CanReceive(second, secondReceived, itemBuilder))
            {
                return false;
            }

            firstReceipt = new TransferReceipt(first, itemBuilder);
            secondReceipt = new TransferReceipt(second, itemBuilder);

            if (!TryReceive(first, firstReceived, firstReceipt) ||
                !TryReceive(second, secondReceived, secondReceipt))
            {
                Rollback(secondReceipt, includeAttemptedItems: true);
                Rollback(firstReceipt, includeAttemptedItems: true);
                return false;
            }

            return true;
        }
        catch (InvalidOperationException)
        {
            Rollback(secondReceipt, includeAttemptedItems: true);
            Rollback(firstReceipt, includeAttemptedItems: true);
            return false;
        }
    }

    private static bool CanReceive(ICharacter character, IReadOnlyList<IItem> items, IItemBuilder itemBuilder)
    {
        var nonCoinItems = items.Where(item => item.Id != CoinsItemId).ToArray();
        if (nonCoinItems.Length > 0 && !character.Inventory.HasSpaceForRange(nonCoinItems))
        {
            return false;
        }

        long moneyPouchSpace = int.MaxValue - character.MoneyPouch.Count;
        long inventoryCoins = 0;
        foreach (var item in items.Where(item => item.Id == CoinsItemId))
        {
            var pouchCoins = Math.Min(moneyPouchSpace, item.Count);
            moneyPouchSpace -= pouchCoins;
            inventoryCoins += item.Count - pouchCoins;
            if (inventoryCoins > int.MaxValue)
            {
                return false;
            }
        }

        if (inventoryCoins > 0)
        {
            var coins = itemBuilder.Create().WithId(CoinsItemId).WithCount((int)inventoryCoins).Build();
            if (!character.Inventory.HasSpaceFor(coins))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryReceive(
        ICharacter character,
        IReadOnlyList<IItem> items,
        TransferReceipt receipt)
    {
        var nonCoinItems = items.Where(item => item.Id != CoinsItemId).ToArray();
        if (nonCoinItems.Length > 0)
        {
            receipt.MarkItemsAttempted(nonCoinItems);
            if (!character.Inventory.AddRange(nonCoinItems))
            {
                return false;
            }

            receipt.MarkItemsApplied();
        }

        var coinCount = items
            .Where(item => item.Id == CoinsItemId)
            .Sum(item => (long)item.Count);
        if (coinCount <= 0)
        {
            return true;
        }

        if (coinCount > int.MaxValue)
        {
            return false;
        }

        var added = AddMoney(character, (int)coinCount, out var delta);
        receipt.SetMoneyDelta(delta);
        return added;
    }

    private static bool Rollback(TransferReceipt? receipt, bool includeAttemptedItems)
    {
        try
        {
            return receipt == null || receipt.Rollback(includeAttemptedItems);
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static IItem[] SnapshotItems(IItemContainer container) =>
        container.OfType<IItem>().Select(item => item.Clone()).ToArray();

    private static MoneyBalance CaptureMoneyBalance(ICharacter character) =>
        new(character.MoneyPouch.Count, character.Inventory.GetCountById(CoinsItemId));

    private static MoneyDelta CaptureRemovedMoney(MoneyBalance before, ICharacter character)
    {
        var after = CaptureMoneyBalance(character);
        return new(
            Math.Max(0, before.PouchCount - after.PouchCount),
            Math.Max(0, before.InventoryCount - after.InventoryCount));
    }

    private static MoneyDelta CaptureAddedMoney(MoneyBalance before, ICharacter character)
    {
        var after = CaptureMoneyBalance(character);
        return new(
            Math.Max(0, after.PouchCount - before.PouchCount),
            Math.Max(0, after.InventoryCount - before.InventoryCount));
    }

    internal readonly record struct MoneyDelta(int PouchCount, int InventoryCount)
    {
        public bool HasChanges => PouchCount > 0 || InventoryCount > 0;
    }

    private readonly record struct MoneyBalance(int PouchCount, int InventoryCount);

    private sealed class TransferReceipt
    {
        private readonly ICharacter _recipient;
        private IReadOnlyList<IItem> _items = [];
        private bool _itemsApplied;
        private bool _itemsAttempted;
        private MoneyDelta _moneyDelta;

        public TransferReceipt(ICharacter recipient, IItemBuilder itemBuilder)
        {
            _recipient = recipient;
            ItemBuilder = itemBuilder;
        }

        public IItemBuilder ItemBuilder { get; }

        public void MarkItemsAttempted(IReadOnlyList<IItem> items)
        {
            _items = items;
            _itemsAttempted = true;
        }

        public void MarkItemsApplied() => _itemsApplied = true;

        public void SetMoneyDelta(MoneyDelta delta) => _moneyDelta = delta;

        public bool Rollback(bool includeAttemptedItems)
        {
            var restored = true;
            if (_itemsApplied || includeAttemptedItems && _itemsAttempted)
            {
                foreach (var item in _items)
                {
                    restored &= RemoveExact(_recipient.Inventory, item);
                }
            }

            if (_moneyDelta.PouchCount > 0 || _moneyDelta.InventoryCount > 0)
            {
                restored &= RemoveAddedMoney(_recipient, _moneyDelta, ItemBuilder);
            }

            return restored;
        }
    }
}
