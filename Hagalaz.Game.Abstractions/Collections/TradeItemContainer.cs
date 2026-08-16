using System;
using System.Collections.Generic;
using System.Threading;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>
/// Base implementation for containers that explicitly participate in trade settlement.
/// </summary>
public abstract class TradeItemContainer : BaseItemContainer, ITradeItemContainer
{
    private static long _nextMutationOrder;
    private readonly object _mutationLock = new();

    /// <summary>
    /// Gets the synchronization boundary shared by trade operations on this container.
    /// </summary>
    internal object MutationLock => _mutationLock;

    /// <summary>
    /// Gets the stable order used when trade operations lock multiple containers.
    /// </summary>
    internal long MutationOrder { get; } = Interlocked.Increment(ref _nextMutationOrder);

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
            if (!AddRangeCore(items, out slotsToUpdate))
            {
                return false;
            }
        }

        NotifyTradeUpdate(slotsToUpdate);
        AdvanceRevision();
        return true;
    }

    /// <inheritdoc />
    public bool RemoveForTrade(IItem item, int preferredSlot = -1)
    {
        ArgumentNullException.ThrowIfNull(item);

        HashSet<int> slotsToUpdate;
        lock (MutationLock)
        {
            if (!RemoveExact(item, preferredSlot, out slotsToUpdate))
            {
                return false;
            }
        }

        NotifyTradeUpdate(slotsToUpdate);
        AdvanceRevision();
        return true;
    }

    private bool RemoveExact(IItem item, int preferredSlot, out HashSet<int> slotsToUpdate)
    {
        slotsToUpdate = [];
        if (GetCount(item) < item.Count)
        {
            return false;
        }

        slotsToUpdate = new HashSet<int>();
        var remaining = item.Count;
        var slot = preferredSlot >= 0 && preferredSlot < Capacity && Items[preferredSlot]?.Equals(item, true) == true
            ? preferredSlot
            : -1;

        while (remaining > 0)
        {
            if (slot < 0 || Items[slot] == null || !Items[slot]!.Equals(item, true))
            {
                slot = FindMatchingSlot(item);
                if (slot < 0)
                {
                    return false;
                }
            }

            var slotItem = Items[slot]!;
            var removed = Math.Min(slotItem.Count, remaining);
            if (removed == slotItem.Count && CountToResetTo == -1)
            {
                Items[slot] = null;
            }
            else if (removed == slotItem.Count && CountToResetTo != -1)
            {
                slotItem.Count = CountToResetTo;
            }
            else
            {
                slotItem.Count -= removed;
            }

            slotsToUpdate.Add(slot);
            remaining -= removed;
            slot = -1;
        }

        return true;
    }

    private int FindMatchingSlot(IItem item)
    {
        for (var i = 0; i < Items.Length; i++)
        {
            if (Items[i]?.Equals(item, true) == true)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Delivers a checked trade update without turning observer failures into a
    /// failed settlement. Normal inherited mutations do not use this path.
    /// </summary>
    protected void NotifyTradeUpdate(HashSet<int>? slots = null)
    {
        try
        {
            OnUpdate(slots);
        }
        catch (InvalidOperationException)
        {
            // Storage mutation has already committed; observer delivery is best effort.
        }
    }
}
