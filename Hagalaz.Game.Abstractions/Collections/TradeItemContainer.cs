using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>
/// Base implementation for containers that explicitly participate in trade mutations.
/// </summary>
public abstract class TradeItemContainer : BaseItemContainer, ITradeItemContainer
{
    /// <summary>
    /// Initializes a trade-capable container with the specified storage behavior and capacity.
    /// </summary>
    protected TradeItemContainer(StorageType type, int capacity)
        : base(type, capacity)
    {
    }

    /// <summary>
    /// Initializes a trade-capable container with the specified storage behavior, items, and capacity.
    /// </summary>
    protected TradeItemContainer(StorageType type, IEnumerable<IItem> items, int capacity)
        : base(type, items, capacity)
    {
    }

    /// <inheritdoc />
    public ItemContainerMutation AddRangeForTrade(IEnumerable<IItem?> items) => AddRangeCheckedCore(items);

    /// <inheritdoc />
    public ItemContainerMutation RemoveForTrade(IItem item, int preferredSlot = -1) =>
        RemoveCheckedCore(item, preferredSlot);
}
