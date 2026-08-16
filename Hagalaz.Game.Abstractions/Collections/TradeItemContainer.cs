using System;
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
    public bool AddRangeForTrade(IEnumerable<IItem?> items)
    {
        var mutation = AddRangeForTradeMutation(items);
        if (mutation.Succeeded)
        {
            return true;
        }

        RollbackOrThrow(mutation);
        return false;
    }

    /// <inheritdoc />
    public bool RemoveForTrade(IItem item, int preferredSlot = -1)
    {
        var mutation = RemoveForTradeMutation(item, preferredSlot);
        if (mutation.Succeeded)
        {
            return true;
        }

        RollbackOrThrow(mutation);
        return false;
    }

    internal TradeItemMutation AddRangeForTradeMutation(IEnumerable<IItem?> items) => AddRangeCheckedCore(items);

    internal TradeItemMutation RemoveForTradeMutation(IItem item, int preferredSlot = -1) =>
        RemoveCheckedCore(item, preferredSlot);

    private static void RollbackOrThrow(TradeItemMutation mutation)
    {
        if (!mutation.TryRollback())
        {
            throw new InvalidOperationException("Trade container operation could not be restored.");
        }
    }
}
