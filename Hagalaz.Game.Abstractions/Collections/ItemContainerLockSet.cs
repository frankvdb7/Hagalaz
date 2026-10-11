using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Acquires multiple item-container boundaries in their shared deadlock-prevention order.</summary>
internal sealed class ItemContainerLockSet : IDisposable
{
    private readonly ItemContainerMutationBoundary[] _lockOrder;
    private readonly ItemContainerTransaction? _allowedTransaction;
    private readonly bool _allowCurrentPublicationOwner;
    private int _locksAcquired;
    private bool _ownsOwnershipCount;

    private ItemContainerLockSet(ItemContainerMutationBoundary[] boundaries,
        ItemContainerTransaction? allowedTransaction, bool allowCurrentPublicationOwner)
    {
        _lockOrder = boundaries.OrderBy(boundary => boundary.LockOrder).ToArray();
        _allowedTransaction = allowedTransaction;
        _allowCurrentPublicationOwner = allowCurrentPublicationOwner;
    }

    internal static ItemContainerLockSet Acquire(IReadOnlyCollection<ItemContainerMutationBoundary> boundaries)
        => AcquireCore(boundaries, null, allowCurrentPublicationOwner: false, returnFalseOnNestedContention: false)
            ?? throw new InvalidOperationException("The item-container lock set could not be acquired.");

    internal static bool TryAcquireStandaloneTransfer(
        IReadOnlyCollection<ItemContainerMutationBoundary> boundaries,
        out ItemContainerLockSet? lockSet)
    {
        lockSet = AcquireCore(boundaries, null, allowCurrentPublicationOwner: false,
            returnFalseOnNestedContention: true);
        return lockSet != null;
    }

    internal static ItemContainerLockSet AcquireForTransactionFinalization(
        IReadOnlyCollection<ItemContainerMutationBoundary> boundaries,
        ItemContainerTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        return AcquireCore(boundaries, transaction, allowCurrentPublicationOwner: false,
            returnFalseOnNestedContention: false, finalization: true)!;
    }

    internal static ItemContainerLockSet AcquireForStandalonePublicationFinalization(
        IReadOnlyCollection<ItemContainerMutationBoundary> boundaries)
        => AcquireCore(boundaries, null, allowCurrentPublicationOwner: true,
            returnFalseOnNestedContention: false, finalization: true)!;

    private static ItemContainerLockSet? AcquireCore(
        IReadOnlyCollection<ItemContainerMutationBoundary> boundaries,
        ItemContainerTransaction? allowedTransaction,
        bool allowCurrentPublicationOwner,
        bool returnFalseOnNestedContention,
        bool finalization = false)
    {
        ArgumentNullException.ThrowIfNull(boundaries);
        if (boundaries.Count == 0) throw new ArgumentException("At least one boundary is required.", nameof(boundaries));

        var lockSet = new ItemContainerLockSet(boundaries.ToArray(), allowedTransaction, allowCurrentPublicationOwner);
        var nested = ItemContainerMutationBoundary.CurrentThreadOwnsStorage && !finalization;
        try
        {
            while (true)
            {
                ItemContainerMutationBoundary? busyBoundary = null;
                foreach (var boundary in lockSet._lockOrder)
                {
                    if (finalization || !nested)
                    {
                        Monitor.Enter(boundary.MutationLock);
                    }
                    else if (!Monitor.TryEnter(boundary.MutationLock))
                    {
                        if (returnFalseOnNestedContention)
                        {
                            lockSet.ReleasePhysicalLocks();
                            return null;
                        }

                        throw new InvalidOperationException(
                            "Cannot wait for another item-container boundary while the current thread owns storage.");
                    }

                    lockSet._locksAcquired++;
                    if (!lockSet.HasConflictingOwner(boundary)) continue;

                    if (boundary.Transaction is { IsOwnedByCurrentThread: true } ||
                        (boundary.HasStandalonePublicationOwner && boundary.IsStandalonePublicationOwnedByCurrentThread))
                        throw new InvalidOperationException("Storage already belongs to a scope on this thread.");

                    if (finalization)
                        throw new InvalidOperationException("A scope boundary changed owner during finalization.");

                    if (nested)
                    {
                        lockSet.ReleasePhysicalLocks();
                        if (returnFalseOnNestedContention) return null;
                        throw new InvalidOperationException(
                            "Cannot wait for another item-container boundary while the current thread owns storage.");
                    }

                    busyBoundary = boundary;
                    lockSet.ReleasePhysicalLocks();
                    break;
                }

                if (busyBoundary == null)
                {
                    ItemContainerMutationBoundary.AddCurrentThreadStorageOwnership();
                    lockSet._ownsOwnershipCount = true;
                    return lockSet;
                }

                lock (busyBoundary.MutationLock)
                {
                    while (lockSet.HasConflictingOwner(busyBoundary))
                    {
                        if (busyBoundary.Transaction is { IsOwnedByCurrentThread: true } ||
                            (busyBoundary.HasStandalonePublicationOwner && busyBoundary.IsStandalonePublicationOwnedByCurrentThread))
                            throw new InvalidOperationException("Storage already belongs to a scope on this thread.");
                        Monitor.Wait(busyBoundary.MutationLock);
                    }
                }
            }
        }
        catch
        {
            lockSet.ReleasePhysicalLocks();
            lockSet.ReleaseOwnershipCount();
            throw;
        }
    }

    private bool HasConflictingOwner(ItemContainerMutationBoundary boundary)
    {
        var transaction = boundary.Transaction;
        if (transaction != null && !ReferenceEquals(transaction, _allowedTransaction)) return true;
        if (!boundary.HasStandalonePublicationOwner) return false;
        return !_allowCurrentPublicationOwner || !boundary.IsStandalonePublicationOwnedByCurrentThread;
    }

    /// <summary>Releases the monitors while keeping ownership accounting for a transaction's completion phase.</summary>
    internal void ReleaseLocksPreservingOwnership()
    {
        ReleasePhysicalLocks();
    }

    /// <summary>Releases the ownership count after the retained transaction binding has been cleared.</summary>
    internal void ReleaseOwnership()
    {
        ReleaseOwnershipCount();
    }

    private void ReleasePhysicalLocks()
    {
        while (_locksAcquired > 0)
        {
            var lockIndex = _locksAcquired - 1;
            Monitor.Exit(_lockOrder[lockIndex].MutationLock);
            _locksAcquired = lockIndex;
        }
    }

    private void ReleaseOwnershipCount()
    {
        if (!_ownsOwnershipCount) return;
        ItemContainerMutationBoundary.RemoveCurrentThreadStorageOwnership();
        _ownsOwnershipCount = false;
    }

    public void Dispose()
    {
        ReleasePhysicalLocks();
        ReleaseOwnershipCount();
    }
}
