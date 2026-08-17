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
        List<ContainerSnapshot> recipientSnapshots = [];
        try
        {
            var firstItems = SnapshotItems(firstOffer);
            var secondItems = SnapshotItems(secondOffer);
            if (!CanReceive(first, secondItems, itemBuilder) || !CanReceive(second, firstItems, itemBuilder))
            {
                return false;
            }

            recipientSnapshots = CaptureSnapshots(first.Inventory, first.MoneyPouch, second.Inventory, second.MoneyPouch);
            if (!Receive(first, secondItems) || !Receive(second, firstItems))
            {
                RestoreSnapshots(recipientSnapshots);
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
            RestoreSnapshots(recipientSnapshots);
            return false;
        }
    }

    internal static bool TryRefundTrade(ICharacter first, ITradeItemContainer firstOffer, ICharacter second,
        ITradeItemContainer secondOffer, IItemBuilder itemBuilder)
    {
        using var locks = AcquireLocks(GetContainers(firstOffer, secondOffer, first, second));
        List<ContainerSnapshot> recipientSnapshots = [];
        try
        {
            var firstItems = SnapshotItems(firstOffer);
            var secondItems = SnapshotItems(secondOffer);
            if (!CanReceive(first, firstItems, itemBuilder) || !CanReceive(second, secondItems, itemBuilder))
            {
                return false;
            }

            recipientSnapshots = CaptureSnapshots(first.Inventory, first.MoneyPouch, second.Inventory, second.MoneyPouch);
            if (!Receive(first, firstItems) || !Receive(second, secondItems))
            {
                RestoreSnapshots(recipientSnapshots);
                return false;
            }

            firstOffer.Clear(false);
            secondOffer.Clear(false);
            return true;
        }
        catch (InvalidOperationException)
        {
            RestoreSnapshots(recipientSnapshots);
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
        var containers = new List<TradeItemContainer>();
        AddContainer(containers, firstOffer);
        AddContainer(containers, secondOffer);
        AddContainer(containers, first.Rewards);
        AddContainer(containers, first.Bank);
        AddContainer(containers, second.Rewards);
        AddContainer(containers, second.Bank);
        using var locks = AcquireLocks(containers);
        List<ContainerSnapshot> conservationSnapshots = [];
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

            conservationSnapshots = CaptureSnapshots(firstOffer, secondOffer, firstDestination, secondDestination);
            if (firstDestination != null && firstItems.Length > 0 && !firstDestination.AddRangeForTrade(firstItems))
            {
                RestoreSnapshots(conservationSnapshots);
                return false;
            }

            if (secondDestination != null && secondItems.Length > 0 && !secondDestination.AddRangeForTrade(secondItems))
            {
                RestoreSnapshots(conservationSnapshots);
                return false;
            }

            firstOffer.Clear(false);
            secondOffer.Clear(false);
            return true;
        }
        catch (InvalidOperationException)
        {
            RestoreSnapshots(conservationSnapshots);
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
        var coinCount = items.Where(item => item.Id == CoinsItemId).Sum(item => (long)item.Count);
        if (coinCount > int.MaxValue)
        {
            return false;
        }

        var pouchSpace = int.MaxValue - (long)character.MoneyPouch.Count;
        var inventoryCoins = Math.Max(0, coinCount - pouchSpace);
        var recipientItems = nonCoinItems;
        if (inventoryCoins > 0)
        {
            var overflowCoins = itemBuilder.Create()
                .WithId(CoinsItemId)
                .WithCount((int)inventoryCoins)
                .Build();
            recipientItems = nonCoinItems.Append(overflowCoins).ToArray();
        }

        return character.Inventory.HasSpaceForRange(recipientItems);
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

    private static List<ContainerSnapshot> CaptureSnapshots(params IItemContainer?[] containers) =>
        containers
            .OfType<TradeItemContainer>()
            .Distinct()
            .Select(container =>
            {
                var items = (IItem[])container.ToArray();
                var counts = items.Select(item => item?.Count ?? 0).ToArray();
                return new ContainerSnapshot(container, items, counts);
            })
            .ToList();

    private static void RestoreSnapshots(IEnumerable<ContainerSnapshot> snapshots)
    {
        foreach (var snapshot in snapshots)
        {
            snapshot.Container.SetItems(snapshot.Items, false);
            for (var i = 0; i < snapshot.Items.Length; i++)
            {
                if (snapshot.Items[i] != null)
                {
                    snapshot.Items[i]!.Count = snapshot.Counts[i];
                }
            }
        }
    }

    private static List<TradeItemContainer> GetContainers(ITradeItemContainer firstOffer, ITradeItemContainer secondOffer,
        ICharacter first, ICharacter second)
    {
        var containers = new List<TradeItemContainer>();
        AddContainer(containers, firstOffer);
        AddContainer(containers, secondOffer);
        AddContainer(containers, first.Inventory);
        AddContainer(containers, second.Inventory);
        AddContainer(containers, first.MoneyPouch);
        AddContainer(containers, second.MoneyPouch);
        return containers;
    }

    private static void AddContainer(List<TradeItemContainer> containers, IItemContainer? container)
    {
        if (container is TradeItemContainer tradeContainer && !containers.Contains(tradeContainer))
        {
            containers.Add(tradeContainer);
        }
    }

    private static LockScope AcquireLocks(IEnumerable<TradeItemContainer> containers) =>
        new(containers.OrderBy(container => container.MutationOrder));

    private sealed record ContainerSnapshot(TradeItemContainer Container, IItem[] Items, int[] Counts);

    private sealed class LockScope : IDisposable
    {
        private readonly IReadOnlyList<TradeItemContainer> _containers;

        public LockScope(IEnumerable<TradeItemContainer> containers)
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
