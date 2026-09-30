using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Characters;

/// <summary>Performs the checked terminal operations of a trade.</summary>
internal static class TradeExchange
{
    private const int CoinsItemId = 995;

    public static bool TryCompleteTrade(ICharacter first, IItemContainer firstOffer, ICharacter second,
        IItemContainer secondOffer, IItemBuilder itemBuilder) =>
        TryExchangeOffers(first, firstOffer, second, secondOffer, secondOffer, firstOffer, itemBuilder);

    public static bool TryRefundTrade(ICharacter first, IItemContainer firstOffer, ICharacter second,
        IItemContainer secondOffer, IItemBuilder itemBuilder) =>
        TryExchangeOffers(first, firstOffer, second, secondOffer, firstOffer, secondOffer, itemBuilder);

    private static bool TryExchangeOffers(ICharacter first, IItemContainer firstOffer, ICharacter second,
        IItemContainer secondOffer, IItemContainer itemsForFirst, IItemContainer itemsForSecond,
        IItemBuilder itemBuilder)
    {
        var transaction = new ItemContainerTransaction(firstOffer.Mutations, secondOffer.Mutations,
            first.Inventory.Items.Mutations, second.Inventory.Items.Mutations);
        first.MoneyPouch.EnlistIn(transaction);
        second.MoneyPouch.EnlistIn(transaction);

        var succeeded = transaction.TryExecute(tx =>
        {
            var firstItems = SnapshotItems(itemsForFirst);
            var secondItems = SnapshotItems(itemsForSecond);
            if (!CanReceive(first, firstItems, itemBuilder) || !CanReceive(second, secondItems, itemBuilder))
            {
                return false;
            }

            if (!Receive(first, firstItems, tx) || !Receive(second, secondItems, tx))
            {
                return false;
            }

            tx.Clear(firstOffer.Mutations);
            tx.Clear(secondOffer.Mutations);
            return true;
        });

        return succeeded;
    }

    /// <summary>Moves untouched escrow to existing recovery containers during forced destruction.</summary>
    internal static bool TryConserveEscrow(ICharacter first, IItemContainer firstOffer, ICharacter second,
        IItemContainer secondOffer)
    {
        var transaction = new ItemContainerTransaction(firstOffer.Mutations, secondOffer.Mutations);
        AddRecoveryBoundary(transaction, first.Rewards?.Items);
        AddRecoveryBoundary(transaction, first.Bank?.Items);
        AddRecoveryBoundary(transaction, second.Rewards?.Items);
        AddRecoveryBoundary(transaction, second.Bank?.Items);

        return transaction.TryExecute(tx =>
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

            if (firstDestination != null && firstItems.Length > 0 &&
                !tx.TryAddRange(firstDestination.Mutations, firstItems))
            {
                return false;
            }

            if (secondDestination != null && secondItems.Length > 0 &&
                !tx.TryAddRange(secondDestination.Mutations, secondItems))
            {
                return false;
            }

            tx.Clear(firstOffer.Mutations);
            tx.Clear(secondOffer.Mutations);
            return true;
        });
    }

    internal static bool TryOfferMoneyFromPouch(ICharacter character, IItemContainer offer, IItem coins)
    {
        if (coins.Count <= 0) return false;

        var transaction = new ItemContainerTransaction(offer.Mutations);
        character.MoneyPouch.EnlistIn(transaction);
        var succeeded = transaction.TryExecute(tx =>
        {
            if (!character.MoneyPouch.Contains(CoinsItemId, coins.Count) || !offer.HasSpaceFor(coins) ||
                !tx.TryAddRange(offer.Mutations, [coins]))
            {
                return false;
            }

            return character.MoneyPouch.TryStageRemoveExact(tx, coins.Count);
        });

        return succeeded;
    }

    internal static bool TryReturnMoneyToPouch(ICharacter character, IItemContainer offer, IItem coins,
        int preferredSlot)
    {
        var transaction = new ItemContainerTransaction(offer.Mutations);
        character.MoneyPouch.EnlistIn(transaction);
        var succeeded = transaction.TryExecute(tx =>
        {
            if (!tx.TryRemoveExact(offer.Mutations, coins, preferredSlot)) return false;
            return character.MoneyPouch.TryStageAddExact(tx, coins.Count);
        });

        return succeeded;
    }

    private static bool Receive(ICharacter character, IReadOnlyList<IItem> items, IItemContainerTransaction transaction)
    {
        var nonCoinItems = items.Where(item => item.Id != CoinsItemId).ToArray();
        if (nonCoinItems.Length > 0 && !transaction.TryAddRange(character.Inventory.Items.Mutations, nonCoinItems))
        {
            return false;
        }

        var coinCount = items.Where(item => item.Id == CoinsItemId).Sum(item => (long)item.Count);
        if (coinCount <= 0) return true;
        if (coinCount > int.MaxValue || !character.MoneyPouch.TryStageAddExact(transaction, (int)coinCount))
        {
            return false;
        }

        return true;
    }

    private static bool CanReceive(ICharacter character, IReadOnlyList<IItem> items, IItemBuilder itemBuilder)
    {
        var nonCoinItems = items.Where(item => item.Id != CoinsItemId).ToArray();
        var coinCount = items.Where(item => item.Id == CoinsItemId).Sum(item => (long)item.Count);
        if (coinCount > int.MaxValue) return false;

        var pouchSpace = int.MaxValue - (long)character.MoneyPouch.Count;
        var inventoryCoins = Math.Max(0, coinCount - pouchSpace);
        var recipientItems = nonCoinItems;
        if (inventoryCoins > 0)
        {
            recipientItems = nonCoinItems.Append(itemBuilder.Create().WithId(CoinsItemId)
                .WithCount((int)inventoryCoins).Build()).ToArray();
        }

        return character.Inventory.Items.HasSpaceForRange(recipientItems);
    }

    private static IItemContainer? GetRecoveryContainer(ICharacter character, IReadOnlyList<IItem> items)
    {
        if (items.Count == 0) return character.Rewards?.Items;
        if (character.Rewards != null && character.Rewards.Items.HasSpaceForRange(items))
        {
            return character.Rewards.Items;
        }

        return character.Bank != null && character.Bank.Items.HasSpaceForRange(items) ? character.Bank.Items : null;
    }

    private static IItem[] SnapshotItems(IItemContainer container) =>
        container.OfType<IItem>().Select(item => item.Clone()).ToArray();

    private static void AddRecoveryBoundary(IItemContainerTransaction transaction, IItemContainer? container)
    {
        if (container != null) transaction.Include(container.Mutations);
    }

}
