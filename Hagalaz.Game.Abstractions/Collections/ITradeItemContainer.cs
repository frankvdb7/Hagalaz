using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>
/// Marks a container that participates in checked trade mutations.
/// </summary>
public interface ITradeItemContainer : IItemContainer
{
    bool AddRangeForTrade(IEnumerable<IItem?> items);

    bool TryAddRangeForTradeStorage(IEnumerable<IItem?> items, out HashSet<int> changedSlots);

    bool RemoveForTrade(IItem item, int preferredSlot = -1);

    bool TryRemoveForTradeStorage(IItem item, int preferredSlot, out HashSet<int> changedSlots);
}
