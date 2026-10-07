using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Coordinates mutations and publication for one owned item storage.</summary>
internal sealed class ItemContainerMutationBoundary
{
    private static long _nextMutationOrder;
    private readonly object _mutationLock = new();
    private readonly long _mutationOrder = Interlocked.Increment(ref _nextMutationOrder);
    private readonly ItemContainerStorage _storage;
    private readonly IItemContainerCompletionOwner? _completion;
    private readonly Action<HashSet<int>?>? _publishChanges;
    private volatile ItemContainerTransaction? _transaction;
    private int _standalonePublicationOwnerThreadId;

    private ItemContainerTransaction EnsureActiveTransaction()
    {
        EnsureMutationLockHeld();
        var transaction = _transaction
            ?? throw new InvalidOperationException("Mutation boundary must belong to an active transaction.");
        transaction.EnsureActive();
        return transaction;
    }

    internal ItemContainerTransaction? GetCurrentThreadTransaction() =>
        IsMutationLockHeldByCurrentThread ? EnsureActiveTransaction() : null;

    internal ItemContainerStorage Storage => _storage;
    internal object MutationLock => _mutationLock;
    internal long MutationOrder => _mutationOrder;
    internal IItemContainerCompletionOwner? CompletionOwner => _completion;
    internal ItemContainerTransaction? Transaction { get => _transaction; set => _transaction = value; }
    internal bool IsMutationLockHeldByCurrentThread => Monitor.IsEntered(_mutationLock);

    internal void EnsureOwnedBy(ItemContainerTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        transaction.EnsureActive();
        EnsureMutationLockHeld();
        if (!ReferenceEquals(_transaction, transaction))
            throw new InvalidOperationException("Mutation boundary must be owned by the active transaction.");
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
    }

    private void ReleaseStandalonePublicationOwnership()
    {
        EnsureMutationLockHeld();
        if (_standalonePublicationOwnerThreadId != Environment.CurrentManagedThreadId)
            throw new InvalidOperationException("Only the standalone publication owner can release storage ownership.");
        _standalonePublicationOwnerThreadId = 0;
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
        _publishChanges = publishChanges;
        _completion = completion;
    }

    internal MutationScope BeginMutation() => BeginMutation(publishStandaloneOnDispose: true);

    // Equipment owns standalone lifecycle effects and must run them before publication.
    internal MutationScope BeginDomainMutation() => BeginMutation(publishStandaloneOnDispose: false);

    internal void EnsureStandaloneOperation()
    {
        Monitor.Enter(_mutationLock);
        try
        {
            if (_transaction != null || HasStandalonePublicationOwner)
                throw new InvalidOperationException("This operation requires storage that is not transaction-bound or publishing.");
        }
        finally
        {
            Monitor.Exit(_mutationLock);
        }
    }

    private MutationScope BeginMutation(bool publishStandaloneOnDispose)
    {
        var ownsLock = !Monitor.IsEntered(_mutationLock);
        if (ownsLock)
        {
            Monitor.Enter(_mutationLock);
        }
        else if (_transaction == null)
        {
            throw new InvalidOperationException("A held mutation lock must belong to an active item-container transaction.");
        }

        try
        {
            EnsureMutationAccess();
            return new MutationScope(this, ownsLock, _transaction, publishStandaloneOnDispose);
        }
        catch
        {
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
        private bool _recorded;
        private bool _disposed;
        private HashSet<int>? _slots;

        internal ItemContainerTransaction? Transaction => _transaction;

        internal MutationScope(ItemContainerMutationBoundary boundary, bool ownsLock,
            ItemContainerTransaction? transaction, bool publishStandaloneOnDispose)
        {
            _boundary = boundary;
            _ownsLock = ownsLock;
            _transaction = transaction;
            _publishStandaloneOnDispose = publishStandaloneOnDispose;
            _recorded = false;
            _disposed = false;
            _slots = null;
        }

        internal void RecordChanges(HashSet<int>? slots)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(MutationScope));
            if (_recorded) throw new InvalidOperationException("Mutation changes can only be recorded once per operation.");
            if (!Monitor.IsEntered(_boundary._mutationLock))
                throw new InvalidOperationException("Mutation changes must be recorded while holding the storage mutation lock.");

            if (_transaction is { } transaction)
            {
                transaction.RecordChanges(_boundary, slots);
            }
            else
            {
                _boundary.ClaimStandalonePublicationOwnership();
                _slots = slots;
            }
            _recorded = true;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_ownsLock) Monitor.Exit(_boundary._mutationLock);

            if (_ownsLock && _transaction == null && _recorded && _publishStandaloneOnDispose)
                _boundary.PublishCommittedChanges(_slots);
        }
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
        transaction.RecordChanges(this, sourceSlots);
        transaction.RecordChanges(destination, destinationSlots);
        return true;
    }

    internal void PublishCommittedChanges(HashSet<int>? slots)
    {
        ThreadInterruptedException? interruption = null;
        bool standalonePublication;
        EnterMutationLock(ref interruption);
        try
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
        }
        finally
        {
            Monitor.Exit(_mutationLock);
        }

        if (!standalonePublication)
        {
            if (interruption is not null) ExceptionDispatchInfo.Capture(interruption).Throw();
            _publishChanges?.Invoke(slots);
            return;
        }

        ExceptionDispatchInfo? publicationFailure = null;
        try
        {
            _publishChanges?.Invoke(slots);
        }
        catch (Exception exception)
        {
            publicationFailure = ExceptionDispatchInfo.Capture(exception);
        }
        finally
        {
            ReleaseStandalonePublicationOwnership(ref interruption);
        }

        if (publicationFailure is not null) publicationFailure.Throw();
        if (interruption is not null) ExceptionDispatchInfo.Capture(interruption).Throw();
    }

    private void EnterMutationLock(ref ThreadInterruptedException? interruption)
    {
        while (true)
        {
            try
            {
                Monitor.Enter(_mutationLock);
                return;
            }
            catch (ThreadInterruptedException exception)
            {
                interruption ??= exception;
            }
        }
    }

    private void ReleaseStandalonePublicationOwnership(ref ThreadInterruptedException? interruption)
    {
        while (true)
        {
            try
            {
                Monitor.Enter(_mutationLock);
                break;
            }
            catch (ThreadInterruptedException exception)
            {
                interruption ??= exception;
            }
        }

        try
        {
            ReleaseStandalonePublicationOwnership();
            Monitor.PulseAll(_mutationLock);
        }
        finally
        {
            Monitor.Exit(_mutationLock);
        }
    }

}
