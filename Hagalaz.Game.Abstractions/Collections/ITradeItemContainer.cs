using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>
/// Marks a container that participates in checked trade mutations.
/// </summary>
public interface ITradeItemContainer : IItemContainer
{
    bool AddRangeForTrade(IEnumerable<IItem?> items) => ItemContainerExtensions.AddRangeForTrade(this, items);

    bool TryAddRangeForTradeStorage(IEnumerable<IItem?> items, out HashSet<int> changedSlots) =>
        ItemContainerExtensions.TryAddRangeForTradeStorage(this, items, out changedSlots);

    bool RemoveForTrade(IItem item, int preferredSlot = -1) => ItemContainerExtensions.RemoveForTrade(this, item, preferredSlot);

    bool TryRemoveForTradeStorage(IItem item, int preferredSlot, out HashSet<int> changedSlots) =>
        ItemContainerExtensions.TryRemoveForTradeStorage(this, item, preferredSlot, out changedSlots);
}
