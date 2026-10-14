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

    private readonly object _mutationLock = new();
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
    internal object MutationLock => _mutationLock;
    internal long LockOrder => _lockOrder;
    internal IItemContainerCompletionOwner? CompletionOwner => _completionOwner;
    internal ItemContainerTransaction? Transaction { get => _transaction; }
    internal bool IsMutationLockHeldByCurrentThread => Monitor.IsEntered(_mutationLock);
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
        EnsureMutationLockHeld();
        if (!ReferenceEquals(_transaction, transaction))
            throw new InvalidOperationException("Storage must be owned by the active transaction.");
    }

    internal void EnsureWriteScope()
    {
        EnsureMutationLockHeld();
        EnsureMutationAccess();
        if (_writeScopeCount == 0)
            throw new InvalidOperationException("Storage replacement requires an active write scope.");
    }

    internal void EnsureCurrentTransaction(ItemContainerTransaction transaction)
    {
        EnsureMutationLockHeld();
        if (!ReferenceEquals(_transaction, transaction))
            throw new InvalidOperationException("Storage is not bound to the expected transaction.");
    }

    internal void BindTransaction(ItemContainerTransaction transaction)
    {
        EnsureMutationLockHeld();
        if (_transaction != null || _standalonePublicationOwnerThreadId != 0)
            throw new InvalidOperationException("Storage is already owned by another scope.");
        _transaction = transaction;
    }

    internal void UnbindTransaction(ItemContainerTransaction transaction, bool pulseWaiters = false)
    {
        EnsureMutationLockHeld();
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
        if (!Monitor.TryEnter(_mutationLock)) return false;
        if (_transaction == null && _standalonePublicationOwnerThreadId == 0) return true;

        var ownedByCurrentThread = _transaction is { IsOwnedByCurrentThread: true } ||
                                   _standalonePublicationOwnerThreadId == Environment.CurrentManagedThreadId;
        Monitor.Exit(_mutationLock);
        if (ownedByCurrentThread)
            throw new InvalidOperationException("Storage already belongs to a scope on this thread.");

        return false;
    }

    internal void WaitForNewScope()
    {
        if (CurrentThreadOwnsStorage)
            throw new InvalidOperationException(
                "Cannot wait for another item-container storage while the current thread owns storage.");

        Monitor.Enter(_mutationLock);
        try
        {
            while (_transaction != null || _standalonePublicationOwnerThreadId != 0)
            {
                ThrowIfOwnedByCurrentThread();
                if (CurrentThreadOwnsStorage)
                    throw new InvalidOperationException(
                        "Cannot wait for another item-container storage while the current thread owns storage.");
                Monitor.Wait(_mutationLock);
            }
        }
        finally
        {
            Monitor.Exit(_mutationLock);
        }
    }

    internal void EnterForCurrentOwner()
    {
        var currentThreadId = Environment.CurrentManagedThreadId;
        if (_transaction is not { IsCommittedByCurrentThread: true } &&
            Volatile.Read(ref _standalonePublicationOwnerThreadId) != currentThreadId)
            throw new InvalidOperationException("Only a completed scope owner can finalize storage access.");

        Monitor.Enter(_mutationLock);
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
        var ownsLock = !Monitor.IsEntered(_mutationLock);
        var ownershipCounted = false;
        var scopeCounted = false;
        if (ownsLock)
        {
            if (CurrentThreadOwnsStorage)
            {
                if (!Monitor.TryEnter(_mutationLock))
                    throw new InvalidOperationException(
                        "Cannot wait for another item-container storage while the current thread owns storage.");
            }
            else
            {
                Monitor.Enter(_mutationLock);
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
                    Monitor.Wait(_mutationLock);
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

            return new ItemContainerWriteScope(this, ownsLock, _transaction, ownershipCounted,
                scopeCounted, Environment.CurrentManagedThreadId);
        }
        catch
        {
            if (ownershipCounted) RemoveCurrentThreadStorageOwnership();
            if (scopeCounted) _writeScopeCount--;
            if (ownsLock) Monitor.Exit(_mutationLock);
            throw;
        }
    }

    internal void RecordChanges(ItemContainerTransaction? transaction, HashSet<int>? slots)
    {
        EnsureMutationLockHeld();
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

    internal IItemContainerCompletionOwner? EndWrite(bool ownsLock, bool scopeCounted, bool ownershipCounted,
        ItemContainerTransaction? transaction)
    {
        var publishStandaloneChanges = ownsLock && scopeCounted && _writeScopeCount == 1 && transaction == null &&
                                       _completionOwner != null && _changeTracker.HasCommittedChanges;
        if (publishStandaloneChanges) ClaimStandalonePublicationOwnership();

        if (scopeCounted) _writeScopeCount--;
        if (ownsLock)
        {
            Monitor.Exit(_mutationLock);
            if (ownershipCounted) RemoveCurrentThreadStorageOwnership();
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
            Monitor.Exit(_mutationLock);
        }
    }

    internal void ClaimStandalonePublicationOwnership()
    {
        EnsureMutationLockHeld();
        if (_transaction != null)
            throw new InvalidOperationException("Transaction-owned storage cannot claim standalone publication ownership.");
        if (_standalonePublicationOwnerThreadId != 0)
            throw new InvalidOperationException("Storage already has a standalone publication owner.");
        _standalonePublicationOwnerThreadId = Environment.CurrentManagedThreadId;
        AddCurrentThreadStorageOwnership();
    }

    internal void ReleaseStandalonePublicationOwnership()
    {
        EnsureMutationLockHeld();
        if (_standalonePublicationOwnerThreadId != Environment.CurrentManagedThreadId)
            throw new InvalidOperationException("Only the standalone publication owner can release storage ownership.");
        _standalonePublicationOwnerThreadId = 0;
        RemoveCurrentThreadStorageOwnership();
        PulseWaiters();
    }

    private void PulseWaiters()
    {
        EnsureMutationLockHeld();
        Monitor.PulseAll(_mutationLock);
    }

    internal void EnsureStandalonePublicationOwnedByCurrentThread()
    {
        EnsureMutationLockHeld();
        if (_transaction != null || _standalonePublicationOwnerThreadId != Environment.CurrentManagedThreadId)
            throw new InvalidOperationException("Standalone completion requires retained publication ownership.");
    }

    internal bool HasStandalonePublicationOwner
    {
        get { EnsureMutationLockHeld(); return _standalonePublicationOwnerThreadId != 0; }
    }

    internal bool IsStandalonePublicationOwnedByCurrentThread
    {
        get
        {
            EnsureMutationLockHeld();
            return _standalonePublicationOwnerThreadId == Environment.CurrentManagedThreadId;
        }
    }

    internal bool HasTrackedChanges
    {
        get
        {
            EnterChangeTrackerRead(consume: false);
            try { return _changeTracker.HasCommittedChanges; }
            finally { Monitor.Exit(_mutationLock); }
        }
    }

    internal int MutationRevision
    {
        get
        {
            EnterChangeTrackerRead(consume: false);
            try { return _storage.MutationRevision; }
            finally { Monitor.Exit(_mutationLock); }
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
        finally { Monitor.Exit(_mutationLock); }
    }

    private void EnterChangeTrackerRead(bool consume)
    {
        var lockAlreadyHeld = Monitor.IsEntered(_mutationLock);
        if (_transaction is { IsCommittedByCurrentThread: true } ||
            Volatile.Read(ref _standalonePublicationOwnerThreadId) == Environment.CurrentManagedThreadId)
        {
            // The logical owner may consume changes while a foreign contender briefly owns the monitor to wait.
            Monitor.Enter(_mutationLock);
        }
        else if (CurrentThreadOwnsStorage)
        {
            if (!Monitor.TryEnter(_mutationLock))
                throw new InvalidOperationException(
                    "Cannot wait for another item-container storage while the current thread owns storage.");
        }
        else
        {
            Monitor.Enter(_mutationLock);
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
                Monitor.Wait(_mutationLock);
            }
        }
        catch
        {
            Monitor.Exit(_mutationLock);
            throw;
        }
    }

    private void EnsureMutationAccess()
    {
        EnsureMutationLockHeld();
        if (_standalonePublicationOwnerThreadId != 0)
            throw new InvalidOperationException("Storage is completing standalone publication.");
        _transaction?.EnsureActive();
    }

    private void EnsureMutationLockHeld()
    {
        if (!Monitor.IsEntered(_mutationLock))
            throw new InvalidOperationException("Storage access must be authorized while holding its mutation lock.");
    }
}
