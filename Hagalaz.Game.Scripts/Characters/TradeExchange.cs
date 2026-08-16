using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Characters;

/// <summary>
/// Applies one checked trade transfer using the existing character containers.
/// </summary>
internal static class TradeExchange
{
    private const int CoinsItemId = 995;

    public static bool TryExchange(
        ICharacter first,
        IItemContainer firstOffer,
        ICharacter second,
        IItemContainer secondOffer,
        IItemBuilder itemBuilder)
    {
        return TryTransfer(
            first,
            SnapshotItems(firstOffer),
            second,
            SnapshotItems(secondOffer),
            itemBuilder);
    }

    public static bool TryRefund(
        ICharacter first,
        IItemContainer firstOffer,
        ICharacter second,
        IItemContainer secondOffer,
        IItemBuilder itemBuilder)
    {
        return TryTransfer(
            first,
            SnapshotItems(firstOffer),
            second,
            SnapshotItems(secondOffer),
            itemBuilder,
            firstReceivesSecondOffer: false);
    }

    private static bool TryTransfer(
        ICharacter first,
        IReadOnlyList<IItem> firstItems,
        ICharacter second,
        IReadOnlyList<IItem> secondItems,
        IItemBuilder itemBuilder,
        bool firstReceivesSecondOffer = true)
    {
        var firstSnapshot = Snapshot(first);
        var secondSnapshot = Snapshot(second);

        try
        {
            var firstReceived = firstReceivesSecondOffer ? secondItems : firstItems;
            var secondReceived = firstReceivesSecondOffer ? firstItems : secondItems;

            if (!CanReceive(first, firstReceived, itemBuilder) ||
                !CanReceive(second, secondReceived, itemBuilder))
            {
                return false;
            }

            if (!TryReceive(first, firstReceived) || !TryReceive(second, secondReceived))
            {
                RestoreOrThrow(first, firstSnapshot);
                RestoreOrThrow(second, secondSnapshot);
                return false;
            }

            return true;
        }
        catch
        {
            RestoreOrThrow(first, firstSnapshot);
            RestoreOrThrow(second, secondSnapshot);
            return false;
        }
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
        foreach (var item in items)
        {
            if (item.Id != CoinsItemId)
            {
                continue;
            }

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

    private static bool TryReceive(ICharacter character, IReadOnlyList<IItem> items)
    {
        var nonCoinItems = items.Where(item => item.Id != CoinsItemId).ToArray();
        if (nonCoinItems.Length > 0 && !character.Inventory.AddRange(nonCoinItems))
        {
            return false;
        }

        foreach (var item in items)
        {
            if (item.Id == CoinsItemId && item.Count > 0 && !character.MoneyPouch.Add(item.Count))
            {
                return false;
            }
        }

        return true;
    }

    internal static IItem?[] Snapshot(IItemContainer container) =>
        container.Select(item => item?.Clone()).ToArray();

    private static IItem[] SnapshotItems(IItemContainer container) =>
        container.OfType<IItem>().Select(item => item.Clone()).ToArray();

    private static CharacterSnapshot Snapshot(ICharacter character) =>
        new(Snapshot(character.Inventory), Snapshot(character.MoneyPouch));

    private static bool Restore(ICharacter character, CharacterSnapshot snapshot)
    {
        var inventoryRestored = Restore(character.Inventory, snapshot.Inventory);
        var moneyPouchRestored = Restore(character.MoneyPouch, snapshot.MoneyPouch);
        return inventoryRestored && moneyPouchRestored;
    }

    private static void RestoreOrThrow(ICharacter character, CharacterSnapshot snapshot)
    {
        if (!Restore(character, snapshot))
        {
            throw new InvalidOperationException("Trade recipient rollback failed.");
        }
    }

    internal static bool Restore(IItemContainer container, IItem?[] snapshot)
    {
        try
        {
            container.Clear(false);
            return container.AddRange(snapshot.Select(item => item?.Clone()));
        }
        catch
        {
            return false;
        }
    }

    private sealed record CharacterSnapshot(IItem?[] Inventory, IItem?[] MoneyPouch);
}
