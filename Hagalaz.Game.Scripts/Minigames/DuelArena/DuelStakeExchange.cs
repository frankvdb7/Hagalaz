using System;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Minigames.DuelArena;

internal sealed class DuelStakeExchange
{
    private const int CoinsItemId = 995;
    private readonly IItemBuilder _itemBuilder;

    public DuelStakeExchange(IItemBuilder itemBuilder)
    {
        ArgumentNullException.ThrowIfNull(itemBuilder);
        _itemBuilder = itemBuilder;
    }

    public bool TryStakeInventoryItem(
        ICharacter character,
        IItemContainer stake,
        IItem item,
        int count,
        int preferredSourceSlot) =>
        character.Inventory.Items.TryTransferTo(stake, item, count, preferredSourceSlot);

    public bool TryReturnItemToInventory(
        ICharacter character,
        IItemContainer stake,
        IItem item,
        int count,
        int preferredSourceSlot) =>
        stake.TryTransferTo(character.Inventory.Items, item, count, preferredSourceSlot);

    public bool TryStakePouchCoins(ICharacter character, IItemContainer stake, int count)
    {
        if (count <= 0) return false;

        using var transaction = ItemContainerTransaction.Begin(stake, character.MoneyPouch);
        var transferCount = (int)Math.Min(count,
            (long)character.MoneyPouch.Count + character.Inventory.Items.GetCountById(CoinsItemId));
        if (transferCount <= 0) return false;
        var coins = _itemBuilder.Create().WithId(CoinsItemId).WithCount(transferCount).Build();
        if (!stake.Add(coins) || !character.MoneyPouch.TryRemoveExact(transferCount)) return false;
        transaction.Commit();
        return true;
    }

    public bool TryReturnCoinsToPouch(
        ICharacter character,
        IItemContainer stake,
        IItem coins,
        int preferredSourceSlot)
    {
        using var transaction = ItemContainerTransaction.Begin(stake, character.MoneyPouch);
        if (!character.MoneyPouch.TryTransferCoinsFrom(stake, coins, preferredSourceSlot)) return false;
        transaction.Commit();
        return true;
    }

    public bool TryRefundBoth(
        ICharacter first,
        IItemContainer firstStake,
        ICharacter second,
        IItemContainer secondStake)
    {
        using var transaction = ItemContainerTransaction.Begin(firstStake, secondStake,
            first.MoneyPouch, second.MoneyPouch);
        if (!RefundStake(first, firstStake) || !RefundStake(second, secondStake)) return false;
        transaction.Commit();
        return true;
    }

    private static bool RefundStake(ICharacter owner, IItemContainer stake)
    {
        var entries = stake.Select((item, slot) => (item, slot))
            .Where(entry => entry.item != null)
            .Select(entry => (Item: entry.item!, entry.slot, Count: entry.item!.Count))
            .ToArray();

        foreach (var (item, slot, count) in entries)
        {
            if (item.Id == CoinsItemId)
            {
                if (!owner.MoneyPouch.TryTransferCoinsFrom(stake, item, slot)) return false;
            }
            else if (!stake.TryTransferTo(owner.Inventory.Items, item, count, slot))
            {
                return false;
            }
        }

        return true;
    }
}
