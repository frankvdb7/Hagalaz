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
        private readonly ITradeItemContainer _firstOffer;
        private readonly ITradeItemContainer _secondOffer;
        private readonly bool _firstReceivesSecondOffer;

        public TradeCompensation(
            TransferReceipt? firstReceipt,
            TransferReceipt? secondReceipt,
            ITradeItemContainer firstOffer,
            ITradeItemContainer secondOffer,
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
        /// Completes a failed transfer through one consistent recovery outcome.
        /// A refund restores value to its original owner; an exchange stores any
        /// remaining opposite-side value for its intended recipient.
        /// </summary>
        public bool TryConserve(ICharacter first, ICharacter second)
        {
            var firstSource = _firstReceivesSecondOffer ? _secondOffer : _firstOffer;
            var secondSource = _firstReceivesSecondOffer ? _firstOffer : _secondOffer;
            var firstMutations = new List<TradeItemMutation>();
            var secondMutations = new List<TradeItemMutation>();

            if (!TryRemoveAppliedDelta(firstSource, _firstReceipt, firstMutations) ||
                !TryRemoveAppliedDelta(secondSource, _secondReceipt, secondMutations))
            {
                var failedSecondMutationsRestored = TryRollback(secondMutations);
                var failedFirstMutationsRestored = TryRollback(firstMutations);
                if (!failedSecondMutationsRestored || !failedFirstMutationsRestored)
                {
                    return false;
                }

                return false;
            }

            if (!_firstReceivesSecondOffer &&
                TryConserveEscrow(first, _firstOffer, second, _secondOffer))
            {
                return true;
            }

            if (_firstReceivesSecondOffer && TryCompleteExchange(first, second, firstMutations, secondMutations))
            {
                return true;
            }

            // If recovery cannot complete, restore the offer mutations and leave
            // the compensation pending for another checked attempt.
            var secondMutationsRestored = TryRollback(secondMutations);
            var firstMutationsRestored = TryRollback(firstMutations);
            if (!secondMutationsRestored || !firstMutationsRestored)
            {
                return false;
            }

            return false;
        }

        private bool TryCompleteExchange(
            ICharacter first,
            ICharacter second,
            IReadOnlyList<TradeItemMutation> firstMutations,
            IReadOnlyList<TradeItemMutation> secondMutations)
        {
            RecoveryReceipt? firstRecovery = null;
            RecoveryReceipt? secondRecovery = null;
            var firstItems = SnapshotItems(_firstOffer);
            var secondItems = SnapshotItems(_secondOffer);
            var firstDestination = GetRecoveryContainer(second, firstItems);
            var secondDestination = GetRecoveryContainer(first, secondItems);
            if ((firstItems.Length > 0 && firstDestination == null) ||
                (secondItems.Length > 0 && secondDestination == null))
            {
                return false;
            }

            if (firstDestination != null &&
                !TryStoreEscrow(_firstOffer, firstDestination, out firstRecovery))
            {
                return false;
            }

            if (secondDestination != null &&
                !TryStoreEscrow(_secondOffer, secondDestination, out secondRecovery))
            {
                var firstRecoveryRestored = firstRecovery?.Rollback() ?? true;
                var secondRecoveryRestored = secondRecovery?.Rollback() ?? true;
                var secondSourceRestored = TryRollback(secondMutations);
                var firstSourceRestored = TryRollback(firstMutations);
                if (!firstRecoveryRestored ||
                    !secondRecoveryRestored ||
                    !secondSourceRestored ||
                    !firstSourceRestored)
                {
                    return false;
                }

                return false;
            }

            return true;
        }

        private static bool TryRemoveAppliedDelta(
            ITradeItemContainer source,
            TransferReceipt? receipt,
            List<TradeItemMutation> mutations)
        {
            if (receipt == null)
            {
                return true;
            }

            foreach (var item in receipt.AppliedItems)
            {
                var mutation = source.RemoveForTrade(item);
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

            var moneyMutation = source.RemoveForTrade(receipt.ItemBuilder.Create()
                .WithId(CoinsItemId)
                .WithCount(receipt.AppliedMoneyCount)
                .Build());
            mutations.Add(moneyMutation);
            return moneyMutation.Succeeded && moneyMutation.AppliedCount == receipt.AppliedMoneyCount;
        }

        private static bool TryRollback(IReadOnlyList<TradeItemMutation> mutations)
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
        ITradeItemContainer firstOffer,
        ICharacter second,
        ITradeItemContainer secondOffer,
        IItemBuilder itemBuilder) =>
        TryExchangeDetailed(first, firstOffer, second, secondOffer, itemBuilder).Status == TransferStatus.Succeeded;

    internal static TransferResult TryExchangeDetailed(
        ICharacter first,
        ITradeItemContainer firstOffer,
        ICharacter second,
        ITradeItemContainer secondOffer,
        IItemBuilder itemBuilder) =>
        TryTransfer(first, firstOffer, second, secondOffer, itemBuilder);

    public static bool TryRefund(
        ICharacter first,
        ITradeItemContainer firstOffer,
        ICharacter second,
        ITradeItemContainer secondOffer,
        IItemBuilder itemBuilder) =>
        TryRefundDetailed(first, firstOffer, second, secondOffer, itemBuilder).Status == TransferStatus.Succeeded;

    internal static TransferResult TryRefundDetailed(
        ICharacter first,
        ITradeItemContainer firstOffer,
        ICharacter second,
        ITradeItemContainer secondOffer,
        IItemBuilder itemBuilder) =>
        TryTransfer(first, firstOffer, second, secondOffer, itemBuilder, firstReceivesSecondOffer: false);

    internal static bool TryConserveEscrow(
        ICharacter first,
        ITradeItemContainer firstOffer,
        ICharacter second,
        ITradeItemContainer secondOffer)
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
                var secondRestored = secondReceipt?.Rollback() ?? true;
                var firstRestored = firstReceipt?.Rollback() ?? true;
                if (!secondRestored || !firstRestored)
                {
                    return false;
                }

                return false;
            }

            return true;
        }
        catch (InvalidOperationException)
        {
            var secondRestored = secondReceipt?.Rollback() ?? true;
            var firstRestored = firstReceipt?.Rollback() ?? true;
            if (!secondRestored || !firstRestored)
            {
                return false;
            }

            return false;
        }
    }

    internal static int RemoveMoney(
        ICharacter character,
        int requestedCount,
        out MoneyPouchMutation mutation)
    {
        mutation = character.MoneyPouch.RemoveForTrade(requestedCount);
        return mutation.AppliedCount;
    }

    internal static bool AddMoney(
        ICharacter character,
        int count,
        out MoneyPouchMutation mutation)
    {
        mutation = character.MoneyPouch.AddForTrade(count);
        return mutation.Succeeded;
    }

    internal static bool RestoreRemovedMoney(
        MoneyPouchMutation mutation)
    {
        return mutation.TryRollback();
    }

    internal static bool RemoveAddedMoney(
        MoneyPouchMutation mutation)
    {
        return mutation.TryRollback();
    }

    private static ITradeItemContainer? GetRecoveryContainer(
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
        ITradeItemContainer offer,
        ITradeItemContainer destination,
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
            var destinationMutation = destination.AddRangeForTrade(items);
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
        ITradeItemContainer firstOffer,
        ICharacter second,
        ITradeItemContainer secondOffer,
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
        ITradeItemContainer firstOffer,
        ITradeItemContainer secondOffer,
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
            var itemMutation = character.Inventory.AddRangeForTrade(nonCoinItems);
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

        var added = AddMoney(character, (int)coinCount, out var delta);
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

    private sealed class RecoveryReceipt
    {
        private readonly ITradeItemContainer _offer;
        private readonly IReadOnlyList<IItem> _items;
        private readonly List<TradeItemMutation> _offerMutations = [];
        private TradeItemMutation? _destinationMutation;

        public RecoveryReceipt(ITradeItemContainer offer, IReadOnlyList<IItem> items)
        {
            _offer = offer;
            _items = items;
        }

        public void SetDestinationMutation(TradeItemMutation mutation) => _destinationMutation = mutation;

        public bool RemoveFromOffer(int index)
        {
            var item = _items[index];
            var mutation = _offer.RemoveForTrade(item);
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
        private TradeItemMutation? _itemMutation;
        private MoneyPouchMutation _moneyMutation = MoneyPouchMutation.Empty(succeeded: true);

        public TransferReceipt(IItemBuilder itemBuilder)
        {
            ItemBuilder = itemBuilder;
        }

        public IItemBuilder ItemBuilder { get; }

        public IReadOnlyList<IItem> AppliedItems => _itemMutation?.AppliedItems ?? [];

        public int AppliedMoneyCount => _moneyMutation.AppliedCount;

        public void SetItemMutation(TradeItemMutation mutation) => _itemMutation = mutation;

        public void SetMoneyMutation(MoneyPouchMutation mutation) => _moneyMutation = mutation;

        public bool Rollback()
        {
            var itemsRestored = _itemMutation?.TryRollback() ?? true;
            var moneyRestored = _moneyMutation.TryRollback();
            return itemsRestored & moneyRestored;
        }
    }
}
