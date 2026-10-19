using System;
using System.Linq;
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

    internal bool TryOfferMoneyFromPouch(ICharacter character, TradingCharacterScript.TradeContainer offer, IItem coins)
    {
        if (coins.Count <= 0) return false;

        using var transaction = ItemContainerTransaction.Begin(offer.MutableItems, character.MoneyPouch);
        if (!offer.AddItem(coins) || !character.MoneyPouch.TryRemoveExact(coins.Count)) return false;
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

}
