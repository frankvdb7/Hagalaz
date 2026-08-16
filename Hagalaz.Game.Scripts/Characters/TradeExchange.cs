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

    internal enum TransferStatus
    {
        Succeeded,
        Failed,
        CompensationIncomplete
    }

    internal sealed class TransferResult
    {
        private TransferResult(TransferStatus status, TradeCompensation? compensation = null)
        {
            Status = status;
            Compensation = compensation;
        }

        public TransferStatus Status { get; }

        public TradeCompensation? Compensation { get; }

        public static TransferResult Succeeded() => new(TransferStatus.Succeeded);

        public static TransferResult Failed() => new(TransferStatus.Failed);

        public static TransferResult CompensationIncomplete(TradeCompensation compensation) =>
            new(TransferStatus.CompensationIncomplete, compensation);
    }

    internal sealed class TradeCompensation
    {
        private readonly TransferReceipt? _firstReceipt;
        private readonly TransferReceipt? _secondReceipt;

        public TradeCompensation(TransferReceipt? firstReceipt, TransferReceipt? secondReceipt)
        {
            _firstReceipt = firstReceipt;
            _secondReceipt = secondReceipt;
        }

        public bool TryCompensate()
        {
            var secondCompensated = Rollback(_secondReceipt, includeAttemptedItems: true);
            var firstCompensated = Rollback(_firstReceipt, includeAttemptedItems: true);
            return secondCompensated & firstCompensated;
        }
    }

    public static bool TryExchange(
        ICharacter first,
        IItemContainer firstOffer,
        ICharacter second,
        IItemContainer secondOffer,
        IItemBuilder itemBuilder) =>
        TryExchangeDetailed(first, firstOffer, second, secondOffer, itemBuilder).Status == TransferStatus.Succeeded;

    internal static TransferResult TryExchangeDetailed(
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
        TryRefundDetailed(first, firstOffer, second, secondOffer, itemBuilder).Status == TransferStatus.Succeeded;

    internal static TransferResult TryRefundDetailed(
        ICharacter first,
        IItemContainer firstOffer,
        ICharacter second,
        IItemContainer secondOffer,
        IItemBuilder itemBuilder) =>
        TryTransfer(first, firstOffer, second, secondOffer, itemBuilder, firstReceivesSecondOffer: false);

    internal static bool TryConserveEscrow(
        ICharacter first,
        IItemContainer firstOffer,
        ICharacter second,
        IItemContainer secondOffer)
    {
        RecoveryReceipt? firstReceipt = null;
        RecoveryReceipt? secondReceipt = null;
        try
        {
            var firstItems = SnapshotItems(firstOffer);
            var secondItems = SnapshotItems(secondOffer);
            var firstDestination = GetRecoveryContainer(first, firstItems);
            var secondDestination = GetRecoveryContainer(second, secondItems);
            if ((firstItems.Length > 0 && firstDestination == null) ||
                (secondItems.Length > 0 && secondDestination == null))
            {
                return false;
            }

            if (firstDestination != null && !TryStoreEscrow(firstOffer, firstDestination, out firstReceipt))
            {
                return false;
            }

            if (secondDestination != null && !TryStoreEscrow(secondOffer, secondDestination, out secondReceipt))
            {
                if (secondReceipt != null && !secondReceipt.Rollback())
                {
                    return false;
                }

                if (firstReceipt != null && !firstReceipt.Rollback())
                {
                    return false;
                }

                return false;
            }

            return true;
        }
        catch (InvalidOperationException)
        {
            if (secondReceipt != null && !secondReceipt.Rollback())
            {
                return false;
            }

            if (firstReceipt != null && !firstReceipt.Rollback())
            {
                return false;
            }

            return false;
        }
    }

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
        return TryRemoveAddedMoney(character, delta, itemBuilder, out _);
    }

    private static bool TryRemoveAddedMoney(
        ICharacter character,
        MoneyDelta delta,
        IItemBuilder itemBuilder,
        out MoneyDelta remaining)
    {
        var remainingInventory = delta.InventoryCount;
        if (delta.InventoryCount > 0)
        {
            var coins = itemBuilder.Create().WithId(CoinsItemId).WithCount(delta.InventoryCount).Build();
            var before = character.Inventory.GetCountById(CoinsItemId);
            try
            {
                var removed = character.Inventory.Remove(coins);
                remainingInventory = Math.Max(0, delta.InventoryCount - Math.Min(delta.InventoryCount, removed));
            }
            catch (InvalidOperationException)
            {
                var after = character.Inventory.GetCountById(CoinsItemId);
                var removed = Math.Max(0, before - after);
                remainingInventory = Math.Max(0, delta.InventoryCount - Math.Min(delta.InventoryCount, removed));
            }
        }

        var remainingPouch = delta.PouchCount;
        if (delta.PouchCount > 0)
        {
            var before = character.MoneyPouch.Count;
            try
            {
                var removed = character.MoneyPouch.Remove(delta.PouchCount);
                remainingPouch = Math.Max(0, delta.PouchCount - Math.Min(delta.PouchCount, removed));
            }
            catch (InvalidOperationException)
            {
                remainingPouch = Math.Max(0, delta.PouchCount - Math.Max(0, before - character.MoneyPouch.Count));
            }
        }

        remaining = new(remainingPouch, remainingInventory);
        return !remaining.HasChanges;
    }

    internal static bool RemoveExact(IItemContainer container, IItem item)
    {
        try
        {
            return container.Remove(item.Clone(), update: false) == item.Count;
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

    private static IItemContainer? GetRecoveryContainer(
        ICharacter character,
        IReadOnlyList<IItem> items)
    {
        if (items.Count == 0)
        {
            return character.Rewards;
        }

        if (character.Rewards != null && character.Rewards.HasSpaceForRange(items))
        {
            return character.Rewards;
        }

        return character.Bank != null && character.Bank.HasSpaceForRange(items)
            ? character.Bank
            : null;
    }

    private static bool TryStoreEscrow(
        IItemContainer offer,
        IItemContainer destination,
        out RecoveryReceipt? receipt)
    {
        receipt = null;
        var items = SnapshotItems(offer);
        if (items.Length == 0)
        {
            return true;
        }

        var recoveryReceipt = new RecoveryReceipt(offer, destination, items);
        receipt = recoveryReceipt;
        try
        {
            if (!destination.HasSpaceForRange(items) || !destination.AddRange(items))
            {
                if (!recoveryReceipt.Rollback())
                {
                    return false;
                }

                return false;
            }

            for (var i = 0; i < items.Length; i++)
            {
                if (!recoveryReceipt.RemoveFromOffer(i))
                {
                    if (!recoveryReceipt.Rollback())
                    {
                        return false;
                    }

                    return false;
                }
            }

            return true;
        }
        catch (InvalidOperationException)
        {
            if (!recoveryReceipt.Rollback())
            {
                return false;
            }

            return false;
        }
    }

    private static TransferResult TryTransfer(
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
                return TransferResult.Failed();
            }

            firstReceipt = new TransferReceipt(first, itemBuilder);
            secondReceipt = new TransferReceipt(second, itemBuilder);

            if (!TryReceive(first, firstReceived, firstReceipt) ||
                !TryReceive(second, secondReceived, secondReceipt))
            {
                return CreateFailedTransferResult(firstReceipt, secondReceipt);
            }

            return TransferResult.Succeeded();
        }
        catch (InvalidOperationException)
        {
            return CreateFailedTransferResult(firstReceipt, secondReceipt);
        }
    }

    private static TransferResult CreateFailedTransferResult(
        TransferReceipt? firstReceipt,
        TransferReceipt? secondReceipt)
    {
        var compensation = new TradeCompensation(firstReceipt, secondReceipt);
        return compensation.TryCompensate()
            ? TransferResult.Failed()
            : TransferResult.CompensationIncomplete(compensation);
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

    private sealed class RecoveryReceipt
    {
        private readonly IItemContainer _offer;
        private readonly IItemContainer _destination;
        private readonly IReadOnlyList<ItemDelta> _items;
        private readonly bool[] _removedFromOffer;

        public RecoveryReceipt(IItemContainer offer, IItemContainer destination, IReadOnlyList<IItem> items)
        {
            _offer = offer;
            _destination = destination;
            _items = items
                .Select(item => new ItemDelta(item, destination.GetCount(item)))
                .ToArray();
            _removedFromOffer = new bool[_items.Count];
        }

        public bool RemoveFromOffer(int index)
        {
            var removed = RemoveExact(_offer, _items[index].Item);
            if (removed)
            {
                _removedFromOffer[index] = true;
            }

            return removed;
        }

        public bool Rollback()
        {
            var destinationRestored = true;
            foreach (var item in _items)
            {
                destinationRestored &= RollbackItemAdd(_destination, item.Item, item.CountBefore);
            }

            if (!destinationRestored)
            {
                return false;
            }

            var offerRestored = true;
            for (var i = 0; i < _items.Count; i++)
            {
                if (!_removedFromOffer[i])
                {
                    continue;
                }

                try
                {
                    var restored = _offer.Add(_items[i].Item.Clone());
                    offerRestored &= restored;
                    if (restored)
                    {
                        _removedFromOffer[i] = false;
                    }
                }
                catch (InvalidOperationException)
                {
                    offerRestored = false;
                }
            }

            return offerRestored;
        }

        private readonly record struct ItemDelta(IItem Item, int CountBefore);
    }

    internal sealed class TransferReceipt
    {
        private readonly ICharacter _recipient;
        private IReadOnlyList<ItemDelta> _items = [];
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
            _items = items
                .Select(item => new ItemDelta(item, _recipient.Inventory.GetCount(item)))
                .ToArray();
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
                    restored &= RollbackItemAdd(_recipient.Inventory, item.Item, item.CountBefore);
                }
            }

            if (_moneyDelta.PouchCount > 0 || _moneyDelta.InventoryCount > 0)
            {
                restored &= TryRemoveAddedMoney(_recipient, _moneyDelta, ItemBuilder, out _moneyDelta);
            }

            return restored;
        }

        private readonly record struct ItemDelta(IItem Item, int CountBefore);
    }
}
