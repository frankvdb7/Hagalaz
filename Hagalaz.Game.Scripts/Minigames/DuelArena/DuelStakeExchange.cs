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
        int preferredSourceSlot)
    {
        using var transaction = ItemContainerTransaction.Begin(character.Inventory.Items.Mutations, stake.Mutations);
        if (!character.Inventory.Items.TryTransferTo(stake, item, count, preferredSourceSlot)) return false;
        transaction.Commit();
        return true;
    }

    public bool TryReturnItemToInventory(
        ICharacter character,
        IItemContainer stake,
        IItem item,
        int count,
        int preferredSourceSlot)
    {
        using var transaction = ItemContainerTransaction.Begin(stake.Mutations, character.Inventory.Items.Mutations);
        if (!stake.TryTransferTo(character.Inventory.Items, item, count, preferredSourceSlot)) return false;
        transaction.Commit();
        return true;
    }

    public bool TryStakePouchCoins(ICharacter character, IItemContainer stake, int count)
    {
        if (count <= 0) return false;

        using var transaction = ItemContainerTransaction.Begin(stake.Mutations, character.MoneyPouch.Mutations);
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
        using var transaction = ItemContainerTransaction.Begin(stake.Mutations, character.MoneyPouch.Mutations);
        var count = coins.Count;
        if (!stake.TryRemoveExact(coins, preferredSourceSlot) || !character.MoneyPouch.TryAddExact(count)) return false;
        transaction.Commit();
        return true;
    }

    public bool TryRefundBoth(
        ICharacter first,
        IItemContainer firstStake,
        ICharacter second,
        IItemContainer secondStake)
    {
        using var transaction = ItemContainerTransaction.Begin(firstStake.Mutations, secondStake.Mutations,
            first.Inventory.Items.Mutations, second.Inventory.Items.Mutations,
            first.MoneyPouch.Mutations, second.MoneyPouch.Mutations);
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
                if (!stake.TryRemoveExact(item, slot) || !owner.MoneyPouch.TryAddExact(count)) return false;
            }
            else if (!stake.TryTransferTo(owner.Inventory.Items, item, count, slot))
            {
                return false;
            }
        }

        return true;
    }
}
