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
    /// Removes the item as one checked trade operation.
    /// </summary>
    bool RemoveForTrade(IItem item, int preferredSlot = -1);
}
