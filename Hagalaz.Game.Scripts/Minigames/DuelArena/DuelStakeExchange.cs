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
        character.Inventory.Items.Mutations.TryTransferTo(stake.Mutations, item, count, preferredSourceSlot);

    public bool TryReturnItemToInventory(
        ICharacter character,
        IItemContainer stake,
        IItem item,
        int count,
        int preferredSourceSlot) =>
        stake.Mutations.TryTransferTo(character.Inventory.Items.Mutations, item, count, preferredSourceSlot);

    public bool TryStakePouchCoins(ICharacter character, IItemContainer stake, int count)
    {
        if (count <= 0) return false;

        var transaction = new ItemContainerTransaction(stake.Mutations);
        character.MoneyPouch.Mutations.EnlistIn(transaction);
        return transaction.TryExecute(tx =>
        {
            var transferCount = (int)Math.Min(count,
                (long)character.MoneyPouch.Count + character.Inventory.Items.GetCountById(CoinsItemId));
            if (transferCount <= 0) return false;

            var coins = _itemBuilder.Create().WithId(CoinsItemId).WithCount(transferCount).Build();
            return tx.TryAddRange(stake.Mutations, [coins]) &&
                   character.MoneyPouch.Mutations.TryStageRemoveExact(tx, transferCount);
        });
    }

    public bool TryReturnCoinsToPouch(
        ICharacter character,
        IItemContainer stake,
        IItem coins,
        int preferredSourceSlot)
    {
        var transaction = new ItemContainerTransaction(stake.Mutations);
        character.MoneyPouch.Mutations.EnlistIn(transaction);
        return transaction.TryExecute(tx =>
            tx.TryRemoveExact(stake.Mutations, coins, preferredSourceSlot) &&
            character.MoneyPouch.Mutations.TryStageAddExact(tx, coins.Count));
    }

    public bool TryRefundBoth(
        ICharacter first,
        IItemContainer firstStake,
        ICharacter second,
        IItemContainer secondStake)
    {
        var transaction = new ItemContainerTransaction(
            firstStake.Mutations,
            secondStake.Mutations,
            first.Inventory.Items.Mutations,
            second.Inventory.Items.Mutations);
        first.MoneyPouch.Mutations.EnlistIn(transaction);
        second.MoneyPouch.Mutations.EnlistIn(transaction);

        return transaction.TryExecute(tx =>
            RefundStake(tx, first, firstStake) && RefundStake(tx, second, secondStake));
    }

    private static bool RefundStake(IItemContainerTransaction transaction, ICharacter owner, IItemContainer stake)
    {
        var entries = stake.Select((item, slot) => (item, slot))
            .Where(entry => entry.item != null)
            .Select(entry => (Item: entry.item!, entry.slot, Count: entry.item!.Count))
            .ToArray();

        foreach (var (item, slot, count) in entries)
        {
            if (item.Id == CoinsItemId)
            {
                if (!transaction.TryRemoveExact(stake.Mutations, item, slot) ||
                    !owner.MoneyPouch.Mutations.TryStageAddExact(transaction, count))
                {
                    return false;
                }
            }
            else if (!transaction.TryTransfer(stake.Mutations, owner.Inventory.Items.Mutations, item, count, slot))
            {
                return false;
            }
        }

        return true;
    }
}
