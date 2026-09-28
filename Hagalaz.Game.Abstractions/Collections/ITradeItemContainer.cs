using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>
/// Marks a container that participates in checked trade mutations.
/// </summary>
public interface ITradeItemContainer : IItemContainer
{
    bool TryAddRangeForTradeStorage(IEnumerable<IItem?> items, out HashSet<int> changedSlots) =>
        ((IItemContainerStorageProvider)this).Storage.TryAddRange(items, out changedSlots);
}
