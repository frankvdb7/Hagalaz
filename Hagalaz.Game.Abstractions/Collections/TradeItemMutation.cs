using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>
/// Stores the exact internal delta of one checked trade-container operation.
/// </summary>
internal sealed class TradeItemMutation
{
    internal enum RollbackOutcome
    {
        Failed,
        Restored
    }

    private readonly int _appliedCount;
    private readonly IReadOnlyList<IItem> _appliedItems;
    private readonly Func<RollbackOutcome> _rollback;
    private bool _stateRestored;

    internal TradeItemMutation(
        int appliedCount,
        bool succeeded,
        bool notificationSucceeded,
        IReadOnlyList<IItem> appliedItems,
        Func<RollbackOutcome> rollback)
    {
        _appliedCount = appliedCount;
        Succeeded = succeeded;
        NotificationSucceeded = notificationSucceeded;
        _appliedItems = appliedItems;
        _rollback = rollback;
    }

    internal int AppliedCount => _stateRestored ? 0 : _appliedCount;

    internal bool Succeeded { get; }

    internal bool NotificationSucceeded { get; }

    internal IReadOnlyList<IItem> AppliedItems => _stateRestored ? [] : _appliedItems;

    internal bool TryRollback()
    {
        if (_appliedCount == 0 || _stateRestored)
        {
            return true;
        }

        if (_rollback() != RollbackOutcome.Restored)
        {
            return false;
        }

        _stateRestored = true;
        return true;
    }
}
