using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Coordinates mutations and publication for one owned item storage.</summary>
internal sealed class ItemContainerMutationBoundary
{
    private static long _nextLockOrder;
    [ThreadStatic]
    private static int _currentThreadStorageOwnershipCount;
    private readonly object _mutationLock = new();
    private readonly long _lockOrder = Interlocked.Increment(ref _nextLockOrder);
    private readonly ItemContainerStorage _storage;
    private readonly ItemContainerChangeTracker _changeTracker;
    private readonly IItemContainerCompletionOwner? _completion;
    private readonly Action<HashSet<int>?>? _publishChanges;
    private volatile ItemContainerTransaction? _transaction;
    private int _standalonePublicationOwnerThreadId;
    private int _mutationScopeCount;

    private ItemContainerTransaction EnsureActiveTransaction()
    {
        EnsureMutationLockHeld();
        var transaction = _transaction
            ?? throw new InvalidOperationException("Mutation boundary must belong to an active transaction.");
        transaction.EnsureActive();
        return transaction;
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

    internal ItemContainerTransaction? GetSharedCurrentThreadTransaction(ItemContainerMutationBoundary destination)
    {
        ArgumentNullException.ThrowIfNull(destination);

        var sourceTransaction = CurrentTransaction;
        var destinationTransaction = destination.CurrentTransaction;
        if (!ReferenceEquals(sourceTransaction, destinationTransaction))
            throw new InvalidOperationException("Both boundaries must belong to the same active transaction.");

        return sourceTransaction;
    }

    internal ItemContainerStorage Storage => _storage;
    internal ItemContainerChangeTracker ChangeTracker => _changeTracker;
    internal object MutationLock => _mutationLock;
    internal long LockOrder => _lockOrder;
    internal IItemContainerCompletionOwner? CompletionOwner => _completion;
    internal ItemContainerTransaction? Transaction { get => _transaction; set => _transaction = value; }
    internal bool IsMutationLockHeldByCurrentThread => Monitor.IsEntered(_mutationLock);

    internal static bool CurrentThreadOwnsStorage => _currentThreadStorageOwnershipCount > 0;

    internal static void AddCurrentThreadStorageOwnership() => _currentThreadStorageOwnershipCount++;

    internal static void RemoveCurrentThreadStorageOwnership()
    {
        System.Diagnostics.Debug.Assert(_currentThreadStorageOwnershipCount > 0);
        _currentThreadStorageOwnershipCount--;
    }

    internal void EnsureOwnedBy(ItemContainerTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        transaction.EnsureActive();
        EnsureMutationLockHeld();
        if (!ReferenceEquals(_transaction, transaction))
            throw new InvalidOperationException("Mutation boundary must be owned by the active transaction.");
    }

    internal void EnsureMutationScope()
    {
        EnsureMutationLockHeld();
        EnsureMutationAccess();
        if (_mutationScopeCount == 0)
            throw new InvalidOperationException("Storage replacement requires an active mutation scope.");
    }

    private void EnsureMutationLockHeld()
    {
        if (!Monitor.IsEntered(_mutationLock))
            throw new InvalidOperationException("Mutation boundary ownership must be inspected while holding its mutation lock.");
    }

    private void EnsureMutationAccess()
    {
        EnsureMutationLockHeld();
        if (_standalonePublicationOwnerThreadId != 0)
            throw new InvalidOperationException("Storage is completing standalone publication.");
        _transaction?.EnsureActive();
    }

    internal bool HasStandalonePublicationOwner
    {
        get { EnsureMutationLockHeld(); return _standalonePublicationOwnerThreadId != 0; }
    }

    internal bool IsStandalonePublicationOwnedByCurrentThread
    {
        get { EnsureMutationLockHeld(); return _standalonePublicationOwnerThreadId == Environment.CurrentManagedThreadId; }
    }

    private void ClaimStandalonePublicationOwnership()
    {
        EnsureMutationLockHeld();
        if (_transaction != null)
            throw new InvalidOperationException("Transaction-owned storage cannot claim standalone publication ownership.");
        if (_standalonePublicationOwnerThreadId != 0)
            throw new InvalidOperationException("Storage already has a standalone publication owner.");
        _standalonePublicationOwnerThreadId = Environment.CurrentManagedThreadId;
        AddCurrentThreadStorageOwnership();
    }

    private void ReleaseStandalonePublicationOwnership()
    {
        EnsureMutationLockHeld();
        if (_standalonePublicationOwnerThreadId != Environment.CurrentManagedThreadId)
            throw new InvalidOperationException("Only the standalone publication owner can release storage ownership.");
        _standalonePublicationOwnerThreadId = 0;
        RemoveCurrentThreadStorageOwnership();
    }

    internal void RestoreTransactionState(ItemContainerTransaction transaction, IItem?[] items, int[] counts, int revision)
    {
        EnsureMutationLockHeld();
        if (!ReferenceEquals(_transaction, transaction))
            throw new InvalidOperationException("Rollback requires the owning transaction boundary.");
        _storage.RestoreSnapshotState(items, counts, revision);
    }

    internal ItemContainerMutationBoundary(ItemContainerStorage storage, Action<HashSet<int>?>? publishChanges, IItemContainerCompletionOwner? completion = null)
    {
        ArgumentNullException.ThrowIfNull(storage);
        _storage = storage;
        _changeTracker = new ItemContainerChangeTracker(this);
        _publishChanges = publishChanges;
        _completion = completion;
    }

    internal bool HasTrackedChanges
    {
        get
        {
            EnterChangeTrackerRead();
            try { return _changeTracker.HasCommittedChanges; }
            finally { Monitor.Exit(_mutationLock); }
        }
    }

    internal HashSet<int>? ConsumeTrackedChanges()
    {
        EnterChangeTrackerRead();
        try
        {
            _changeTracker.TryConsumeCommitted(out var slots);
            return slots;
        }
        finally { Monitor.Exit(_mutationLock); }
    }

    private void EnterChangeTrackerRead()
    {
        var lockAlreadyHeld = Monitor.IsEntered(_mutationLock);
        if (CurrentThreadOwnsStorage)
        {
            if (!Monitor.TryEnter(_mutationLock))
                throw new InvalidOperationException("Cannot wait for another item-container boundary while the current thread owns storage.");
        }
        else
        {
            Monitor.Enter(_mutationLock);
        }

        try
        {
            if (lockAlreadyHeld && _mutationScopeCount != 0)
                throw new InvalidOperationException("Committed changes cannot be read during an active mutation scope.");

            while (_transaction != null || _standalonePublicationOwnerThreadId != 0)
            {
                ThrowIfOwnedByCurrentThread("Committed changes cannot be read while the current thread owns a transaction or publication scope.");
                if (CurrentThreadOwnsStorage)
                    throw new InvalidOperationException("Cannot wait for another item-container boundary while the current thread owns storage.");
                Monitor.Wait(_mutationLock);
            }
        }
        catch
        {
            Monitor.Exit(_mutationLock);
            throw;
        }
    }

    internal MutationScope BeginMutation() => BeginMutation(publishStandaloneOnDispose: true);

    // Equipment owns standalone lifecycle effects and must run them before publication.
    internal MutationScope BeginMutationWithExplicitStandalonePublication() => BeginMutation(publishStandaloneOnDispose: false);

    internal void ThrowIfOwnedByCurrentThread(string? message = null)
    {
        if (_transaction is { IsOwnedByCurrentThread: true } ||
            Volatile.Read(ref _standalonePublicationOwnerThreadId) == Environment.CurrentManagedThreadId)
            throw new InvalidOperationException(message ?? "This operation cannot run within a transaction or publication owned by the current thread.");
    }

    private MutationScope BeginMutation(bool publishStandaloneOnDispose)
    {
        var ownsLock = !Monitor.IsEntered(_mutationLock);
        var ownershipCounted = false;
        var scopeCounted = false;
        if (ownsLock)
        {
            if (CurrentThreadOwnsStorage)
            {
                if (!Monitor.TryEnter(_mutationLock))
                    throw new InvalidOperationException("Cannot wait for another item-container boundary while the current thread owns storage.");
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
                        throw new InvalidOperationException("Cannot wait for another item-container boundary while the current thread owns storage.");
                    Monitor.Wait(_mutationLock);
                }
            }
            else if (CurrentTransaction == null)
            {
                throw new InvalidOperationException("A held mutation lock must belong to an active item-container transaction.");
            }

            EnsureMutationAccess();
            _mutationScopeCount++;
            scopeCounted = true;
            if (ownsLock)
            {
                AddCurrentThreadStorageOwnership();
                ownershipCounted = true;
            }

            return new MutationScope(this, ownsLock, _transaction, publishStandaloneOnDispose, ownershipCounted, scopeCounted);
        }
        catch
        {
            if (ownershipCounted) RemoveCurrentThreadStorageOwnership();
            if (scopeCounted) _mutationScopeCount--;
            if (ownsLock) Monitor.Exit(_mutationLock);
            throw;
        }
    }

    internal ref struct MutationScope
    {
        private readonly ItemContainerMutationBoundary _boundary;
        private readonly ItemContainerTransaction? _transaction;
        private readonly bool _ownsLock;
        private readonly bool _publishStandaloneOnDispose;
        private bool _ownershipCounted;
        private bool _scopeCounted;
        private bool _recorded;
        private bool _standalonePublicationClaimed;
        private bool _disposed;

        internal ItemContainerTransaction? Transaction => _transaction;

        internal MutationScope(ItemContainerMutationBoundary boundary, bool ownsLock,
            ItemContainerTransaction? transaction, bool publishStandaloneOnDispose, bool ownershipCounted,
            bool scopeCounted)
        {
            _boundary = boundary;
            _ownsLock = ownsLock;
            _transaction = transaction;
            _publishStandaloneOnDispose = publishStandaloneOnDispose;
            _ownershipCounted = ownershipCounted;
            _scopeCounted = scopeCounted;
            _recorded = false;
            _standalonePublicationClaimed = false;
            _disposed = false;
        }

        /// <summary>
        /// Records changed slots for publication. A <see langword="null"/> set means the whole container changed.
        /// </summary>
        internal void RecordChanges(HashSet<int>? slots)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(MutationScope));
            if (_recorded) throw new InvalidOperationException("Mutation changes can only be recorded once per operation.");
            if (!Monitor.IsEntered(_boundary._mutationLock))
                throw new InvalidOperationException("Mutation changes must be recorded while holding the storage mutation lock.");

            if (_transaction is { } transaction)
            {
                _boundary.RecordTransactionChanges(transaction, slots);
            }
            else
            {
                if (slots is not { Count: 0 })
                {
                    _boundary.ClaimStandalonePublicationOwnership();
                    _standalonePublicationClaimed = true;
                    _boundary._changeTracker.RecordCommitted(slots);
                }
            }
            _recorded = true;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_scopeCounted)
            {
                _boundary._mutationScopeCount--;
                _scopeCounted = false;
            }

            if (_ownsLock)
            {
                Monitor.Exit(_boundary._mutationLock);
                if (_ownershipCounted)
                {
                    RemoveCurrentThreadStorageOwnership();
                    _ownershipCounted = false;
                }
            }

            if (_ownsLock && _transaction == null && _standalonePublicationClaimed && _publishStandaloneOnDispose)
                _boundary.PublishCommittedChanges();
        }
    }

    internal void RecordTransactionChanges(ItemContainerTransaction transaction, HashSet<int>? slots)
    {
        EnsureOwnedBy(transaction);
        _changeTracker.RecordPending(slots);
    }

    /// <summary>Transfers an exact quantity between storage boundaries already enlisted in one caller-owned transaction.</summary>
    internal bool TryTransferTo(
        ItemContainerMutationBoundary destination,
        IItem item,
        int count,
        int preferredSourceSlot = -1,
        int destinationSlot = -1,
        IItem? destinationItem = null)
    {
        ArgumentNullException.ThrowIfNull(destination);
        var transaction = EnsureActiveTransaction();
        if (!ReferenceEquals(transaction, destination.EnsureActiveTransaction()))
            throw new InvalidOperationException("Both storage boundaries must belong to the same active transaction.");
        if (!_storage.TryTransferTo(destination._storage, item, count,
                preferredSourceSlot, destinationSlot, destinationItem, out var sourceSlots, out var destinationSlots)) return false;
        RecordTransactionChanges(transaction, sourceSlots);
        destination.RecordTransactionChanges(transaction, destinationSlots);
        return true;
    }

    /// <summary>Transfers between unbound boundaries and retains both publication claims until completion.</summary>
    internal bool TryTransferStandaloneTo(
        ItemContainerMutationBoundary destination,
        IItem item,
        int count,
        int preferredSourceSlot = -1,
        int destinationSlot = -1,
        IItem? destinationItem = null,
        Func<bool>? validateWhileLocked = null,
        Action? beforePublication = null)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(item);

        if (ReferenceEquals(this, destination) || count <= 0 || destinationSlot < -1)
            return false;

        ThrowIfOwnedByCurrentThread();
        destination.ThrowIfOwnedByCurrentThread();
        if (IsMutationLockHeldByCurrentThread || destination.IsMutationLockHeldByCurrentThread)
            throw new InvalidOperationException("A standalone transfer cannot borrow a mutation lock.");
        if (!ItemContainerLockSet.TryAcquireStandaloneTransfer([this, destination], out var lockSet))
            return false;

        HashSet<int> sourceSlots;
        HashSet<int> destinationSlots;
        using (lockSet!)
        {
            if (validateWhileLocked != null && !validateWhileLocked())
                return false;

            if (!_storage.TryTransferTo(destination._storage, item, count,
                    preferredSourceSlot, destinationSlot, destinationItem, out sourceSlots, out destinationSlots))
                return false;

            ClaimStandalonePublicationOwnership();
            try
            {
                destination.ClaimStandalonePublicationOwnership();
            }
            catch
            {
                ReleaseStandalonePublicationOwnership();
                throw;
            }

            _changeTracker.RecordCommitted(sourceSlots);
            destination._changeTracker.RecordCommitted(destinationSlots);
        }

        List<Exception>? failures = null;
        try
        {
            if (beforePublication != null)
            {
                try { beforePublication(); }
                catch (Exception exception) { (failures ??= []).Add(exception); }
            }

            try
            {
                PublishStandaloneChangesRetainingOwnership();
            }
            catch (Exception exception)
            {
                (failures ??= []).Add(exception);
            }

            try
            {
                destination.PublishStandaloneChangesRetainingOwnership();
            }
            catch (Exception exception)
            {
                (failures ??= []).Add(exception);
            }
        }
        finally
        {
            using (ItemContainerLockSet.AcquireForStandalonePublicationFinalization([this, destination]))
            {
                ReleaseStandalonePublicationOwnership();
                destination.ReleaseStandalonePublicationOwnership();
                Monitor.PulseAll(_mutationLock);
                Monitor.PulseAll(destination._mutationLock);
            }
        }

        if (failures is { Count: 1 })
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures is { Count: > 1 })
            throw new AggregateException(failures).Flatten();

        return true;
    }

    private void PublishStandaloneChangesRetainingOwnership()
    {
        HashSet<int>? slots = null;
        var hasChanges = false;
        lock (_mutationLock)
        {
            if (_transaction != null || !IsStandalonePublicationOwnedByCurrentThread)
                throw new InvalidOperationException("Standalone publication requires retained boundary ownership.");
            if (_publishChanges != null)
                hasChanges = _changeTracker.TryConsumeCommitted(out slots);
        }

        if (hasChanges) _publishChanges!(slots);
    }

    internal void PublishCommittedChanges()
    {
        bool standalonePublication;
        bool hasChanges;
        HashSet<int>? slots = null;
        lock (_mutationLock)
        {
            if (_transaction != null)
            {
                if (HasStandalonePublicationOwner)
                    throw new InvalidOperationException("Storage cannot be transaction-bound and standalone-publishing at once.");
                standalonePublication = false;
            }
            else if (IsStandalonePublicationOwnedByCurrentThread)
            {
                standalonePublication = true;
            }
            else
            {
                throw new InvalidOperationException("Committed publication requires storage ownership.");
            }

            hasChanges = _publishChanges != null && _changeTracker.TryConsumeCommitted(out slots);
        }

        if (!standalonePublication)
        {
            if (hasChanges) _publishChanges!(slots);
            return;
        }

        try
        {
            if (hasChanges) _publishChanges!(slots);
        }
        finally
        {
            lock (_mutationLock)
            {
                ReleaseStandalonePublicationOwnership();
                Monitor.PulseAll(_mutationLock);
            }
        }
    }

    /// <summary>Runs standalone domain effects after unlock, then publishes and releases the retained ownership.</summary>
    internal void CompleteStandaloneChanges(Action beforePublication)
    {
        ArgumentNullException.ThrowIfNull(beforePublication);
        lock (_mutationLock)
        {
            if (_transaction != null || !IsStandalonePublicationOwnedByCurrentThread)
                throw new InvalidOperationException("Standalone completion requires retained boundary ownership.");
        }

        List<Exception>? failures = null;
        try { beforePublication(); }
        catch (Exception exception) { (failures ??= []).Add(exception); }

        try { PublishCommittedChanges(); }
        catch (Exception exception) { (failures ??= []).Add(exception); }

        if (failures is { Count: 1 }) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures is { Count: > 1 }) throw new AggregateException(failures).Flatten();
    }

}
