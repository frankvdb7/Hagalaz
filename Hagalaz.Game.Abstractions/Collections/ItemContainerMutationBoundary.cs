using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Coordinates mutations and publication for one owned item storage.</summary>
internal sealed class ItemContainerMutationBoundary
{
    private readonly ItemContainerStorage _storage;
    private readonly IItemContainerCompletionOwner? _completion;
    private readonly Action<HashSet<int>?>? _publishChanges;

    private ItemContainerTransaction EnsureActiveTransaction()
    {
        var transaction = _storage.Transaction
            ?? throw new InvalidOperationException("Storage must belong to an active transaction.");
        transaction.EnsureActive();
        return transaction;
    }

    internal ItemContainerStorage Storage => _storage;
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
        Monitor.Enter(_storage.MutationLock);
        try
        {
            if (_storage.Transaction != null || _storage.HasStandalonePublicationOwner)
                throw new InvalidOperationException("This operation requires storage that is not transaction-bound or publishing.");
        }
        finally
        {
            Monitor.Exit(_storage.MutationLock);
        }
    }

    private MutationScope BeginMutation(bool publishStandaloneOnDispose)
    {
        var ownsLock = !Monitor.IsEntered(_storage.MutationLock);
        if (ownsLock)
        {
            Monitor.Enter(_storage.MutationLock);
        }
        else if (_storage.Transaction == null)
        {
            throw new InvalidOperationException("A held mutation lock must belong to an active item-container transaction.");
        }

        try
        {
            _storage.EnsureMutationAccess();
            return new MutationScope(this, ownsLock, _storage.Transaction, publishStandaloneOnDispose);
        }
        catch
        {
            if (ownsLock) Monitor.Exit(_storage.MutationLock);
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
            if (!Monitor.IsEntered(_boundary._storage.MutationLock))
                throw new InvalidOperationException("Mutation changes must be recorded while holding the storage mutation lock.");

            if (_transaction is { } transaction)
            {
                transaction.RecordChanges(_boundary._storage, slots);
            }
            else
            {
                _boundary._storage.ClaimStandalonePublicationOwnership();
                _slots = slots;
            }
            _recorded = true;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_ownsLock) Monitor.Exit(_boundary._storage.MutationLock);

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
        transaction.RecordChanges(_storage, sourceSlots);
        transaction.RecordChanges(destination._storage, destinationSlots);
        return true;
    }

    internal void PublishCommittedChanges(HashSet<int>? slots)
    {
        List<Exception>? failures = null;
        bool standalonePublication;
        EnterMutationLock(ref failures);
        try
        {
            if (_storage.Transaction != null)
            {
                if (_storage.HasStandalonePublicationOwner)
                    throw new InvalidOperationException("Storage cannot be transaction-bound and standalone-publishing at once.");
                standalonePublication = false;
            }
            else if (_storage.IsStandalonePublicationOwnedByCurrentThread)
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
            Monitor.Exit(_storage.MutationLock);
        }

        if (!standalonePublication)
        {
            try { _publishChanges?.Invoke(slots); }
            catch (Exception exception) { (failures ??= []).Add(exception); }
            ThrowFailures(failures);
            return;
        }

        try
        {
            _publishChanges?.Invoke(slots);
        }
        catch (Exception exception)
        {
            (failures ??= []).Add(exception);
        }
        finally
        {
            ReleaseStandalonePublicationOwnership(ref failures);
        }

        ThrowFailures(failures);
    }

    private void EnterMutationLock(ref List<Exception>? failures)
    {
        while (true)
        {
            try
            {
                Monitor.Enter(_storage.MutationLock);
                return;
            }
            catch (ThreadInterruptedException exception)
            {
                (failures ??= []).Add(exception);
            }
        }
    }

    private void ReleaseStandalonePublicationOwnership(ref List<Exception>? failures)
    {
        while (true)
        {
            try
            {
                Monitor.Enter(_storage.MutationLock);
                break;
            }
            catch (ThreadInterruptedException exception)
            {
                (failures ??= []).Add(exception);
            }
        }

        try
        {
            try { _storage.ReleaseStandalonePublicationOwnership(); }
            catch (Exception exception) { (failures ??= []).Add(exception); }

            try { Monitor.PulseAll(_storage.MutationLock); }
            catch (Exception exception) { (failures ??= []).Add(exception); }
        }
        finally
        {
            try { Monitor.Exit(_storage.MutationLock); }
            catch (Exception exception) { (failures ??= []).Add(exception); }
        }
    }

    private static void ThrowFailures(List<Exception>? failures)
    {
        if (failures is { Count: 1 }) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures is { Count: > 1 })
        {
            throw new AggregateException(failures.SelectMany(failure => failure is AggregateException aggregate
                ? aggregate.Flatten().InnerExceptions.AsEnumerable()
                : [failure]));
        }
    }

    internal void DiscardPendingCompletion(ItemContainerTransaction transaction) => _completion?.DiscardPendingCompletion(transaction);
    internal void CompleteBeforePublication(ItemContainerTransaction transaction, int order) => _completion?.CompleteBeforePublication(transaction, order);
    internal void CompleteAfterPublication(ItemContainerTransaction transaction, int order) => _completion?.CompleteAfterPublication(transaction, order);
}
