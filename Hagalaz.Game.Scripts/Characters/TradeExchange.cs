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
        ITradeItemContainer secondOffer, IItemBuilder itemBuilder) =>
        TryExchangeOffers(first, firstOffer, second, secondOffer, secondOffer, firstOffer, itemBuilder);

    internal static bool TryRefundTrade(ICharacter first, ITradeItemContainer firstOffer, ICharacter second,
        ITradeItemContainer secondOffer, IItemBuilder itemBuilder) =>
        TryExchangeOffers(first, firstOffer, second, secondOffer, firstOffer, secondOffer, itemBuilder);

    private static bool TryExchangeOffers(ICharacter first, ITradeItemContainer firstOffer, ICharacter second,
        ITradeItemContainer secondOffer, ITradeItemContainer itemsForFirst, ITradeItemContainer itemsForSecond,
        IItemBuilder itemBuilder)
    {
        var changes = CreateChanges();
        var pouchMessages = new List<(IMoneyPouchContainer Pouch, int ChangeCount)>();
        List<ContainerSnapshot>? restoreSnapshots = null;
        using (AcquireLocks(GetContainers(firstOffer, secondOffer, first, second)))
        {
            var firstItems = SnapshotItems(itemsForFirst);
            var secondItems = SnapshotItems(itemsForSecond);
            if (!CanReceive(first, firstItems, itemBuilder) || !CanReceive(second, secondItems, itemBuilder))
            {
                return false;
            }

            var recipientSnapshots = CaptureSnapshots(first.Inventory, first.MoneyPouch, second.Inventory, second.MoneyPouch);
            if (!Receive(first, firstItems, changes, pouchMessages) || !Receive(second, secondItems, changes, pouchMessages))
            {
                RestoreSnapshotsStorage(recipientSnapshots);
                restoreSnapshots = recipientSnapshots;
            }
            else
            {
                RecordChangedSlots(changes, firstOffer, GetOccupiedSlots(firstOffer));
                RecordChangedSlots(changes, secondOffer, GetOccupiedSlots(secondOffer));
                firstOffer.Clear(false);
                secondOffer.Clear(false);
            }
        }

        return FinishMoneyPouchTransfer(changes, pouchMessages, restoreSnapshots);
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
        var changes = CreateChanges();
        List<ContainerSnapshot>? restoreSnapshots = null;
        using (AcquireLocks(containers))
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

            var conservationSnapshots = CaptureSnapshots(firstOffer, secondOffer, firstDestination, secondDestination);
            HashSet<int> firstChangedSlots = [];
            HashSet<int> secondChangedSlots = [];
            var failed = false;
            if (firstDestination != null && firstItems.Length > 0 &&
                !firstDestination.TryAddRangeForTradeStorage(firstItems, out firstChangedSlots))
            {
                RestoreSnapshotsStorage(conservationSnapshots);
                restoreSnapshots = conservationSnapshots;
                failed = true;
            }

            if (!failed && secondDestination != null && secondItems.Length > 0 &&
                !secondDestination.TryAddRangeForTradeStorage(secondItems, out secondChangedSlots))
            {
                RestoreSnapshotsStorage(conservationSnapshots);
                restoreSnapshots = conservationSnapshots;
                failed = true;
            }

            if (!failed)
            {
                if (firstDestination != null && firstItems.Length > 0)
                    RecordChangedSlots(changes, firstDestination, firstChangedSlots);
                if (secondDestination != null && secondItems.Length > 0)
                    RecordChangedSlots(changes, secondDestination, secondChangedSlots);

                RecordChangedSlots(changes, firstOffer, GetOccupiedSlots(firstOffer));
                RecordChangedSlots(changes, secondOffer, GetOccupiedSlots(secondOffer));
                firstOffer.Clear(false);
                secondOffer.Clear(false);
            }
        }

        if (restoreSnapshots != null)
        {
            PublishRestoredSnapshots(restoreSnapshots);
            return false;
        }

        PublishChanges(changes);
        return true;
    }

    internal static bool TryOfferMoneyFromPouch(ICharacter character, ITradeItemContainer offer, IItem coins)
    {
        if (coins.Count <= 0)
        {
            return false;
        }

        var changes = CreateChanges();
        var pouchMessages = new List<(IMoneyPouchContainer Pouch, int ChangeCount)>();
        List<ContainerSnapshot>? restoreSnapshots = null;
        using (AcquireLocks(GetContainers(offer, offer, character, character)))
        {
            if (!character.MoneyPouch.Contains(CoinsItemId, coins.Count) || !offer.HasSpaceFor(coins))
            {
                return false;
            }

            var snapshots = CaptureSnapshots(offer, character.Inventory, character.MoneyPouch);
            if (!offer.TryAddRangeForTradeStorage([coins], out var offerSlots))
            {
                return false;
            }

            if (!character.MoneyPouch.TryRemoveForTradeStorage(coins.Count, out var pouchChangeCount,
                    out var inventorySlots))
            {
                RestoreSnapshotsStorage(snapshots);
                restoreSnapshots = snapshots;
            }
            else
            {
                RecordChangedSlots(changes, offer, offerSlots);
                RecordChangedSlots(changes, character.Inventory, inventorySlots);
                pouchMessages.Add((character.MoneyPouch, pouchChangeCount));
            }
        }

        return FinishMoneyPouchTransfer(changes, pouchMessages, restoreSnapshots);
    }

    internal static bool TryReturnMoneyToPouch(ICharacter character, ITradeItemContainer offer, IItem coins,
        int preferredSlot)
    {
        var changes = CreateChanges();
        var pouchMessages = new List<(IMoneyPouchContainer Pouch, int ChangeCount)>();
        List<ContainerSnapshot>? restoreSnapshots = null;
        using (AcquireLocks(GetContainers(offer, offer, character, character)))
        {
            var snapshots = CaptureSnapshots(offer, character.Inventory, character.MoneyPouch);
            if (!offer.TryRemoveForTradeStorage(coins, preferredSlot, out var offerSlots))
            {
                return false;
            }

            if (!character.MoneyPouch.TryAddForTradeStorage(coins.Count, out var pouchChangeCount,
                    out var inventorySlots))
            {
                RestoreSnapshotsStorage(snapshots);
                restoreSnapshots = snapshots;
            }
            else
            {
                RecordChangedSlots(changes, offer, offerSlots);
                RecordChangedSlots(changes, character.Inventory, inventorySlots);
                pouchMessages.Add((character.MoneyPouch, pouchChangeCount));
            }
        }

        return FinishMoneyPouchTransfer(changes, pouchMessages, restoreSnapshots);
    }

    private static bool FinishMoneyPouchTransfer(Dictionary<IItemContainer, HashSet<int>> changes,
        IEnumerable<(IMoneyPouchContainer Pouch, int ChangeCount)> pouchMessages,
        List<ContainerSnapshot>? restoreSnapshots)
    {
        if (restoreSnapshots != null)
        {
            PublishRestoredSnapshots(restoreSnapshots);
            return false;
        }

        PublishChanges(changes);
        PublishPouchMessages(pouchMessages);
        return true;
    }

    private static bool Receive(ICharacter character, IReadOnlyList<IItem> items,
        Dictionary<IItemContainer, HashSet<int>> changes,
        ICollection<(IMoneyPouchContainer Pouch, int ChangeCount)> pouchMessages)
    {
        var nonCoinItems = items.Where(item => item.Id != CoinsItemId).ToArray();
        if (nonCoinItems.Length > 0)
        {
            if (!character.Inventory.TryAddRangeForTradeStorage(nonCoinItems, out var inventorySlots))
            {
                return false;
            }

            RecordChangedSlots(changes, character.Inventory, inventorySlots);
        }

        var coinCount = items.Where(item => item.Id == CoinsItemId).Sum(item => (long)item.Count);
        if (coinCount <= 0)
        {
            return true;
        }

        if (coinCount > int.MaxValue || !character.MoneyPouch.TryAddForTradeStorage((int)coinCount,
                out var pouchChangeCount, out var coinInventorySlots))
        {
            return false;
        }

        RecordChangedSlots(changes, character.Inventory, coinInventorySlots);
        pouchMessages.Add((character.MoneyPouch, pouchChangeCount));
        return true;
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
            .OfType<BaseItemContainer>()
            .Distinct()
            .Select(container =>
            {
                var items = new IItem[container.Capacity];
                for (var slot = 0; slot < items.Length; slot++)
                {
                    items[slot] = container[slot]!;
                }

                var counts = items.Select(item => item?.Count ?? 0).ToArray();
                return new ContainerSnapshot(container, items, counts);
            })
            .ToList();

    private static void RestoreSnapshotsStorage(IEnumerable<ContainerSnapshot> snapshots)
    {
        foreach (var snapshot in snapshots)
        {
            for (var i = 0; i < snapshot.Items.Length; i++)
            {
                if (snapshot.Items[i] != null)
                {
                    snapshot.Items[i]!.Count = snapshot.Counts[i];
                }
            }

            snapshot.Container.SetItems(snapshot.Items, false);
        }
    }

    private static void PublishRestoredSnapshots(IEnumerable<ContainerSnapshot> snapshots)
    {
        foreach (var snapshot in snapshots)
        {
            snapshot.Container.OnUpdate();
        }
    }

    private static Dictionary<IItemContainer, HashSet<int>> CreateChanges() =>
        new(ReferenceEqualityComparer.Instance);

    private static void RecordChangedSlots(Dictionary<IItemContainer, HashSet<int>> changes,
        IItemContainer container, IEnumerable<int> slots)
    {
        if (!changes.TryGetValue(container, out var changedSlots))
        {
            changedSlots = [];
            changes.Add(container, changedSlots);
        }

        changedSlots.UnionWith(slots);
    }

    private static void PublishChanges(Dictionary<IItemContainer, HashSet<int>> changes)
    {
        foreach (var (container, slots) in changes)
        {
            if (slots.Count > 0)
            {
                container.OnUpdate(slots);
            }
        }
    }

    private static void PublishPouchMessages(IEnumerable<(IMoneyPouchContainer Pouch, int ChangeCount)> changes)
    {
        foreach (var (pouch, changeCount) in changes)
        {
            pouch.PublishTradeChanges(changeCount);
        }
    }

    private static HashSet<int> GetOccupiedSlots(IItemContainer container)
    {
        var slots = new HashSet<int>();
        for (var slot = 0; slot < container.Capacity; slot++)
        {
            if (container[slot] != null)
            {
                slots.Add(slot);
            }
        }

        return slots;
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
        if (container is TradeItemContainer tradeContainer &&
            !containers.Any(existing => ReferenceEquals(existing.MutationLock, tradeContainer.MutationLock)))
        {
            containers.Add(tradeContainer);
        }
    }

    private static LockScope AcquireLocks(IEnumerable<TradeItemContainer> containers) =>
        new(containers.OrderBy(container => container.MutationOrder));

    private sealed record ContainerSnapshot(BaseItemContainer Container, IItem[] Items, int[] Counts);

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
