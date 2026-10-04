using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Coordinates synchronous atomic item-container mutations, rollback, and deferred publication.</summary>
/// <remarks>
/// This is a synchronous scope. Begin acquires and binds all participating storage; mutations affect live
/// storage eagerly while the Active scope owns its mutation locks. Commit makes storage irreversible and
/// releases mutation locks before committed completion and publication, while storage remains bound to
/// this scope until that work finishes. Foreign overlapping scopes wait; same-thread reentrant overlap is
/// rejected. Dispose without Commit restores storage before releasing the scope. Begin, mutations, Commit,
/// and Dispose must run on the originating thread. Rollback restores slot topology, item references, item
/// counts, and storage revision; it does not deep-snapshot arbitrary mutable item metadata such as
/// <c>ExtraData</c>. Replace an item transactionally when rollback-sensitive metadata changes. Do not cross
/// await boundaries.
/// </remarks>
public sealed class ItemContainerTransaction : IDisposable
{
    private readonly int _threadId = Environment.CurrentManagedThreadId;
    private readonly ItemContainerMutationBoundary[] _boundaries;
    private readonly ItemContainerStorage[] _lockOrder;
    private readonly List<StorageSnapshot> _snapshots = [];
    private readonly Dictionary<ItemContainerStorage, HashSet<int>?> _changed = [];
    private int _completionCount;
    private int _locksAcquired;
    private TransactionState _state;

    private ItemContainerTransaction(ItemContainerMutationBoundary[] boundaries)
    {
        _boundaries = boundaries;
        _lockOrder = boundaries.Select(boundary => boundary.Storage).OrderBy(storage => storage.MutationOrder).ToArray();
    }

    /// <summary>Resolves every participant, acquires ordered locks and captures rollback state before binding the scope.</summary>
    /// <exception cref="ArgumentException">Participants are empty, unsupported, or contribute invalid storage.</exception>
    /// <exception cref="InvalidOperationException">A participant already belongs to a scope on the current thread.</exception>
    public static ItemContainerTransaction Begin(params IItemTransactional[] participants)
        => BeginResolved(Resolve(participants));

    internal static ItemContainerMutationBoundary ResolveSingleBoundary(IItemTransactional participant)
    {
        ArgumentNullException.ThrowIfNull(participant);
        if (participant is not IItemTransactionSource source)
            throw new ArgumentException("Unsupported item transaction participant.", nameof(participant));

        var boundaries = source.Boundaries;
        if (boundaries == null || boundaries.Count != 1 || boundaries[0] == null)
            throw new ArgumentException("This operation requires exactly one item storage.", nameof(participant));

        return boundaries[0];
    }

    private static ItemContainerTransaction BeginResolved(ItemContainerMutationBoundary[] boundaries)
    {
        foreach (var boundary in boundaries)
            if (boundary.Storage.Transaction is { IsOwnedByCurrentThread: true })
                throw new InvalidOperationException("Storage already belongs to a transaction on this thread.");

        var transaction = new ItemContainerTransaction(boundaries);
        try
        {
            while (true)
            {
                var retry = false;
                foreach (var storage in transaction._lockOrder)
                {
                    Monitor.Enter(storage.MutationLock);
                    transaction._locksAcquired++;
                    if (storage.Transaction is not { } owner) continue;
                    if (owner.IsOwnedByCurrentThread)
                        throw new InvalidOperationException("Storage already belongs to a transaction on this thread.");

                    // Do not retain a lock prefix while waiting: the owner reacquires its ordered set to
                    // release the committed scope, so a retained prefix could deadlock scope teardown.
                    List<Exception>? releaseFailures = null;
                    transaction.ReleaseMutationLocks(ref releaseFailures);
                    ThrowFailures(releaseFailures);
                    Monitor.Enter(storage.MutationLock);
                    try
                    {
                        while (storage.Transaction is { } currentOwner)
                        {
                            if (currentOwner.IsOwnedByCurrentThread)
                                throw new InvalidOperationException("Storage already belongs to a transaction on this thread.");
                            Monitor.Wait(storage.MutationLock);
                        }
                    }
                    finally { Monitor.Exit(storage.MutationLock); }
                    retry = true;
                    break;
                }
                if (!retry) break;
            }
            foreach (var storage in transaction._lockOrder)
            {
                var items = storage.ToArray();
                transaction._snapshots.Add(new StorageSnapshot(storage, items,
                    items.Select(item => item?.Count ?? 0).ToArray(), storage.MutationRevision));
            }
            foreach (var storage in transaction._lockOrder) storage.Transaction = transaction;
            return transaction;
        }
        catch (Exception constructionFailure)
        {
            transaction._snapshots.Clear();
            transaction.ReleaseUncommittedResources([constructionFailure]);
            throw;
        }
    }

    /// <summary>Declares live storage irreversible, releases mutation locks, performs committed completion/publication, and releases the scope.</summary>
    /// <remarks>
    /// Mutations already affect live storage under locks; Commit discards rollback ability rather than applying staged data.
    /// Callbacks run after mutation locks are released but before storage scope bindings are released. Hook or publication
    /// failures propagate after storage has permanently committed. Dispose cannot undo that state, and another Commit
    /// cannot retry completion. Fixed domain completion runs before and after container publication in mutation order.
    /// Container failure skips later containers and all post-publication completion. A post-publication failure skips later
    /// completion. Multiple independent failures are retained in a flat AggregateException.
    /// </remarks>
    public void Commit()
    {
        EnsureActive();
        _state = TransactionState.Committed;
        _snapshots.Clear();
        List<Exception>? failures = null;
        var mutationLocksReleased = ReleaseMutationLocks(ref failures);
        try
        {
            if (mutationLocksReleased)
            {
                // Ordinals preserve mutation order across owners; executable work stays with each owner.
                for (var order = 0; order < _completionCount; order++)
                    foreach (var boundary in _boundaries)
                    {
                        try { boundary.CompleteBeforePublication(this, order); }
                        catch (Exception exception) { (failures ??= []).Add(exception); }
                    }
                foreach (var boundary in _boundaries)
                    if (_changed.TryGetValue(boundary.Storage, out var slots)) boundary.PublishCommittedChanges(slots);
                for (var order = 0; order < _completionCount; order++)
                    foreach (var boundary in _boundaries) boundary.CompleteAfterPublication(this, order);
            }
        }
        catch (Exception completionFailure)
        {
            (failures ??= []).Add(completionFailure);
        }
        finally
        {
            DiscardPendingCompletion(ref failures);
            ReleaseCommittedScopeBindings(ref failures);
            _state = TransactionState.Completed;
        }
        ThrowFailures(failures);
    }

    /// <summary>Restores all captured storage if still active; disposal after commit or disposal is inert.</summary>
    public void Dispose()
    {
        EnsureThread();
        if (_state != TransactionState.Active) return;
        _state = TransactionState.Disposed;
        List<Exception>? failures = null;
        try
        {
            foreach (var snapshot in _snapshots)
            {
                try { snapshot.Storage.RestoreTransactionState(snapshot.Items, snapshot.Counts, snapshot.Revision); }
                catch (Exception exception) { (failures ??= []).Add(exception); }
            }
        }
        finally
        {
            _snapshots.Clear();
            DiscardPendingCompletion(ref failures);
            ClearScopeBindingsAndPulse(ref failures);
            ReleaseMutationLocks(ref failures);
        }
        ThrowFailures(failures);
    }

    internal void RecordChanges(ItemContainerStorage storage, HashSet<int>? slots)
    {
        EnsureActive();
        if (slots is { Count: 0 }) return;
        if (slots == null) _changed[storage] = null;
        else if (_changed.TryGetValue(storage, out var existing)) existing?.UnionWith(slots);
        else _changed.Add(storage, new HashSet<int>(slots));
    }

    // A domain records this ordinal with its own pending facts, never executable work in the scope.
    internal int NextCompletionOrder() { EnsureActive(); return _completionCount++; }

    private void DiscardPendingCompletion(ref List<Exception>? failures)
    {
        foreach (var boundary in _boundaries)
        {
            try { boundary.DiscardPendingCompletion(this); }
            catch (Exception exception) { (failures ??= []).Add(exception); }
        }
    }

    internal bool IsOwnedByCurrentThread => Environment.CurrentManagedThreadId == _threadId;

    internal void EnsureActive()
    {
        EnsureThread();
        if (_state != TransactionState.Active) throw new InvalidOperationException("The transaction is no longer active.");
    }

    private void EnsureThread()
    {
        if (!IsOwnedByCurrentThread)
            throw new InvalidOperationException("Transaction use must occur on its originating thread.");
    }

    private bool ReleaseMutationLocks(ref List<Exception>? failures)
    {
        var succeeded = true;
        while (_locksAcquired > 0)
        {
            var storage = _lockOrder[--_locksAcquired];
            try { Monitor.Exit(storage.MutationLock); }
            catch (Exception exception) { succeeded = false; (failures ??= []).Add(exception); }
        }
        return succeeded;
    }

    private void ReleaseUncommittedResources(List<Exception>? failures = null)
    {
        ClearScopeBindingsAndPulse(ref failures);
        ReleaseMutationLocks(ref failures);
        ThrowFailures(failures);
    }

    private void ClearScopeBindingsAndPulse(ref List<Exception>? failures)
    {
        for (var index = _lockOrder.Length - 1; index >= 0; index--)
        {
            var storage = _lockOrder[index];
            try
            {
                if (ReferenceEquals(storage.Transaction, this)) storage.Transaction = null;
                if (Monitor.IsEntered(storage.MutationLock)) Monitor.PulseAll(storage.MutationLock);
            }
            catch (Exception exception) { (failures ??= []).Add(exception); }
        }
    }

    private void ReleaseCommittedScopeBindings(ref List<Exception>? failures)
    {
        var acquired = 0;
        try
        {
            foreach (var storage in _lockOrder)
            {
                EnterForCommittedScopeCleanup(storage.MutationLock, ref failures);
                acquired++;
            }

            foreach (var storage in _lockOrder)
            {
                if (ReferenceEquals(storage.Transaction, this)) storage.Transaction = null;
            }

            foreach (var storage in _lockOrder)
            {
                Monitor.PulseAll(storage.MutationLock);
            }
        }
        finally
        {
            while (acquired > 0)
            {
                try { Monitor.Exit(_lockOrder[--acquired].MutationLock); }
                catch (Exception exception) { (failures ??= []).Add(exception); }
            }
        }
    }

    private static void EnterForCommittedScopeCleanup(object mutationLock, ref List<Exception>? failures)
    {
        while (true)
        {
            try
            {
                Monitor.Enter(mutationLock);
                return;
            }
            catch (ThreadInterruptedException exception)
            {
                (failures ??= []).Add(exception);
            }
        }
    }

    private static void ThrowFailures(List<Exception>? failures)
    {
        if (failures is { Count: 1 }) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures is { Count: > 1 })
            throw new AggregateException(failures.SelectMany(failure => failure is AggregateException aggregate
                ? aggregate.Flatten().InnerExceptions.AsEnumerable()
                : [failure]));
    }

    private static ItemContainerMutationBoundary[] Resolve(IItemTransactional[] participants)
    {
        ArgumentNullException.ThrowIfNull(participants);
        if (participants.Length == 0) throw new ArgumentException("At least one participant is required.", nameof(participants));
        var seen = new HashSet<IItemTransactional>(ReferenceEqualityComparer.Instance);
        var boundariesByStorage = new Dictionary<ItemContainerStorage, ItemContainerMutationBoundary>();
        var boundaries = new List<ItemContainerMutationBoundary>();
        foreach (var participant in participants)
        {
            ArgumentNullException.ThrowIfNull(participant);
            if (!seen.Add(participant)) continue;
            if (participant is not IItemTransactionSource source)
                throw new ArgumentException("Use an item-transactional object provided by the item-container domain.", nameof(participants));
            var contributions = source.Boundaries;
            if (contributions == null || contributions.Count == 0)
                throw new ArgumentException("A participant must contribute storage.", nameof(participants));
            foreach (var boundary in contributions)
            {
                if (boundary == null) throw new ArgumentException("A participant contributed a null boundary.", nameof(participants));
                if (boundariesByStorage.TryGetValue(boundary.Storage, out var existing))
                {
                    if (!ReferenceEquals(existing, boundary))
                    {
                        throw new ArgumentException(
                            "Multiple transaction boundaries were contributed for the same storage.",
                            nameof(participants));
                    }

                    continue;
                }

                boundariesByStorage.Add(boundary.Storage, boundary);
                boundaries.Add(boundary);
            }
        }
        return boundaries.ToArray();
    }

    private enum TransactionState { Active, Committed, Completed, Disposed }
    private sealed record StorageSnapshot(ItemContainerStorage Storage, IItem?[] Items, int[] Counts, int Revision);
}
