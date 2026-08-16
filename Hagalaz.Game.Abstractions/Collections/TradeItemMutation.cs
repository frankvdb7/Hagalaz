using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>
/// Represents one exact item mutation owned by a trade operation.
/// </summary>
public sealed class TradeItemMutation
{
    internal enum RollbackOutcome
    {
        Failed,
        Restored,
        RestoredWithNotificationFailure
    }

    private readonly int _appliedCount;
    private readonly IReadOnlyList<IItem> _appliedItems;
    private readonly Func<RollbackOutcome> _rollback;
    private bool _stateRestored;
    private bool _notificationPending;

    internal TradeItemMutation(
        int appliedCount,
        bool succeeded,
        IReadOnlyList<IItem> appliedItems,
        Func<RollbackOutcome> rollback)
    {
        _appliedCount = appliedCount;
        Succeeded = succeeded;
        _appliedItems = appliedItems;
        _rollback = rollback;
    }

    /// <summary>
    /// Gets the number of units still applied by this mutation.
    /// </summary>
    public int AppliedCount => _stateRestored ? 0 : _appliedCount;

    /// <summary>
    /// Gets whether the requested mutation was applied completely.
    /// </summary>
    public bool Succeeded { get; }

    /// <summary>
    /// Gets the exact item delta still applied by an add operation.
    /// </summary>
    public IReadOnlyList<IItem> AppliedItems => _stateRestored ? [] : _appliedItems;

    /// <summary>
    /// Gets whether this mutation still changes the container.
    /// </summary>
    public bool HasChanges => AppliedCount > 0;

    /// <summary>
    /// Attempts to remove only this trade's applied delta and notify the container observers.
    /// Unrelated changes are preserved when the exact delta remains identifiable.
    /// </summary>
    public bool TryRollback()
    {
        if (_appliedCount == 0 || (_stateRestored && !_notificationPending))
        {
            return true;
        }

        var outcome = _rollback();
        switch (outcome)
        {
            case RollbackOutcome.Restored:
                _stateRestored = true;
                _notificationPending = false;
                return true;
            case RollbackOutcome.RestoredWithNotificationFailure:
                _stateRestored = true;
                _notificationPending = true;
                return false;
            default:
                return false;
        }
    }
}
