using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>
/// Represents one exact item mutation owned by a trade operation.
/// </summary>
public sealed class TradeItemMutation
{
    private readonly int _appliedCount;
    private readonly IReadOnlyList<IItem> _appliedItems;
    private readonly Func<bool> _rollback;
    private bool _rolledBack;

    internal TradeItemMutation(
        int appliedCount,
        bool succeeded,
        IReadOnlyList<IItem> appliedItems,
        Func<bool> rollback)
    {
        _appliedCount = appliedCount;
        Succeeded = succeeded;
        _appliedItems = appliedItems;
        _rollback = rollback;
    }

    /// <summary>
    /// Gets the number of units still applied by this mutation.
    /// </summary>
    public int AppliedCount => _rolledBack ? 0 : _appliedCount;

    /// <summary>
    /// Gets whether the requested mutation was applied completely.
    /// </summary>
    public bool Succeeded { get; }

    /// <summary>
    /// Gets the exact item delta still applied by an add operation.
    /// </summary>
    public IReadOnlyList<IItem> AppliedItems => _rolledBack ? [] : _appliedItems;

    /// <summary>
    /// Gets whether this mutation still changes the container.
    /// </summary>
    public bool HasChanges => AppliedCount > 0;

    /// <summary>
    /// Attempts to restore the exact pre-mutation slot and item state.
    /// </summary>
    public bool TryRollback()
    {
        if (_rolledBack || _appliedCount == 0)
        {
            return true;
        }

        var restored = _rollback();
        if (restored)
        {
            _rolledBack = true;
        }

        return restored;
    }
}
