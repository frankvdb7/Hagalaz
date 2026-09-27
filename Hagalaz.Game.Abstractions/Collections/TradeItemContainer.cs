using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>
/// Base implementation for containers that explicitly participate in trade settlement.
/// </summary>
public abstract class TradeItemContainer : BaseItemContainer, ITradeItemContainer
{
    /// <summary>
    /// Gets the synchronization boundary shared by trade operations on this container.
    /// </summary>
    public object MutationLock => ContainerMutationLock;

    /// <summary>
    /// Gets the stable order used when trade operations lock multiple containers.
    /// </summary>
    public long MutationOrder => ContainerMutationOrder;

    protected TradeItemContainer(StorageType type, int capacity)
        : base(type, capacity)
    {
    }

    protected TradeItemContainer(StorageType type, IEnumerable<IItem> items, int capacity)
        : base(type, items, capacity)
    {
    }

    /// <inheritdoc />
    public bool AddRangeForTrade(IEnumerable<IItem?> items)
    {
        if (!TryAddRangeForTradeStorage(items, out var slotsToUpdate))
        {
            return false;
        }

        OnUpdate(slotsToUpdate);
        return true;
    }

    /// <inheritdoc />
    public bool TryAddRangeForTradeStorage(IEnumerable<IItem?> items, out HashSet<int> changedSlots)
    {
        ArgumentNullException.ThrowIfNull(items);
        changedSlots = [];
        lock (MutationLock)
        {
            var itemsBefore = (IItem?[])Items.Clone();
            var countsBefore = new int[itemsBefore.Length];
            for (var i = 0; i < itemsBefore.Length; i++)
            {
                countsBefore[i] = itemsBefore[i]?.Count ?? 0;
            }

            if (!AddRangeCore(items, out changedSlots))
            {
                RestoreSnapshot(itemsBefore, countsBefore);
                changedSlots.Clear();
                return false;
            }

            AdvanceRevision();
        }

        return true;
    }

    private void RestoreSnapshot(IItem?[] items, IReadOnlyList<int> counts)
    {
        for (var i = 0; i < items.Length; i++)
        {
            if (items[i] != null)
            {
                items[i]!.Count = counts[i];
            }
        }

        Items = items;
    }

    /// <inheritdoc />
    public bool RemoveForTrade(IItem item, int preferredSlot = -1)
    {
        if (!TryRemoveForTradeStorage(item, preferredSlot, out var slotsToUpdate))
        {
            return false;
        }

        OnUpdate(slotsToUpdate);
        return true;
    }

    /// <inheritdoc />
    public bool TryRemoveForTradeStorage(IItem item, int preferredSlot, out HashSet<int> changedSlots)
    {
        ArgumentNullException.ThrowIfNull(item);
        changedSlots = [];
        lock (MutationLock)
        {
            if (!TryRemoveExactCore(item, item.Count, preferredSlot, out changedSlots))
            {
                changedSlots.Clear();
                return false;
            }
        }

        return true;
    }

}
