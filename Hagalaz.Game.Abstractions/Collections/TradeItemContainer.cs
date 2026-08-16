using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>
/// Base implementation for containers that explicitly participate in trade settlement.
/// </summary>
public abstract class TradeItemContainer : BaseItemContainer, ITradeItemContainer
{
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

        lock (MutationLock)
        {
            return base.AddRange(items);
        }
    }

    /// <inheritdoc />
    public bool RemoveForTrade(IItem item, int preferredSlot = -1)
    {
        ArgumentNullException.ThrowIfNull(item);

        lock (MutationLock)
        {
            return RemoveExact(item, preferredSlot);
        }
    }

    private bool RemoveExact(IItem item, int preferredSlot)
    {
        if (GetCount(item) < item.Count)
        {
            return false;
        }

        var slots = new HashSet<int>();
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

            slots.Add(slot);
            remaining -= removed;
            slot = -1;
        }

        NotifyUpdate(slots);
        AdvanceRevision();
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
    /// Delivers a trade-container update without turning observer failures into a failed settlement.
    /// </summary>
    protected override void NotifyUpdate(HashSet<int>? slots = null)
    {
        try
        {
            base.NotifyUpdate(slots);
        }
        catch (InvalidOperationException)
        {
            // Storage mutation has already committed; observer delivery is best effort.
        }
    }
}
