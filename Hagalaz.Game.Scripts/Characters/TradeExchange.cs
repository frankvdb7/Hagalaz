using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Characters;

/// <summary>
/// Performs the two checked terminal operations of a trade: completion and refund.
/// </summary>
internal static class TradeExchange
{
    private const int CoinsItemId = 995;

    public static bool TryExchange(ICharacter first, ITradeItemContainer firstOffer, ICharacter second,
        ITradeItemContainer secondOffer, IItemBuilder itemBuilder) =>
        TryCompleteTrade(first, firstOffer, second, secondOffer, itemBuilder);

    public static bool TryRefund(ICharacter first, ITradeItemContainer firstOffer, ICharacter second,
        ITradeItemContainer secondOffer, IItemBuilder itemBuilder) =>
        TryRefundTrade(first, firstOffer, second, secondOffer, itemBuilder);

    internal static bool TryCompleteTrade(ICharacter first, ITradeItemContainer firstOffer, ICharacter second,
        ITradeItemContainer secondOffer, IItemBuilder itemBuilder)
    {
        using var locks = AcquireLocks(GetContainers(firstOffer, secondOffer, first, second));
        try
        {
            var firstItems = SnapshotItems(firstOffer);
            var secondItems = SnapshotItems(secondOffer);
            if (!CanReceive(first, secondItems, itemBuilder) || !CanReceive(second, firstItems, itemBuilder))
            {
                return false;
            }

            if (!Receive(first, secondItems) || !Receive(second, firstItems))
            {
                return false;
            }

            firstOffer.Clear(false);
            secondOffer.Clear(false);
            return true;
        }
        catch (InvalidOperationException)
        {
            // Capacity and storage checks happen under the same boundary as the
            // commit. An unexpected domain failure is reported as a failed try.
            return false;
        }
    }

    internal static bool TryRefundTrade(ICharacter first, ITradeItemContainer firstOffer, ICharacter second,
        ITradeItemContainer secondOffer, IItemBuilder itemBuilder)
    {
        using var locks = AcquireLocks(GetContainers(firstOffer, secondOffer, first, second));
        try
        {
            var firstItems = SnapshotItems(firstOffer);
            var secondItems = SnapshotItems(secondOffer);
            if (!CanReceive(first, firstItems, itemBuilder) || !CanReceive(second, secondItems, itemBuilder))
            {
                return false;
            }

            if (!Receive(first, firstItems) || !Receive(second, secondItems))
            {
                return false;
            }

            firstOffer.Clear(false);
            secondOffer.Clear(false);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>
    /// Moves untouched escrow to the existing recovery containers. This is used
    /// only when cancellation cannot return value during forced destruction.
    /// </summary>
    internal static bool TryConserveEscrow(ICharacter first, ITradeItemContainer firstOffer, ICharacter second,
        ITradeItemContainer secondOffer)
    {
        var containers = new List<BaseItemContainer>();
        AddContainer(containers, firstOffer);
        AddContainer(containers, secondOffer);
        AddContainer(containers, first.Rewards);
        AddContainer(containers, first.Bank);
        AddContainer(containers, second.Rewards);
        AddContainer(containers, second.Bank);
        using var locks = AcquireLocks(containers);
        try
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

            if (firstDestination != null && firstItems.Length > 0 && !firstDestination.AddRangeForTrade(firstItems))
            {
                return false;
            }

            if (secondDestination != null && secondItems.Length > 0 && !secondDestination.AddRangeForTrade(secondItems))
            {
                return false;
            }

            firstOffer.Clear(false);
            secondOffer.Clear(false);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    internal static bool AddRangeForTrade(ITradeItemContainer container, IEnumerable<IItem?> items) =>
        container.AddRangeForTrade(items);

    internal static bool RemoveForTrade(ITradeItemContainer container, IItem item, int preferredSlot = -1) =>
        container.RemoveForTrade(item, preferredSlot);

    internal static bool AddMoney(ICharacter character, int count) => character.MoneyPouch.AddForTrade(count);

    internal static bool RemoveMoney(ICharacter character, int count) => character.MoneyPouch.RemoveForTrade(count);

    private static bool Receive(ICharacter character, IReadOnlyList<IItem> items)
    {
        var nonCoinItems = items.Where(item => item.Id != CoinsItemId).ToArray();
        if (nonCoinItems.Length > 0 && !character.Inventory.AddRangeForTrade(nonCoinItems))
        {
            return false;
        }

        var coinCount = items.Where(item => item.Id == CoinsItemId).Sum(item => (long)item.Count);
        return coinCount <= 0 || coinCount <= int.MaxValue && character.MoneyPouch.AddForTrade((int)coinCount);
    }

    private static bool CanReceive(ICharacter character, IReadOnlyList<IItem> items, IItemBuilder itemBuilder)
    {
        var nonCoinItems = items.Where(item => item.Id != CoinsItemId).ToArray();
        if (nonCoinItems.Length > 0 && !character.Inventory.HasSpaceForRange(nonCoinItems))
        {
            return false;
        }

        long pouchSpace = int.MaxValue - (long)character.MoneyPouch.Count;
        long inventoryCoins = 0;
        foreach (var item in items.Where(item => item.Id == CoinsItemId))
        {
            var pouchCoins = Math.Min(pouchSpace, item.Count);
            pouchSpace -= pouchCoins;
            inventoryCoins += item.Count - pouchCoins;
        }

        return inventoryCoins <= int.MaxValue &&
               (inventoryCoins <= 0 || character.Inventory.HasSpaceFor(
                   itemBuilder.Create().WithId(CoinsItemId).WithCount((int)inventoryCoins).Build()));
    }

    private static ITradeItemContainer? GetRecoveryContainer(ICharacter character, IReadOnlyList<IItem> items)
    {
        if (items.Count == 0)
        {
            return character.Rewards;
        }

        if (character.Rewards != null && character.Rewards.HasSpaceForRange(items))
        {
            return character.Rewards;
        }

        return character.Bank != null && character.Bank.HasSpaceForRange(items) ? character.Bank : null;
    }

    private static IItem[] SnapshotItems(IItemContainer container) =>
        container.OfType<IItem>().Select(item => item.Clone()).ToArray();

    private static List<BaseItemContainer> GetContainers(ITradeItemContainer firstOffer, ITradeItemContainer secondOffer,
        ICharacter first, ICharacter second)
    {
        var containers = new List<BaseItemContainer>();
        AddContainer(containers, firstOffer);
        AddContainer(containers, secondOffer);
        AddContainer(containers, first.Inventory);
        AddContainer(containers, second.Inventory);
        AddContainer(containers, first.MoneyPouch);
        AddContainer(containers, second.MoneyPouch);
        return containers;
    }

    private static void AddContainer(List<BaseItemContainer> containers, IItemContainer? container)
    {
        if (container is BaseItemContainer baseContainer && !containers.Contains(baseContainer))
        {
            containers.Add(baseContainer);
        }
    }

    private static LockScope AcquireLocks(IEnumerable<BaseItemContainer> containers) =>
        new(containers.OrderBy(container => container.MutationOrder));

    private sealed class LockScope : IDisposable
    {
        private readonly IReadOnlyList<BaseItemContainer> _containers;

        public LockScope(IEnumerable<BaseItemContainer> containers)
        {
            _containers = containers.ToArray();
            foreach (var container in _containers)
            {
                Monitor.Enter(container.MutationLock);
            }
        }

        public void Dispose()
        {
            for (var i = _containers.Count - 1; i >= 0; i--)
            {
                Monitor.Exit(_containers[i].MutationLock);
            }
        }
    }
}
