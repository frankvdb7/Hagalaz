using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>
/// Represents one checked item-container mutation and its exact applied delta.
/// </summary>
public sealed class ItemContainerMutation
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

    internal ItemContainerMutation(
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

    /// <summary>
    /// Gets the number of units still applied by this mutation.
    /// </summary>
    public int AppliedCount => _stateRestored ? 0 : _appliedCount;

    /// <summary>
    /// Gets whether the requested mutation was applied completely.
    /// </summary>
    public bool Succeeded { get; }

    /// <summary>
    /// Gets whether the observer notification completed without throwing.
    /// A failed notification does not undo a successful data mutation.
    /// </summary>
    public bool NotificationSucceeded { get; }

    /// <summary>
    /// Gets the exact item delta still applied by an add operation.
    /// </summary>
    public IReadOnlyList<IItem> AppliedItems => _stateRestored ? [] : _appliedItems;

    /// <summary>
    /// Gets whether this mutation still changes the container.
    /// </summary>
    public bool HasChanges => AppliedCount > 0;

    /// <summary>
    /// Attempts to remove only this mutation's applied delta and notify the container observers.
    /// Unrelated changes are preserved when the exact delta remains identifiable.
    /// </summary>
    public bool TryRollback()
    {
        if (_appliedCount == 0 || _stateRestored)
        {
            return true;
        }

        var outcome = _rollback();
        switch (outcome)
        {
            case RollbackOutcome.Restored:
                _stateRestored = true;
                return true;
            default:
                return false;
        }
    }
}
