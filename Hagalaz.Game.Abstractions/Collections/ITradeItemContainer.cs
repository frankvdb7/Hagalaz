using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>
/// Provides the checked item mutations required by the trade lifecycle.
/// </summary>
public interface ITradeItemContainer : IItemContainer
{
    /// <summary>
    /// Adds items and records the exact trade delta for compensation.
    /// </summary>
    TradeItemMutation AddRangeForTrade(IEnumerable<IItem?> items);

    /// <summary>
    /// Removes items and records the exact trade delta for compensation.
    /// </summary>
    TradeItemMutation RemoveForTrade(IItem item, int preferredSlot = -1);
}
