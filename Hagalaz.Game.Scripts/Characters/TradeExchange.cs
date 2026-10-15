using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Characters;

/// <summary>Performs the checked terminal operations of a trade.</summary>
internal sealed class TradeExchange
{
    private const int CoinsItemId = 995;

    /// <summary>Stages a trade completion inside the caller-owned item-container transaction.</summary>
    /// <remarks>
    /// The caller must include both offer containers and both MoneyPouches in the transaction. Each MoneyPouch
    /// contributes its inventory storage. This method does not create or commit a transaction.
    /// </remarks>
    public bool TryStageCompletion(ICharacter first, TradingCharacterScript.TradeContainer firstOffer, ICharacter second,
        TradingCharacterScript.TradeContainer secondOffer) =>
        StageOffers(first, second, secondOffer, firstOffer);

    /// <summary>Stages a trade refund inside the caller-owned item-container transaction.</summary>
    /// <remarks>
    /// The caller must include both offer containers and both MoneyPouches in the transaction. Each MoneyPouch
    /// contributes its inventory storage. This method does not create or commit a transaction.
    /// </remarks>
    public bool TryStageRefund(ICharacter first, TradingCharacterScript.TradeContainer firstOffer, ICharacter second,
        TradingCharacterScript.TradeContainer secondOffer) =>
        StageOffers(first, second, firstOffer, secondOffer);

    private bool StageOffers(ICharacter first, ICharacter second,
        TradingCharacterScript.TradeContainer itemsForFirst, TradingCharacterScript.TradeContainer itemsForSecond)
    {
        if (!TransferOffer(itemsForFirst, first) || !TransferOffer(itemsForSecond, second)) return false;
        return true;
    }

    /// <summary>Stages moving untouched escrow to recovery containers during forced destruction.</summary>
    /// <remarks>
    /// The caller must begin a transaction containing both offer containers and every available recovery destination
    /// that may be selected before calling this method. This method does not create or commit a transaction.
    /// </remarks>
    internal bool TryStageEscrowRecovery(ICharacter first, TradingCharacterScript.TradeContainer firstOffer, ICharacter second,
        TradingCharacterScript.TradeContainer secondOffer)
    {
        var firstItems = CloneOfferedItems(firstOffer.Items);
        var secondItems = CloneOfferedItems(secondOffer.Items);
        var firstDestination = GetRecoveryContainer(first, firstItems);
        var secondDestination = GetRecoveryContainer(second, secondItems);
        if ((firstItems.Length > 0 && firstDestination == null) ||
            (secondItems.Length > 0 && secondDestination == null)) return false;
        if (firstDestination != null && firstItems.Length > 0 && !TransferOffer(firstOffer, firstDestination)) return false;
        if (secondDestination != null && secondItems.Length > 0 && !TransferOffer(secondOffer, secondDestination)) return false;
        return true;
    }

    internal bool TryOfferMoneyFromPouch(ICharacter character, TradingCharacterScript.TradeContainer offer, IItem coins)
    {
        if (coins.Count <= 0) return false;

        using var transaction = ItemContainerTransaction.Begin(offer.MutableItems, character.MoneyPouch);
        if (!character.MoneyPouch.HasCoins(coins.Count) || !offer.Items.HasSpaceFor(coins) || !offer.AddItem(coins) ||
            !character.MoneyPouch.TryRemoveExact(coins.Count)) return false;
        transaction.Commit();
        offer.PublishChanges();
        return true;
    }

    internal bool TryReturnMoneyToPouch(ICharacter character, TradingCharacterScript.TradeContainer offer, IItem coins, int preferredSlot)
    {
        using var transaction = ItemContainerTransaction.Begin(offer.MutableItems, character.MoneyPouch);
        if (!character.MoneyPouch.TryTransferCoinsFrom(offer.MutableItems, coins, preferredSlot)) return false;
        transaction.Commit();
        offer.PublishChanges();
        return true;
    }

    private bool TransferOffer(TradingCharacterScript.TradeContainer source, ICharacter recipient)
    {
        var offeredItems = source.Items.Select((item, slot) => (item, slot))
            .Where(entry => entry.item != null)
            .Select(entry => (Item: entry.item!, entry.slot))
            .ToArray();

        foreach (var (item, slot) in offeredItems)
        {
            if (item.Id == CoinsItemId)
            {
                if (!recipient.MoneyPouch.TryTransferCoinsFrom(source.MutableItems, item, slot)) return false;
            }
            else if (!recipient.Inventory.TryTransferItemFrom(source.MutableItems, item, item.Count,
                         slot, destinationItem: item.Clone()))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TransferOffer(TradingCharacterScript.TradeContainer source, IItemTransactional destination)
    {
        var offeredItems = source.Items.Select((item, slot) => (item, slot))
            .Where(entry => entry.item != null)
            .Select(entry => (Item: entry.item!, entry.slot))
            .ToArray();

        foreach (var (item, slot) in offeredItems)
        {
            var transferred = destination switch
            {
                IRewardContainer rewards => rewards.ReceiveFromTrade(source.MutableItems, item, item.Count, slot),
                IBankContainer bank => bank.ReceiveFromTrade(source.MutableItems, item, item.Count, slot),
                _ => throw new InvalidOperationException("Trade recovery requires a bank or reward container.")
            };
            if (!transferred) return false;
        }

        return true;
    }

    private IItemTransactional? GetRecoveryContainer(ICharacter character, IReadOnlyList<IItem> items)
    {
        if (items.Count == 0) return (IItemTransactional?)character.Rewards ?? character.Bank;
        if (character.Rewards != null && character.Rewards.Items.HasSpaceForRange(items))
        {
            return character.Rewards;
        }

        return character.Bank != null && character.Bank.Items.HasSpaceForRange(items) ? character.Bank : null;
    }

    private IItem[] CloneOfferedItems(IReadOnlyItemContainer container) =>
        container.OfType<IItem>().Select(item => item.Clone()).ToArray();
}
