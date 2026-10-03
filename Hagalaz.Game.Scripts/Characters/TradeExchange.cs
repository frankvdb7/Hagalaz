using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Characters;

internal readonly struct TradeExchangeResult(ItemContainerTransaction transaction,
    MoneyPouchChange? firstPouchChange, MoneyPouchChange? secondPouchChange)
{
    public bool Committed => transaction.Committed;

    public void PublishChanges() => MoneyPouchChange.PublishChanges(transaction, firstPouchChange, secondPouchChange);
}

/// <summary>Performs the checked terminal operations of a trade.</summary>
internal sealed class TradeExchange
{
    private const int CoinsItemId = 995;
    private readonly IItemBuilder _itemBuilder;

    public TradeExchange(IItemBuilder itemBuilder)
    {
        ArgumentNullException.ThrowIfNull(itemBuilder);
        _itemBuilder = itemBuilder;
    }

    public TradeExchangeResult CommitTrade(ICharacter first, IItemContainer firstOffer, ICharacter second,
        IItemContainer secondOffer) =>
        CommitOffers(first, firstOffer, second, secondOffer, secondOffer, firstOffer);

    public TradeExchangeResult CommitRefund(ICharacter first, IItemContainer firstOffer, ICharacter second,
        IItemContainer secondOffer) =>
        CommitOffers(first, firstOffer, second, secondOffer, firstOffer, secondOffer);

    private TradeExchangeResult CommitOffers(ICharacter first, IItemContainer firstOffer, ICharacter second,
        IItemContainer secondOffer, IItemContainer itemsForFirst, IItemContainer itemsForSecond)
    {
        var transaction = new ItemContainerTransaction(firstOffer.Mutations, secondOffer.Mutations,
            first.Inventory.Items.Mutations, second.Inventory.Items.Mutations);
        first.MoneyPouch.Mutations.EnlistIn(transaction);
        second.MoneyPouch.Mutations.EnlistIn(transaction);

        MoneyPouchChange? firstChange = null;
        MoneyPouchChange? secondChange = null;
        transaction.TryCommit(tx =>
        {
            var firstItems = CloneOfferedItems(itemsForFirst);
            var secondItems = CloneOfferedItems(itemsForSecond);
            if (!CanReceive(first, firstItems) || !CanReceive(second, secondItems))
            {
                return false;
            }

            var firstReceived = Receive(first, firstItems, tx);
            if (!firstReceived.Received) return false;
            firstChange = firstReceived.PouchChange;
            var secondReceived = Receive(second, secondItems, tx);
            if (!secondReceived.Received) return false;
            secondChange = secondReceived.PouchChange;

            tx.Clear(firstOffer.Mutations);
            tx.Clear(secondOffer.Mutations);
            return true;
        });
        return new TradeExchangeResult(transaction, firstChange, secondChange);
    }

    /// <summary>Moves untouched escrow to existing recovery containers during forced destruction.</summary>
    internal TradeExchangeResult CommitEscrowRecovery(ICharacter first, IItemContainer firstOffer, ICharacter second,
        IItemContainer secondOffer)
    {
        var transaction = new ItemContainerTransaction(firstOffer.Mutations, secondOffer.Mutations);
        AddRecoveryBoundary(transaction, first.Rewards?.Items);
        AddRecoveryBoundary(transaction, first.Bank?.Items);
        AddRecoveryBoundary(transaction, second.Rewards?.Items);
        AddRecoveryBoundary(transaction, second.Bank?.Items);

        transaction.TryCommit(tx =>
        {
            var firstItems = CloneOfferedItems(firstOffer);
            var secondItems = CloneOfferedItems(secondOffer);
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
        return new TradeExchangeResult(transaction, null, null);
    }

    internal bool TryOfferMoneyFromPouch(ICharacter character, IItemContainer offer, IItem coins)
    {
        if (coins.Count <= 0) return false;

        var transaction = new ItemContainerTransaction(offer.Mutations);
        character.MoneyPouch.Mutations.EnlistIn(transaction);
        MoneyPouchChange? change = null;
        var succeeded = transaction.TryCommit(tx =>
        {
            if (!character.MoneyPouch.HasCoins(coins.Count) || !offer.HasSpaceFor(coins) ||
                !tx.TryAddRange(offer.Mutations, [coins]))
            {
                return false;
            }

            return (change = character.MoneyPouch.Mutations.StageRemoveExact(tx, coins.Count)) != null;
        });

        MoneyPouchChange.PublishChanges(transaction, change);
        return succeeded;
    }

    internal bool TryReturnMoneyToPouch(ICharacter character, IItemContainer offer, IItem coins,
        int preferredSlot)
    {
        var transaction = new ItemContainerTransaction(offer.Mutations);
        character.MoneyPouch.Mutations.EnlistIn(transaction);
        MoneyPouchChange? change = null;
        var succeeded = transaction.TryCommit(tx =>
        {
            if (!tx.TryRemoveExact(offer.Mutations, coins, preferredSlot)) return false;
            return (change = character.MoneyPouch.Mutations.StageAddExact(tx, coins.Count)) != null;
        });

        MoneyPouchChange.PublishChanges(transaction, change);
        return succeeded;
    }

    private (bool Received, MoneyPouchChange? PouchChange) Receive(ICharacter character, IReadOnlyList<IItem> items,
        IItemContainerTransaction transaction)
    {
        var nonCoinItems = items.Where(item => item.Id != CoinsItemId).ToArray();
        if (nonCoinItems.Length > 0 && !transaction.TryAddRange(character.Inventory.Items.Mutations, nonCoinItems))
        {
            return (false, null);
        }

        var coinCount = items.Where(item => item.Id == CoinsItemId).Sum(item => (long)item.Count);
        if (coinCount <= 0) return (true, null);
        if (coinCount > int.MaxValue) return (false, null);
        var change = character.MoneyPouch.Mutations.StageAddExact(transaction, (int)coinCount);
        return (change != null, change);
    }

    private bool CanReceive(ICharacter character, IReadOnlyList<IItem> items)
    {
        var nonCoinItems = items.Where(item => item.Id != CoinsItemId).ToArray();
        var coinCount = items.Where(item => item.Id == CoinsItemId).Sum(item => (long)item.Count);
        if (coinCount > int.MaxValue) return false;

        var pouchSpace = int.MaxValue - (long)character.MoneyPouch.Count;
        var inventoryCoins = Math.Max(0, coinCount - pouchSpace);
        var recipientItems = nonCoinItems;
        if (inventoryCoins > 0)
        {
            recipientItems = nonCoinItems.Append(_itemBuilder.Create().WithId(CoinsItemId)
                .WithCount((int)inventoryCoins).Build()).ToArray();
        }

        return character.Inventory.Items.HasSpaceForRange(recipientItems);
    }

    private IItemContainer? GetRecoveryContainer(ICharacter character, IReadOnlyList<IItem> items)
    {
        if (items.Count == 0) return character.Rewards?.Items;
        if (character.Rewards != null && character.Rewards.Items.HasSpaceForRange(items))
        {
            return character.Rewards.Items;
        }

        return character.Bank != null && character.Bank.Items.HasSpaceForRange(items) ? character.Bank.Items : null;
    }

    private IItem[] CloneOfferedItems(IItemContainer container) =>
        container.OfType<IItem>().Select(item => item.Clone()).ToArray();

    private void AddRecoveryBoundary(IItemContainerTransaction transaction, IItemContainer? container)
    {
        if (container != null) transaction.Include(container.Mutations);
    }

}
