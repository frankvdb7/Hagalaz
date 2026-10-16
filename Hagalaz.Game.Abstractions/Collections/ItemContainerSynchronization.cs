using System;
using System.Collections.Generic;
using System.Threading;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Owns access synchronization and logical ownership for one item storage.</summary>
internal sealed class ItemContainerSynchronization
{
    private static long _nextLockOrder;

    [ThreadStatic]
    private static int _currentThreadStorageOwnershipCount;

    private readonly object _synchronizationLock = new();
    private readonly long _lockOrder = Interlocked.Increment(ref _nextLockOrder);
    private readonly ItemContainerStorage _storage;
    private readonly ItemContainerChangeTracker _changeTracker;
    private readonly IItemContainerCompletionOwner? _completionOwner;
    private volatile ItemContainerTransaction? _transaction;
    private int _standalonePublicationOwnerThreadId;
    private int _writeScopeCount;

    internal ItemContainerSynchronization(ItemContainerStorage storage,
        IItemContainerCompletionOwner? completionOwner = null)
    {
        ArgumentNullException.ThrowIfNull(storage);
        _storage = storage;
        _changeTracker = new ItemContainerChangeTracker(this);
        _completionOwner = completionOwner;
    }

    internal ItemContainerStorage Storage => _storage;
    internal ItemContainerChangeTracker ChangeTracker => _changeTracker;
    internal object SynchronizationLock => _synchronizationLock;
    internal long LockOrder => _lockOrder;
    internal IItemContainerCompletionOwner? CompletionOwner => _completionOwner;
    internal bool IsSynchronizationLockHeldByCurrentThread => Monitor.IsEntered(_synchronizationLock);
    internal static bool CurrentThreadOwnsStorage => _currentThreadStorageOwnershipCount > 0;

    internal static void AddCurrentThreadStorageOwnership() => _currentThreadStorageOwnershipCount++;

    internal static void RemoveCurrentThreadStorageOwnership()
    {
        System.Diagnostics.Debug.Assert(_currentThreadStorageOwnershipCount > 0);
        _currentThreadStorageOwnershipCount--;
    }

    internal ItemContainerTransaction? CurrentTransaction
    {
        get
        {
            var transaction = _transaction;
            if (transaction is not { IsOwnedByCurrentThread: true }) return null;

            transaction.EnsureActive();
            return transaction;
        }
    }

    internal ItemContainerTransaction? GetSharedCurrentThreadTransaction(ItemContainerSynchronization destination)
    {
        ArgumentNullException.ThrowIfNull(destination);

        var sourceTransaction = CurrentTransaction;
        var destinationTransaction = destination.CurrentTransaction;
        if (!ReferenceEquals(sourceTransaction, destinationTransaction))
            throw new InvalidOperationException("Both storages must belong to the same active transaction.");

        return sourceTransaction;
    }

    internal void EnsureOwnedBy(ItemContainerTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        transaction.EnsureActive();
        EnsureSynchronizationLockHeld();
        if (!ReferenceEquals(_transaction, transaction))
            throw new InvalidOperationException("Storage must be owned by the active transaction.");
    }

    internal void EnsureWriteScope()
    {
        EnsureSynchronizationLockHeld();
        EnsureMutationAccess();
        if (_writeScopeCount == 0)
            throw new InvalidOperationException("Storage replacement requires an active write scope.");
    }

    internal void EnsureCurrentTransaction(ItemContainerTransaction transaction)
    {
        EnsureSynchronizationLockHeld();
        if (!ReferenceEquals(_transaction, transaction))
            throw new InvalidOperationException("Storage is not bound to the expected transaction.");
    }

    internal void BindTransaction(ItemContainerTransaction transaction)
    {
        EnsureSynchronizationLockHeld();
        if (_transaction != null || _standalonePublicationOwnerThreadId != 0)
            throw new InvalidOperationException("Storage is already owned by another scope.");
        _transaction = transaction;
    }

    internal void UnbindTransaction(ItemContainerTransaction transaction, bool pulseWaiters = false)
    {
        EnsureSynchronizationLockHeld();
        if (!ReferenceEquals(_transaction, transaction)) return;
        _transaction = null;
        if (pulseWaiters) PulseWaiters();
    }

    internal void RestoreTransactionState(ItemContainerTransaction transaction, IItem?[] items, int[] counts,
        int revision)
    {
        EnsureCurrentTransaction(transaction);
        _storage.RestoreSnapshotState(items, counts, revision);
    }

    internal bool TryEnterForNewScope()
    {
        if (!Monitor.TryEnter(_synchronizationLock)) return false;
        if (_transaction == null && _standalonePublicationOwnerThreadId == 0) return true;

        var ownedByCurrentThread = _transaction is { IsOwnedByCurrentThread: true } ||
                                   _standalonePublicationOwnerThreadId == Environment.CurrentManagedThreadId;
        Monitor.Exit(_synchronizationLock);
        if (ownedByCurrentThread)
            throw new InvalidOperationException("Storage already belongs to a scope on this thread.");

        return false;
    }

    internal void WaitForNewScope()
    {
        if (CurrentThreadOwnsStorage)
            throw new InvalidOperationException(
                "Cannot wait for another item-container storage while the current thread owns storage.");

        Monitor.Enter(_synchronizationLock);
        try
        {
            while (_transaction != null || _standalonePublicationOwnerThreadId != 0)
            {
                ThrowIfOwnedByCurrentThread();
                if (CurrentThreadOwnsStorage)
                    throw new InvalidOperationException(
                        "Cannot wait for another item-container storage while the current thread owns storage.");
                Monitor.Wait(_synchronizationLock);
            }
        }
        finally
        {
            Monitor.Exit(_synchronizationLock);
        }
    }

    internal void EnterForCurrentOwner()
    {
        var currentThreadId = Environment.CurrentManagedThreadId;
        if (_transaction is not { IsCommittedByCurrentThread: true } &&
            Volatile.Read(ref _standalonePublicationOwnerThreadId) != currentThreadId)
            throw new InvalidOperationException("Only a completed scope owner can finalize storage access.");

        Monitor.Enter(_synchronizationLock);
    }

    internal void ThrowIfOwnedByCurrentThread(string? message = null)
    {
        if (_transaction is { IsOwnedByCurrentThread: true } ||
            Volatile.Read(ref _standalonePublicationOwnerThreadId) == Environment.CurrentManagedThreadId)
            throw new InvalidOperationException(message ??
                "This operation cannot run within a transaction or publication owned by the current thread.");
    }

    internal ItemContainerWriteScope BeginWrite()
    {
        var ownsLock = !Monitor.IsEntered(_synchronizationLock);
        var ownershipCounted = false;
        var scopeCounted = false;
        if (ownsLock)
        {
            if (CurrentThreadOwnsStorage)
            {
                if (!Monitor.TryEnter(_synchronizationLock))
                    throw new InvalidOperationException(
                        "Cannot wait for another item-container storage while the current thread owns storage.");
            }
            else
            {
                Monitor.Enter(_synchronizationLock);
            }
        }

        try
        {
            if (ownsLock)
            {
                while (_transaction != null || _standalonePublicationOwnerThreadId != 0)
                {
                    ThrowIfOwnedByCurrentThread();
                    if (CurrentThreadOwnsStorage)
                        throw new InvalidOperationException(
                            "Cannot wait for another item-container storage while the current thread owns storage.");
                    Monitor.Wait(_synchronizationLock);
                }
            }
            else if (CurrentTransaction == null && _writeScopeCount == 0)
            {
                throw new InvalidOperationException(
                    "A held mutation lock must belong to an active item-container transaction or write scope.");
            }

            EnsureMutationAccess();
            _writeScopeCount++;
            scopeCounted = true;
            if (ownsLock)
            {
                AddCurrentThreadStorageOwnership();
                ownershipCounted = true;
            }

            return new ItemContainerWriteScope(this, ownsLock, _transaction, Environment.CurrentManagedThreadId);
        }
        catch
        {
            if (ownershipCounted) RemoveCurrentThreadStorageOwnership();
            if (scopeCounted) _writeScopeCount--;
            if (ownsLock) Monitor.Exit(_synchronizationLock);
            throw;
        }
    }

    internal void RecordChanges(ItemContainerTransaction? transaction, HashSet<int>? slots)
    {
        EnsureSynchronizationLockHeld();
        if (transaction is { } activeTransaction)
        {
            EnsureOwnedBy(activeTransaction);
            _changeTracker.RecordPending(slots);
        }
        else if (slots is not { Count: 0 })
        {
            EnsureMutationAccess();
            _changeTracker.RecordCommitted(slots);
        }
    }

    internal IItemContainerCompletionOwner? EndWrite(bool ownsLock, ItemContainerTransaction? transaction)
    {
        var publishStandaloneChanges = ownsLock && _writeScopeCount == 1 && transaction == null &&
                                       _completionOwner != null && _changeTracker.HasCommittedChanges;
        if (publishStandaloneChanges) ClaimStandalonePublicationOwnership();

        _writeScopeCount--;
        if (ownsLock)
        {
            Monitor.Exit(_synchronizationLock);
            RemoveCurrentThreadStorageOwnership();
        }

        return publishStandaloneChanges ? _completionOwner : null;
    }

    internal void CompleteStandalonePublication()
    {
        EnterForCurrentOwner();
        try
        {
            ReleaseStandalonePublicationOwnership();
            _changeTracker.DiscardCommitted();
        }
        finally
        {
            Monitor.Exit(_synchronizationLock);
        }
    }

    internal void ClaimStandalonePublicationOwnership()
    {
        EnsureSynchronizationLockHeld();
        if (_transaction != null)
            throw new InvalidOperationException("Transaction-owned storage cannot claim standalone publication ownership.");
        if (_standalonePublicationOwnerThreadId != 0)
            throw new InvalidOperationException("Storage already has a standalone publication owner.");
        _standalonePublicationOwnerThreadId = Environment.CurrentManagedThreadId;
        AddCurrentThreadStorageOwnership();
    }

    internal void ReleaseStandalonePublicationOwnership()
    {
        EnsureSynchronizationLockHeld();
        if (_standalonePublicationOwnerThreadId != Environment.CurrentManagedThreadId)
            throw new InvalidOperationException("Only the standalone publication owner can release storage ownership.");
        _standalonePublicationOwnerThreadId = 0;
        RemoveCurrentThreadStorageOwnership();
        PulseWaiters();
    }

    private void PulseWaiters()
    {
        EnsureSynchronizationLockHeld();
        Monitor.PulseAll(_synchronizationLock);
    }

    internal void EnsureStandalonePublicationOwnedByCurrentThread()
    {
        EnsureSynchronizationLockHeld();
        if (_transaction != null || _standalonePublicationOwnerThreadId != Environment.CurrentManagedThreadId)
            throw new InvalidOperationException("Standalone completion requires retained publication ownership.");
    }

    internal bool IsStandalonePublicationOwnedByCurrentThread
    {
        get
        {
            EnsureSynchronizationLockHeld();
            return _standalonePublicationOwnerThreadId == Environment.CurrentManagedThreadId;
        }
    }

    internal bool HasTrackedChanges
    {
        get
        {
            EnterChangeTrackerRead(consume: false);
            try { return _changeTracker.HasCommittedChanges; }
            finally { Monitor.Exit(_synchronizationLock); }
        }
    }

    internal int MutationRevision
    {
        get
        {
            EnterChangeTrackerRead(consume: false);
            try { return _storage.MutationRevision; }
            finally { Monitor.Exit(_synchronizationLock); }
        }
    }

    internal HashSet<int>? ConsumeTrackedChanges()
    {
        EnterChangeTrackerRead(consume: true);
        try
        {
            _changeTracker.TryConsumeCommitted(out var slots);
            return slots;
        }
        finally { Monitor.Exit(_synchronizationLock); }
    }

    private void EnterChangeTrackerRead(bool consume)
    {
        var lockAlreadyHeld = Monitor.IsEntered(_synchronizationLock);
        if (_transaction is { IsCommittedByCurrentThread: true } ||
            Volatile.Read(ref _standalonePublicationOwnerThreadId) == Environment.CurrentManagedThreadId)
        {
            // The logical owner may consume changes while a foreign contender briefly owns the monitor to wait.
            Monitor.Enter(_synchronizationLock);
        }
        else if (CurrentThreadOwnsStorage)
        {
            if (!Monitor.TryEnter(_synchronizationLock))
                throw new InvalidOperationException(
                    "Cannot wait for another item-container storage while the current thread owns storage.");
        }
        else
        {
            Monitor.Enter(_synchronizationLock);
        }

        try
        {
            if (lockAlreadyHeld && _writeScopeCount != 0)
                throw new InvalidOperationException("Committed changes cannot be read during an active write scope.");

            while (_transaction != null || _standalonePublicationOwnerThreadId != 0)
            {
                if (_transaction is { IsCommittedByCurrentThread: true } ||
                    IsStandalonePublicationOwnedByCurrentThread ||
                    (!consume && _transaction is { IsOwnedByCurrentThread: true }))
                    return;

                ThrowIfOwnedByCurrentThread(
                    "Committed changes cannot be read while the current thread owns a transaction or publication scope.");
                if (CurrentThreadOwnsStorage)
                    throw new InvalidOperationException(
                        "Cannot wait for another item-container storage while the current thread owns storage.");
                Monitor.Wait(_synchronizationLock);
            }
        }
        catch
        {
            Monitor.Exit(_synchronizationLock);
            throw;
        }
    }

    private void EnsureMutationAccess()
    {
        EnsureSynchronizationLockHeld();
        if (_standalonePublicationOwnerThreadId != 0)
            throw new InvalidOperationException("Storage is completing standalone publication.");
        _transaction?.EnsureActive();
    }

    private void EnsureSynchronizationLockHeld()
    {
        if (!Monitor.IsEntered(_synchronizationLock))
            throw new InvalidOperationException("Storage access must be authorized while holding its mutation lock.");
    }
}
