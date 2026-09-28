using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Coordinates exact transfers between domain-owned item containers.</summary>
public static class ItemContainerTransfer
{
    public static bool TryTransfer(
        IItemContainer source,
        IItemContainer destination,
        IItem item,
        int count,
        int preferredSourceSlot = -1,
        int destinationSlot = -1,
        IItem? destinationItem = null)
    {
        if (!TryTransferStorage(source, destination, item, count, preferredSourceSlot, destinationSlot,
                destinationItem, out var sourceSlots, out var destinationSlots))
        {
            return false;
        }

        source.PublishChanges(sourceSlots);
        destination.PublishChanges(destinationSlots);
        return true;
    }

    public static bool TryTransferStorage(
        IItemContainer source,
        IItemContainer destination,
        IItem item,
        int count,
        int preferredSourceSlot,
        int destinationSlot,
        IItem? destinationItem,
        out HashSet<int> sourceChangedSlots,
        out HashSet<int> destinationChangedSlots)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(item);

        var sourceStorage = GetStorage(source);
        var destinationStorage = GetStorage(destination);
        if (!ItemContainerStorage.TryTransfer(sourceStorage, destinationStorage, item, count,
                preferredSourceSlot, destinationSlot, destinationItem,
                out sourceChangedSlots, out destinationChangedSlots))
        {
            sourceChangedSlots.Clear();
            destinationChangedSlots.Clear();
            return false;
        }

        return true;
    }

    public static void AddAndRemoveFrom(IItemContainer destination, IItemContainer source)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(source);
        if (ReferenceEquals(destination, source))
        {
            return;
        }

        for (var slot = 0; slot < source.Capacity; slot++)
        {
            var item = source[slot];
            if (item == null || item.Count <= 0)
            {
                continue;
            }

            TryTransfer(source, destination, item, item.Count, slot);
        }
    }

    private static ItemContainerStorage GetStorage(IItemContainer container)
    {
        if (container is not IItemContainerStorageProvider provider)
        {
            throw new ArgumentException("Container must provide composed item storage.", nameof(container));
        }

        return provider.Storage;
    }
}
