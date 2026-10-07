using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Characters;

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

    /// <summary>Stages a trade completion inside the caller-owned item-container transaction.</summary>
    /// <remarks>
    /// The caller must begin a transaction containing both offer containers, both player inventories, and both
    /// MoneyPouch aggregates before calling this method. This method does not create or commit a transaction.
    /// </remarks>
    public bool TryStageCompletion(ICharacter first, IItemContainer firstOffer, ICharacter second,
        IItemContainer secondOffer) =>
        StageOffers(first, second, secondOffer, firstOffer);

    /// <summary>Stages a trade refund inside the caller-owned item-container transaction.</summary>
    /// <remarks>
    /// The caller must begin a transaction containing both offer containers, both player inventories, and both
    /// MoneyPouch aggregates before calling this method. This method does not create or commit a transaction.
    /// </remarks>
    public bool TryStageRefund(ICharacter first, IItemContainer firstOffer, ICharacter second,
        IItemContainer secondOffer) =>
        StageOffers(first, second, firstOffer, secondOffer);

    private bool StageOffers(ICharacter first, ICharacter second,
        IItemContainer itemsForFirst, IItemContainer itemsForSecond)
    {
        var firstItems = CloneOfferedItems(itemsForFirst);
        var secondItems = CloneOfferedItems(itemsForSecond);
        if (!CanReceive(first, firstItems) || !CanReceive(second, secondItems)) return false;
        if (!TransferOffer(itemsForFirst, first) || !TransferOffer(itemsForSecond, second)) return false;
        return true;
    }

    /// <summary>Stages moving untouched escrow to recovery containers during forced destruction.</summary>
    /// <remarks>
    /// The caller must begin a transaction containing both offer containers and every available recovery destination
    /// that may be selected before calling this method. This method does not create or commit a transaction.
    /// </remarks>
    internal bool TryStageEscrowRecovery(ICharacter first, IItemContainer firstOffer, ICharacter second,
        IItemContainer secondOffer)
    {
        var firstItems = CloneOfferedItems(firstOffer);
        var secondItems = CloneOfferedItems(secondOffer);
        var firstDestination = GetRecoveryContainer(first, firstItems);
        var secondDestination = GetRecoveryContainer(second, secondItems);
        if ((firstItems.Length > 0 && firstDestination == null) ||
            (secondItems.Length > 0 && secondDestination == null)) return false;
        if (firstDestination != null && firstItems.Length > 0 && !TransferOffer(firstOffer, firstDestination)) return false;
        if (secondDestination != null && secondItems.Length > 0 && !TransferOffer(secondOffer, secondDestination)) return false;
        return true;
    }

    internal bool TryOfferMoneyFromPouch(ICharacter character, IItemContainer offer, IItem coins)
    {
        if (coins.Count <= 0) return false;

        using var transaction = ItemContainerTransaction.Begin(offer, character.MoneyPouch);
        if (!character.MoneyPouch.HasCoins(coins.Count) || !offer.HasSpaceFor(coins) || !offer.Add(coins) ||
            !character.MoneyPouch.TryRemoveExact(coins.Count)) return false;
        transaction.Commit();
        return true;
    }

    internal bool TryReturnMoneyToPouch(ICharacter character, IItemContainer offer, IItem coins, int preferredSlot)
    {
        using var transaction = ItemContainerTransaction.Begin(offer, character.MoneyPouch);
        var count = coins.Count;
        if (!offer.TryRemoveExact(coins, preferredSlot) || !character.MoneyPouch.TryAddExact(count)) return false;
        transaction.Commit();
        return true;
    }

    private bool TransferOffer(IItemContainer source, ICharacter recipient)
    {
        var offeredItems = source.Select((item, slot) => (item, slot))
            .Where(entry => entry.item != null)
            .Select(entry => (Item: entry.item!, entry.slot))
            .ToArray();

        foreach (var (item, slot) in offeredItems)
        {
            if (item.Id == CoinsItemId)
            {
                if (!recipient.MoneyPouch.TryTransferCoinsFrom(source, item, slot)) return false;
            }
            else if (!source.TryTransferTo(recipient.Inventory.Items, item, item.Count,
                         slot, destinationItem: item.Clone()))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TransferOffer(IItemContainer source, IItemContainer destination)
    {
        var offeredItems = source.Select((item, slot) => (item, slot))
            .Where(entry => entry.item != null)
            .Select(entry => (Item: entry.item!, entry.slot))
            .ToArray();

        foreach (var (item, slot) in offeredItems)
        {
            if (!source.TryTransferTo(destination, item, item.Count, slot, destinationItem: item.Clone())) return false;
        }

        return true;
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

}
