using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Acquires and releases item-container synchronization monitors in deterministic order.</summary>
internal sealed class ItemContainerLockSet : IDisposable
{
    private readonly ItemContainerSynchronization[] _lockOrder;
    private int _locksAcquired;

    private ItemContainerLockSet(ItemContainerSynchronization[] synchronizations)
    {
        _lockOrder = synchronizations.OrderBy(synchronization => synchronization.LockOrder).ToArray();
    }

    internal static ItemContainerLockSet Acquire(IReadOnlyCollection<ItemContainerSynchronization> synchronizations)
    {
        var lockSet = new ItemContainerLockSet(Validate(synchronizations));
        while (true)
        {
            if (lockSet.TryAcquireForNewScope(out var blockedSynchronization)) return lockSet;
            blockedSynchronization!.WaitForNewScope();
        }
    }

    internal static bool TryAcquire(IReadOnlyCollection<ItemContainerSynchronization> synchronizations,
        out ItemContainerLockSet? lockSet)
    {
        lockSet = new ItemContainerLockSet(Validate(synchronizations));
        if (lockSet.TryAcquireForNewScope(out _)) return true;
        lockSet = null;
        return false;
    }

    /// <summary>Acquires monitors for a caller that already owns the corresponding logical scope.</summary>
    internal static ItemContainerLockSet AcquireForCurrentOwners(
        IReadOnlyCollection<ItemContainerSynchronization> synchronizations)
    {
        var lockSet = new ItemContainerLockSet(Validate(synchronizations));
        try
        {
            foreach (var synchronization in lockSet._lockOrder)
            {
                synchronization.EnterForCurrentOwner();
                lockSet._locksAcquired++;
            }

            return lockSet;
        }
        catch
        {
            lockSet.ReleaseLocks();
            throw;
        }
    }

    private static ItemContainerSynchronization[] Validate(
        IReadOnlyCollection<ItemContainerSynchronization> synchronizations)
    {
        ArgumentNullException.ThrowIfNull(synchronizations);
        if (synchronizations.Count == 0)
            throw new ArgumentException("At least one storage synchronization is required.", nameof(synchronizations));

        if (synchronizations.Any(synchronization => synchronization == null))
            throw new ArgumentException("Storage synchronization cannot be null.", nameof(synchronizations));

        return synchronizations.Distinct<ItemContainerSynchronization>(ReferenceEqualityComparer.Instance).ToArray();
    }

    private bool TryAcquireForNewScope(out ItemContainerSynchronization? blockedSynchronization)
    {
        blockedSynchronization = null;
        try
        {
            foreach (var synchronization in _lockOrder)
            {
                if (synchronization.TryEnterForNewScope())
                {
                    _locksAcquired++;
                    continue;
                }

                ReleaseLocks();
                blockedSynchronization = synchronization;
                return false;
            }

            return true;
        }
        catch
        {
            ReleaseLocks();
            throw;
        }
    }

    internal void ReleaseLocks()
    {
        while (_locksAcquired > 0)
        {
            var index = _locksAcquired - 1;
            Monitor.Exit(_lockOrder[index].SynchronizationLock);
            _locksAcquired = index;
        }
    }

    public void Dispose() => ReleaseLocks();
}
