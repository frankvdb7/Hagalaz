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

    public bool TryStageCompletion(ICharacter first, IItemContainer firstOffer, ICharacter second,
        IItemContainer secondOffer) =>
        StageOffers(first, firstOffer, second, secondOffer, secondOffer, firstOffer);

    public bool TryStageRefund(ICharacter first, IItemContainer firstOffer, ICharacter second,
        IItemContainer secondOffer) =>
        StageOffers(first, firstOffer, second, secondOffer, firstOffer, secondOffer);

    private bool StageOffers(ICharacter first, IItemContainer firstOffer, ICharacter second,
        IItemContainer secondOffer, IItemContainer itemsForFirst, IItemContainer itemsForSecond)
    {
        var firstItems = CloneOfferedItems(itemsForFirst);
        var secondItems = CloneOfferedItems(itemsForSecond);
        if (!CanReceive(first, firstItems) || !CanReceive(second, secondItems)) return false;
        if (!Receive(first, firstItems) || !Receive(second, secondItems)) return false;
        firstOffer.Clear(true);
        secondOffer.Clear(true);
        return true;
    }

    /// <summary>Moves untouched escrow to existing recovery containers during forced destruction.</summary>
    internal bool TryStageEscrowRecovery(ICharacter first, IItemContainer firstOffer, ICharacter second,
        IItemContainer secondOffer)
    {
        var firstItems = CloneOfferedItems(firstOffer);
        var secondItems = CloneOfferedItems(secondOffer);
        var firstDestination = GetRecoveryContainer(first, firstItems);
        var secondDestination = GetRecoveryContainer(second, secondItems);
        if ((firstItems.Length > 0 && firstDestination == null) ||
            (secondItems.Length > 0 && secondDestination == null)) return false;
        if (firstDestination != null && firstItems.Length > 0 && !firstDestination.AddRange(firstItems)) return false;
        if (secondDestination != null && secondItems.Length > 0 && !secondDestination.AddRange(secondItems)) return false;
        firstOffer.Clear(true);
        secondOffer.Clear(true);
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

    private bool Receive(ICharacter character, IReadOnlyList<IItem> items)
    {
        var nonCoinItems = items.Where(item => item.Id != CoinsItemId).ToArray();
        if (nonCoinItems.Length > 0 && !character.Inventory.Items.AddRange(nonCoinItems)) return false;
        var coinCount = items.Where(item => item.Id == CoinsItemId).Sum(item => (long)item.Count);
        if (coinCount <= 0) return true;
        return coinCount <= int.MaxValue && character.MoneyPouch.TryAddExact((int)coinCount);
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
