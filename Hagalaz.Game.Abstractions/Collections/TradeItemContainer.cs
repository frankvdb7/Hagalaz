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
        ArgumentNullException.ThrowIfNull(items);

        HashSet<int> slotsToUpdate;
        lock (MutationLock)
        {
            var itemsBefore = (IItem?[])Items.Clone();
            var countsBefore = new int[itemsBefore.Length];
            for (var i = 0; i < itemsBefore.Length; i++)
            {
                countsBefore[i] = itemsBefore[i]?.Count ?? 0;
            }

            if (!AddRangeCore(items, out slotsToUpdate))
            {
                RestoreSnapshot(itemsBefore, countsBefore);
                return false;
            }

            AdvanceRevision();
        }

        OnUpdate(slotsToUpdate);
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
        ArgumentNullException.ThrowIfNull(item);

        HashSet<int> slotsToUpdate;
        lock (MutationLock)
        {
            if (!TryRemoveExactCore(item, item.Count, preferredSlot, out slotsToUpdate))
            {
                return false;
            }
        }

        OnUpdate(slotsToUpdate);
        return true;
    }

}
