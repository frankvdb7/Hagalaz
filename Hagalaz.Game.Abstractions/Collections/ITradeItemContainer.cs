using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>
/// Provides the checked item mutations required by the trade lifecycle.
/// </summary>
public interface ITradeItemContainer : IItemContainer
{
    /// <summary>
    /// Adds all items as one checked trade operation.
    /// </summary>
    bool AddRangeForTrade(IEnumerable<IItem?> items);

    /// <summary>
    /// Adds all items atomically without publishing the changed slots.
    /// </summary>
    bool TryAddRangeForTradeStorage(IEnumerable<IItem?> items, out HashSet<int> changedSlots);

    /// <summary>
    /// Removes the item as one checked trade operation.
    /// </summary>
    bool RemoveForTrade(IItem item, int preferredSlot = -1);

    /// <summary>
    /// Removes the item atomically without publishing the changed slots.
    /// </summary>
    bool TryRemoveForTradeStorage(IItem item, int preferredSlot, out HashSet<int> changedSlots);
}
