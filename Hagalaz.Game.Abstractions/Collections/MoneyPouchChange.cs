using System;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Immutable pouch publication facts captured while staging a successful coin mutation.</summary>
public readonly struct MoneyPouchChange
{
    private readonly IMoneyPouchMutationBoundary _pouch;
    private readonly int _previousCount;
    private readonly int _changeCount;

    internal MoneyPouchChange(IMoneyPouchMutationBoundary pouch, int previousCount, int changeCount)
    {
        _pouch = pouch;
        _previousCount = previousCount;
        _changeCount = changeCount;
    }

    /// <summary>Publishes committed containers, then pouch changes, stopping at the first exception.</summary>
    public static void PublishChanges(ItemContainerTransaction transaction, params MoneyPouchChange?[] changes)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentNullException.ThrowIfNull(changes);
        if (!transaction.PublishChanges()) return;

        foreach (var change in changes)
        {
            if (change is not { } committedChange || committedChange._changeCount == 0) continue;
            committedChange._pouch.PublishChange(committedChange._previousCount, committedChange._changeCount);
        }
    }
}
