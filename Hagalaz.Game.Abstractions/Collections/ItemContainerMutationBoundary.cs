using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Coordinates mutations and publication for one owned item storage.</summary>
public sealed class ItemContainerMutationBoundary
{
    private readonly ItemContainerStorage _storage;
    private readonly Action<HashSet<int>?>? _publishChanges;

    internal ItemContainerStorage Storage => _storage;

    internal ItemContainerMutationBoundary(ItemContainerStorage storage, Action<HashSet<int>?>? publishChanges)
    {
        ArgumentNullException.ThrowIfNull(storage);
        _storage = storage;
        _publishChanges = publishChanges;
    }

    /// <summary>Transfers an exact quantity to another boundary and publishes both committed sides.</summary>
    public bool TryTransferTo(
        ItemContainerMutationBoundary destination,
        IItem item,
        int count,
        int preferredSourceSlot = -1,
        int destinationSlot = -1,
        IItem? destinationItem = null)
    {
        if (!TryTransferToStorage(destination, item, count, preferredSourceSlot, destinationSlot,
                destinationItem, out var sourceSlots, out var destinationSlots))
        {
            return false;
        }

        PublishChanges(sourceSlots);
        destination.PublishChanges(destinationSlots);
        return true;
    }

    /// <summary>Moves all occupied source slots using exact transfers.</summary>
    public void AddAndRemoveFrom(ItemContainerMutationBoundary source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (ReferenceEquals(this, source)) return;

        for (var slot = 0; slot < source.Storage.Capacity; slot++)
        {
            var item = source.Storage[slot];
            if (item is { Count: > 0 })
            {
                source.TryTransferTo(this, item, item.Count, slot);
            }
        }
    }

    internal bool TryTransferToStorage(
        ItemContainerMutationBoundary destination,
        IItem item,
        int count,
        int preferredSourceSlot,
        int destinationSlot,
        IItem? destinationItem,
        out HashSet<int> sourceChangedSlots,
        out HashSet<int> destinationChangedSlots)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(item);

        var transferred = false;
        sourceChangedSlots = [];
        destinationChangedSlots = [];
        HashSet<int> sourceSlots = [];
        HashSet<int> destinationSlots = [];
        WithLocks([this, destination], () => transferred = ItemContainerStorage.TryTransfer(
            _storage, destination._storage, item, count, preferredSourceSlot, destinationSlot,
            destinationItem, out sourceSlots, out destinationSlots));
        sourceChangedSlots = sourceSlots;
        destinationChangedSlots = destinationSlots;
        return transferred;
    }

    internal void PublishChanges(HashSet<int>? changedSlots) => _publishChanges?.Invoke(changedSlots);

    internal void WithLocks(IEnumerable<ItemContainerMutationBoundary> boundaries, Action operation)
    {
        var storages = boundaries.Select(boundary => boundary._storage)
            .Distinct()
            .OrderBy(storage => storage.MutationOrder)
            .ToArray();
        WithLockAt(0);

        void WithLockAt(int index)
        {
            if (index == storages.Length)
            {
                operation();
                return;
            }

            lock (storages[index].MutationLock)
            {
                WithLockAt(index + 1);
            }
        }
    }
}
