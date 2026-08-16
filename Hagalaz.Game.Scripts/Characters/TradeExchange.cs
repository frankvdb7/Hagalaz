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
        private readonly IItemContainer _firstOffer;
        private readonly IItemContainer _secondOffer;
        private readonly bool _firstReceivesSecondOffer;

        public TradeCompensation(
            TransferReceipt? firstReceipt,
            TransferReceipt? secondReceipt,
            IItemContainer firstOffer,
            IItemContainer secondOffer,
            bool firstReceivesSecondOffer)
        {
            _firstReceipt = firstReceipt;
            _secondReceipt = secondReceipt;
            _firstOffer = firstOffer;
            _secondOffer = secondOffer;
            _firstReceivesSecondOffer = firstReceivesSecondOffer;
        }

        public bool TryCompensate()
        {
            var secondCompensated = Rollback(_secondReceipt);
            var firstCompensated = Rollback(_firstReceipt);
            return secondCompensated & firstCompensated;
        }

        /// <summary>
        /// Commits the exact applied portions of a failed transfer and stores the
        /// remaining offer value in the existing recovery containers.
        /// </summary>
        public bool TryConserve(ICharacter first, ICharacter second)
        {
            var firstSource = _firstReceivesSecondOffer ? _secondOffer : _firstOffer;
            var secondSource = _firstReceivesSecondOffer ? _firstOffer : _secondOffer;
            var firstMutations = new List<ItemContainerMutation>();
            var secondMutations = new List<ItemContainerMutation>();

            if (!TryRemoveAppliedDelta(firstSource, _firstReceipt, firstMutations) ||
                !TryRemoveAppliedDelta(secondSource, _secondReceipt, secondMutations))
            {
                TryRollback(secondMutations);
                TryRollback(firstMutations);
                return false;
            }

            if (TryConserveEscrow(first, _firstOffer, second, _secondOffer))
            {
                return true;
            }

            // The recovery containers must accept the remaining escrow before the
            // source offers are changed permanently.
            if (!TryRollback(secondMutations) || !TryRollback(firstMutations))
            {
                return false;
            }

            return false;
        }

        private static bool TryRemoveAppliedDelta(
            IItemContainer source,
            TransferReceipt? receipt,
            List<ItemContainerMutation> mutations)
        {
            if (receipt == null)
            {
                return true;
            }

            foreach (var item in receipt.AppliedItems)
            {
                var mutation = source.RemoveWithMutation(item);
                mutations.Add(mutation);
                if (!mutation.Succeeded || mutation.AppliedCount != item.Count)
                {
                    return false;
                }
            }

            if (receipt.AppliedMoneyCount <= 0)
            {
                return true;
            }

            var moneyMutation = source.RemoveWithMutation(receipt.ItemBuilder.Create()
                .WithId(CoinsItemId)
                .WithCount(receipt.AppliedMoneyCount)
                .Build());
            mutations.Add(moneyMutation);
            return moneyMutation.Succeeded && moneyMutation.AppliedCount == receipt.AppliedMoneyCount;
        }

        private static bool TryRollback(IReadOnlyList<ItemContainerMutation> mutations)
        {
            var restored = true;
            for (var i = mutations.Count - 1; i >= 0; i--)
            {
                restored &= mutations[i].TryRollback();
            }

            return restored;
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
        IItemBuilder itemBuilder,
        out MoneyMutation mutation)
    {
        if (requestedCount <= 0)
        {
            mutation = MoneyMutation.Empty(succeeded: false);
            return 0;
        }

        var pouchCount = Math.Min(requestedCount, character.MoneyPouch.Count);
        var pouchMutation = pouchCount > 0
            ? character.MoneyPouch.RemoveWithMutation(CreateCoins(itemBuilder, pouchCount))
            : null;
        if (pouchMutation != null && !pouchMutation.Succeeded)
        {
            mutation = new MoneyMutation(false, pouchMutation, null);
            return 0;
        }

        var removed = pouchMutation?.AppliedCount ?? 0;
        var remaining = requestedCount - removed;
        ItemContainerMutation? inventoryMutation = null;
        if (remaining > 0)
        {
            inventoryMutation = character.Inventory.RemoveWithMutation(CreateCoins(itemBuilder, remaining));
            removed += inventoryMutation.AppliedCount;
            if (!inventoryMutation.Succeeded)
            {
                mutation = new MoneyMutation(false, pouchMutation, inventoryMutation);
                return 0;
            }
        }

        mutation = new MoneyMutation(true, pouchMutation, inventoryMutation);
        return removed;
    }

    internal static bool AddMoney(
        ICharacter character,
        int count,
        IItemBuilder itemBuilder,
        out MoneyMutation mutation)
    {
        if (count <= 0)
        {
            mutation = MoneyMutation.Empty(succeeded: false);
            return false;
        }

        var pouchCount = Math.Min(count, int.MaxValue - character.MoneyPouch.Count);
        var pouchMutation = pouchCount > 0
            ? character.MoneyPouch.AddRangeWithMutation([CreateCoins(itemBuilder, pouchCount)])
            : null;
        if (pouchMutation != null && !pouchMutation.Succeeded)
        {
            mutation = new MoneyMutation(false, pouchMutation, null);
            return false;
        }

        var added = pouchMutation?.AppliedCount ?? 0;
        var remaining = count - added;
        ItemContainerMutation? inventoryMutation = null;
        if (remaining > 0)
        {
            inventoryMutation = character.Inventory.AddRangeWithMutation([CreateCoins(itemBuilder, remaining)]);
            added += inventoryMutation.AppliedCount;
            if (!inventoryMutation.Succeeded)
            {
                mutation = new MoneyMutation(false, pouchMutation, inventoryMutation);
                return false;
            }
        }

        var succeeded = added == count;
        mutation = new MoneyMutation(succeeded, pouchMutation, inventoryMutation);
        return succeeded;
    }

    internal static bool RestoreRemovedMoney(
        MoneyMutation mutation)
    {
        return mutation.TryRollback();
    }

    internal static bool RemoveAddedMoney(
        MoneyMutation mutation)
    {
        return mutation.TryRollback();
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

        var recoveryReceipt = new RecoveryReceipt(offer, items);
        receipt = recoveryReceipt;
        try
        {
            var destinationMutation = destination.AddRangeWithMutation(items);
            recoveryReceipt.SetDestinationMutation(destinationMutation);
            if (!destinationMutation.Succeeded)
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

            firstReceipt = new TransferReceipt(itemBuilder);
            secondReceipt = new TransferReceipt(itemBuilder);

            if (!TryReceive(first, firstReceived, firstReceipt) ||
                !TryReceive(second, secondReceived, secondReceipt))
            {
                return CreateFailedTransferResult(
                    firstReceipt,
                    secondReceipt,
                    firstOffer,
                    secondOffer,
                    firstReceivesSecondOffer);
            }

            return TransferResult.Succeeded();
        }
        catch (InvalidOperationException)
        {
            return CreateFailedTransferResult(
                firstReceipt,
                secondReceipt,
                firstOffer,
                secondOffer,
                firstReceivesSecondOffer);
        }
    }

    private static TransferResult CreateFailedTransferResult(
        TransferReceipt? firstReceipt,
        TransferReceipt? secondReceipt,
        IItemContainer firstOffer,
        IItemContainer secondOffer,
        bool firstReceivesSecondOffer)
    {
        var compensation = new TradeCompensation(
            firstReceipt,
            secondReceipt,
            firstOffer,
            secondOffer,
            firstReceivesSecondOffer);
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
            var itemMutation = character.Inventory.AddRangeWithMutation(nonCoinItems);
            receipt.SetItemMutation(itemMutation);
            if (!itemMutation.Succeeded)
            {
                return false;
            }
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

        var added = AddMoney(character, (int)coinCount, receipt.ItemBuilder, out var delta);
        receipt.SetMoneyMutation(delta);
        return added;
    }

    private static bool Rollback(TransferReceipt? receipt)
    {
        try
        {
            return receipt == null || receipt.Rollback();
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static IItem[] SnapshotItems(IItemContainer container) =>
        container.OfType<IItem>().Select(item => item.Clone()).ToArray();

    private static IItem CreateCoins(IItemBuilder itemBuilder, int count) =>
        itemBuilder.Create().WithId(CoinsItemId).WithCount(count).Build();

    internal sealed class MoneyMutation
    {
        private readonly ItemContainerMutation? _pouchMutation;
        private readonly ItemContainerMutation? _inventoryMutation;

        public MoneyMutation(
            bool succeeded,
            ItemContainerMutation? pouchMutation,
            ItemContainerMutation? inventoryMutation)
        {
            Succeeded = succeeded;
            _pouchMutation = pouchMutation;
            _inventoryMutation = inventoryMutation;
        }

        public bool Succeeded { get; }

        public int PouchCount => _pouchMutation?.AppliedCount ?? 0;

        public int InventoryCount => _inventoryMutation?.AppliedCount ?? 0;

        public int AppliedCount => PouchCount + InventoryCount;

        public bool HasChanges => AppliedCount > 0;

        public bool TryRollback()
        {
            var inventoryRestored = _inventoryMutation?.TryRollback() ?? true;
            var pouchRestored = _pouchMutation?.TryRollback() ?? true;
            return inventoryRestored & pouchRestored;
        }

        public static MoneyMutation Empty(bool succeeded) => new(succeeded, null, null);
    }

    private sealed class RecoveryReceipt
    {
        private readonly IItemContainer _offer;
        private readonly IReadOnlyList<IItem> _items;
        private readonly List<ItemContainerMutation> _offerMutations = [];
        private ItemContainerMutation? _destinationMutation;

        public RecoveryReceipt(IItemContainer offer, IReadOnlyList<IItem> items)
        {
            _offer = offer;
            _items = items;
        }

        public void SetDestinationMutation(ItemContainerMutation mutation) => _destinationMutation = mutation;

        public bool RemoveFromOffer(int index)
        {
            var item = _items[index];
            var mutation = _offer.RemoveWithMutation(item);
            _offerMutations.Add(mutation);
            return mutation.Succeeded && mutation.AppliedCount == item.Count;
        }

        public bool Rollback()
        {
            var offerRestored = true;
            for (var i = _offerMutations.Count - 1; i >= 0; i--)
            {
                offerRestored &= _offerMutations[i].TryRollback();
            }

            if (!offerRestored)
            {
                return false;
            }

            return _destinationMutation?.TryRollback() ?? true;
        }
    }

    internal sealed class TransferReceipt
    {
        private ItemContainerMutation? _itemMutation;
        private MoneyMutation _moneyMutation = MoneyMutation.Empty(succeeded: true);

        public TransferReceipt(IItemBuilder itemBuilder)
        {
            ItemBuilder = itemBuilder;
        }

        public IItemBuilder ItemBuilder { get; }

        public IReadOnlyList<IItem> AppliedItems => _itemMutation?.AppliedItems ?? [];

        public int AppliedMoneyCount => _moneyMutation.AppliedCount;

        public void SetItemMutation(ItemContainerMutation mutation) => _itemMutation = mutation;

        public void SetMoneyMutation(MoneyMutation mutation) => _moneyMutation = mutation;

        public bool Rollback()
        {
            var itemsRestored = _itemMutation?.TryRollback() ?? true;
            var moneyRestored = _moneyMutation.TryRollback();
            return itemsRestored & moneyRestored;
        }
    }
}
