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

    public static bool TryExchange(ICharacter first, ItemContainer firstOffer, ICharacter second,
        ItemContainer secondOffer, IItemBuilder itemBuilder) =>
        TryCompleteTrade(first, firstOffer, second, secondOffer, itemBuilder);

    public static bool TryRefund(ICharacter first, ItemContainer firstOffer, ICharacter second,
        ItemContainer secondOffer, IItemBuilder itemBuilder) =>
        TryRefundTrade(first, firstOffer, second, secondOffer, itemBuilder);

    internal static bool TryCompleteTrade(ICharacter first, ItemContainer firstOffer, ICharacter second,
        ItemContainer secondOffer, IItemBuilder itemBuilder) =>
        TryExchangeOffers(first, firstOffer, second, secondOffer, secondOffer, firstOffer, itemBuilder);

    internal static bool TryRefundTrade(ICharacter first, ItemContainer firstOffer, ICharacter second,
        ItemContainer secondOffer, IItemBuilder itemBuilder) =>
        TryExchangeOffers(first, firstOffer, second, secondOffer, firstOffer, secondOffer, itemBuilder);

    private static bool TryExchangeOffers(ICharacter first, ItemContainer firstOffer, ICharacter second,
        ItemContainer secondOffer, ItemContainer itemsForFirst, ItemContainer itemsForSecond,
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

            var recipientSnapshots = CaptureSnapshots((IItemContainerStorageOwner)first.Inventory.Items,
                (IItemContainerStorageOwner)first.MoneyPouch, (IItemContainerStorageOwner)second.Inventory.Items,
                (IItemContainerStorageOwner)second.MoneyPouch);
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
    internal static bool TryConserveEscrow(ICharacter first, ItemContainer firstOffer, ICharacter second,
        ItemContainer secondOffer)
    {
        var containers = new List<ItemContainerStorage>();
        AddContainer(containers, firstOffer);
        AddContainer(containers, secondOffer);
        AddContainer(containers, (IItemContainerStorageOwner?)first.Rewards?.Items);
        AddContainer(containers, (IItemContainerStorageOwner?)first.Bank?.Items);
        AddContainer(containers, (IItemContainerStorageOwner?)second.Rewards?.Items);
        AddContainer(containers, (IItemContainerStorageOwner?)second.Bank?.Items);
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

            var conservationSnapshots = CaptureSnapshots(firstOffer, secondOffer,
                (IItemContainerStorageOwner?)firstDestination, (IItemContainerStorageOwner?)secondDestination);
            HashSet<int> firstChangedSlots = [];
            HashSet<int> secondChangedSlots = [];
            var failed = false;
            if (firstDestination != null && firstItems.Length > 0 &&
                !GetStorage((IItemContainerStorageOwner)firstDestination).TryAddRange(firstItems, out firstChangedSlots))
            {
                RestoreSnapshotsStorage(conservationSnapshots);
                restoreSnapshots = conservationSnapshots;
                failed = true;
            }

            if (!failed && secondDestination != null && secondItems.Length > 0 &&
                !GetStorage((IItemContainerStorageOwner)secondDestination).TryAddRange(secondItems, out secondChangedSlots))
            {
                RestoreSnapshotsStorage(conservationSnapshots);
                restoreSnapshots = conservationSnapshots;
                failed = true;
            }

            if (!failed)
            {
                if (firstDestination != null && firstItems.Length > 0)
                    RecordChangedSlots(changes, (IItemContainerStorageOwner)firstDestination, firstChangedSlots);
                if (secondDestination != null && secondItems.Length > 0)
                    RecordChangedSlots(changes, (IItemContainerStorageOwner)secondDestination, secondChangedSlots);

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

    internal static bool TryOfferMoneyFromPouch(ICharacter character, ItemContainer offer, IItem coins)
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

            var snapshots = CaptureSnapshots(offer, (IItemContainerStorageOwner)character.Inventory.Items,
                (IItemContainerStorageOwner)character.MoneyPouch);
            if (!GetStorage(offer).TryAddRange([coins], out var offerSlots))
            {
                return false;
            }

            if (!character.MoneyPouch.TryRemoveExactStorage(coins.Count, out var pouchChangeCount,
                    out var inventorySlots))
            {
                RestoreSnapshotsStorage(snapshots);
                restoreSnapshots = snapshots;
            }
            else
            {
                RecordChangedSlots(changes, offer, offerSlots);
                RecordChangedSlots(changes, (IItemContainerStorageOwner)character.Inventory.Items, inventorySlots);
                pouchMessages.Add((character.MoneyPouch, pouchChangeCount));
            }
        }

        return FinishMoneyPouchTransfer(changes, pouchMessages, restoreSnapshots);
    }

    internal static bool TryReturnMoneyToPouch(ICharacter character, ItemContainer offer, IItem coins,
        int preferredSlot)
    {
        var changes = CreateChanges();
        var pouchMessages = new List<(IMoneyPouchContainer Pouch, int ChangeCount)>();
        List<ContainerSnapshot>? restoreSnapshots = null;
        using (AcquireLocks(GetContainers(offer, offer, character, character)))
        {
            var snapshots = CaptureSnapshots(offer, (IItemContainerStorageOwner)character.Inventory.Items,
                (IItemContainerStorageOwner)character.MoneyPouch);
            if (!GetStorage(offer).TryRemoveExact(coins, preferredSlot, out var offerSlots))
            {
                return false;
            }

            if (!character.MoneyPouch.TryAddExactStorage(coins.Count, out var pouchChangeCount,
                    out var inventorySlots))
            {
                RestoreSnapshotsStorage(snapshots);
                restoreSnapshots = snapshots;
            }
            else
            {
                RecordChangedSlots(changes, offer, offerSlots);
                RecordChangedSlots(changes, (IItemContainerStorageOwner)character.Inventory.Items, inventorySlots);
                pouchMessages.Add((character.MoneyPouch, pouchChangeCount));
            }
        }

        return FinishMoneyPouchTransfer(changes, pouchMessages, restoreSnapshots);
    }

    private static bool FinishMoneyPouchTransfer(Dictionary<IItemContainerStorageOwner, HashSet<int>> changes,
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
        Dictionary<IItemContainerStorageOwner, HashSet<int>> changes,
        ICollection<(IMoneyPouchContainer Pouch, int ChangeCount)> pouchMessages)
    {
        var nonCoinItems = items.Where(item => item.Id != CoinsItemId).ToArray();
        if (nonCoinItems.Length > 0)
        {
            if (!GetStorage((IItemContainerStorageOwner)character.Inventory.Items).TryAddRange(nonCoinItems, out var inventorySlots))
            {
                return false;
            }

            RecordChangedSlots(changes, (IItemContainerStorageOwner)character.Inventory.Items, inventorySlots);
        }

        var coinCount = items.Where(item => item.Id == CoinsItemId).Sum(item => (long)item.Count);
        if (coinCount <= 0)
        {
            return true;
        }

        if (coinCount > int.MaxValue || !character.MoneyPouch.TryAddExactStorage((int)coinCount,
                out var pouchChangeCount, out var coinInventorySlots))
        {
            return false;
        }

        RecordChangedSlots(changes, (IItemContainerStorageOwner)character.Inventory.Items, coinInventorySlots);
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

        return character.Inventory.Items.HasSpaceForRange(recipientItems);
    }

    private static IItemContainer? GetRecoveryContainer(ICharacter character, IReadOnlyList<IItem> items)
    {
        if (items.Count == 0)
        {
            return character.Rewards?.Items;
        }

        if (character.Rewards != null && character.Rewards.Items.HasSpaceForRange(items))
        {
            return character.Rewards.Items;
        }

        return character.Bank != null && character.Bank.Items.HasSpaceForRange(items) ? character.Bank.Items : null;
    }

    private static IItem[] SnapshotItems(ItemContainer container) =>
        container.OfType<IItem>().Select(item => item.Clone()).ToArray();

    private static List<ContainerSnapshot> CaptureSnapshots(params IItemContainerStorageOwner?[] containers) =>
        containers
            .OfType<IItemContainerStorageOwner>()
            .Select(provider => provider.Storage)
            .Distinct()
            .Select(storage =>
            {
                var items = new IItem?[storage.Capacity];
                for (var slot = 0; slot < items.Length; slot++)
                {
                    items[slot] = storage[slot];
                }

                var counts = items.Select(item => item?.Count ?? 0).ToArray();
                var owner = containers.OfType<IItemContainerStorageOwner>().First(value =>
                    ReferenceEquals(value.Storage, storage));
                return new ContainerSnapshot(owner, storage, items, counts);
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

            snapshot.Storage.ReplaceState(snapshot.Items);
        }
    }

    private static void PublishRestoredSnapshots(IEnumerable<ContainerSnapshot> snapshots)
    {
        foreach (var snapshot in snapshots)
        {
            snapshot.Owner.PublishChanges(null);
        }
    }

    private static Dictionary<IItemContainerStorageOwner, HashSet<int>> CreateChanges() =>
        new(ReferenceEqualityComparer.Instance);

    private static void RecordChangedSlots(Dictionary<IItemContainerStorageOwner, HashSet<int>> changes,
        IItemContainerStorageOwner container, IEnumerable<int> slots)
    {
        if (!changes.TryGetValue(container, out var changedSlots))
        {
            changedSlots = [];
            changes.Add(container, changedSlots);
        }

        changedSlots.UnionWith(slots);
    }

    private static void PublishChanges(Dictionary<IItemContainerStorageOwner, HashSet<int>> changes)
    {
        foreach (var (container, slots) in changes)
        {
            if (slots.Count > 0)
            {
                container.PublishChanges(slots);
            }
        }
    }

    private static void PublishPouchMessages(IEnumerable<(IMoneyPouchContainer Pouch, int ChangeCount)> changes)
    {
        foreach (var (pouch, changeCount) in changes)
        {
            pouch.PublishChanges(changeCount);
        }
    }

    private static HashSet<int> GetOccupiedSlots(ItemContainer container)
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

    private static List<ItemContainerStorage> GetContainers(ItemContainer firstOffer, ItemContainer secondOffer,
        ICharacter first, ICharacter second)
    {
        var containers = new List<ItemContainerStorage>();
        AddContainer(containers, firstOffer);
        AddContainer(containers, secondOffer);
        AddContainer(containers, (IItemContainerStorageOwner)first.Inventory.Items);
        AddContainer(containers, (IItemContainerStorageOwner)second.Inventory.Items);
        AddContainer(containers, (IItemContainerStorageOwner)first.MoneyPouch);
        AddContainer(containers, (IItemContainerStorageOwner)second.MoneyPouch);
        return containers;
    }

    private static void AddContainer(List<ItemContainerStorage> containers, IItemContainerStorageOwner? container)
    {
        if (container != null && !containers.Any(existing => ReferenceEquals(existing, container.Storage)))
        {
            containers.Add(container.Storage);
        }
    }

    private static ItemContainerStorage GetStorage(IItemContainerStorageOwner container) => container.Storage;

    private static LockScope AcquireLocks(IEnumerable<ItemContainerStorage> containers) =>
        new(containers.OrderBy(storage => storage.MutationOrder));

    private sealed record ContainerSnapshot(IItemContainerStorageOwner Owner, ItemContainerStorage Storage, IItem?[] Items, int[] Counts);

    private sealed class LockScope : IDisposable
    {
        private readonly IReadOnlyList<ItemContainerStorage> _containers;

        public LockScope(IEnumerable<ItemContainerStorage> containers)
        {
            _containers = containers.ToArray();
            foreach (var storage in _containers)
            {
                Monitor.Enter(storage.MutationLock);
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
