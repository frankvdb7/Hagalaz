using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Coordinates synchronous atomic item-container mutations, rollback, and deferred publication.</summary>
/// <remarks>
/// Mutations affect live storage eagerly; unsynchronized readers are not isolated from them.
/// Dispose without Commit restores captured storage. Begin, mutations, Commit and Dispose must run
/// on the originating thread. Do not cross await boundaries or transfer a scope to another thread.
/// Independent scopes on other threads serialize through the participating storage locks.
/// </remarks>
public sealed class ItemContainerTransaction : IDisposable
{
    private readonly int _threadId = Environment.CurrentManagedThreadId;
    private readonly ItemContainerMutationBoundary[] _boundaries;
    private readonly ItemContainerStorage[] _lockOrder;
    private readonly List<StorageSnapshot> _snapshots = [];
    private readonly Dictionary<ItemContainerStorage, HashSet<int>?> _changed = [];
    private readonly List<Action> _hooks = [];
    private readonly List<Action> _pouchNotifications = [];
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
    public static ItemContainerTransaction Begin(params IItemContainerTransactionParticipant[] participants)
        => BeginResolved(Resolve(participants));

    private static ItemContainerTransaction BeginResolved(ItemContainerMutationBoundary[] boundaries)
    {
        foreach (var boundary in boundaries)
            if (boundary.Storage.Transaction is { IsOwnedByCurrentThread: true })
                throw new InvalidOperationException("Storage already belongs to a transaction on this thread.");

        var transaction = new ItemContainerTransaction(boundaries);
        try
        {
            foreach (var storage in transaction._lockOrder)
            {
                Monitor.Enter(storage.MutationLock);
                transaction._locksAcquired++;
                if (storage.Transaction != null) throw new InvalidOperationException("Storage already belongs to a transaction.");
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
            transaction.ReleaseResources([constructionFailure]);
            throw;
        }
    }

    /// <summary>Declares the current storage irreversible, unlocks, and performs deferred hooks and publication once.</summary>
    /// <remarks>
    /// Mutations already affect storage under locks; Commit discards rollback ability rather than applying staged data.
    /// Hook or publication failures propagate after storage has permanently committed. Dispose cannot undo that state,
    /// and another Commit cannot retry completion. A container publication failure skips later containers and all pouch
    /// notifications. Multiple independent hook and publication failures are retained in a flat AggregateException.
    /// </remarks>
    public void Commit()
    {
        EnsureActive();
        _state = TransactionState.Committed;
        _snapshots.Clear();
        ReleaseResources(); // No external completion may run until every transaction lock is released.

        List<Exception>? failures = null;
        foreach (var hook in _hooks)
        {
            try { hook(); }
            catch (Exception exception)
            {
                (failures ??= []).Add(exception);
            }
        }
        try
        {
            foreach (var boundary in _boundaries)
                if (_changed.TryGetValue(boundary.Storage, out var slots)) boundary.PublishCommittedChanges(slots);
            foreach (var notification in _pouchNotifications) notification();
        }
        catch (Exception publicationFailure)
        {
            (failures ??= []).Add(publicationFailure);
        }
        ThrowFailures(failures);
        _state = TransactionState.Completed;
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
            ReleaseResources(failures);
        }
    }

    // Only current-thread bindings are joinable. Other owners serialize through ordered locks.
    internal static ItemContainerTransaction? BeginIfNeeded(params IItemContainerTransactionParticipant[] participants)
    {
        var boundaries = Resolve(participants);
        var active = boundaries.Select(boundary => boundary.Storage.Transaction)
            .FirstOrDefault(transaction => transaction is { IsOwnedByCurrentThread: true });
        if (active == null) return BeginResolved(boundaries);
        active.EnsureActive();
        if (boundaries.Any(boundary => !ReferenceEquals(boundary.Storage.Transaction, active)))
            throw new InvalidOperationException("Every required storage must belong to the same transaction.");
        return null;
    }

    internal void RecordChanges(ItemContainerStorage storage, HashSet<int>? slots)
    {
        EnsureActive();
        if (slots is { Count: 0 }) return;
        if (slots == null) _changed[storage] = null;
        else if (_changed.TryGetValue(storage, out var existing)) existing?.UnionWith(slots);
        else _changed.Add(storage, new HashSet<int>(slots));
    }

    internal void DeferBeforePublication(Action hook) { EnsureActive(); _hooks.Add(hook); }
    internal void DeferPouchNotification(Action notification) { EnsureActive(); _pouchNotifications.Add(notification); }

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

    private void ReleaseResources(List<Exception>? failures = null)
    {
        for (var index = _lockOrder.Length - 1; index >= 0; index--)
        {
            var storage = _lockOrder[index];
            try { if (ReferenceEquals(storage.Transaction, this)) storage.Transaction = null; }
            catch (Exception exception) { (failures ??= []).Add(exception); }
        }
        while (_locksAcquired > 0)
        {
            var storage = _lockOrder[--_locksAcquired];
            try { Monitor.Exit(storage.MutationLock); }
            catch (Exception exception) { (failures ??= []).Add(exception); }
        }
        ThrowFailures(failures);
    }

    private static void ThrowFailures(List<Exception>? failures)
    {
        if (failures is { Count: 1 }) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures is { Count: > 1 })
            throw new AggregateException(failures.SelectMany(failure => failure is AggregateException aggregate
                ? aggregate.Flatten().InnerExceptions.AsEnumerable()
                : [failure]));
    }

    private static ItemContainerMutationBoundary[] Resolve(IItemContainerTransactionParticipant[] participants)
    {
        ArgumentNullException.ThrowIfNull(participants);
        if (participants.Length == 0) throw new ArgumentException("At least one participant is required.", nameof(participants));
        var seen = new HashSet<IItemContainerTransactionParticipant>(ReferenceEqualityComparer.Instance);
        var storages = new HashSet<ItemContainerStorage>();
        var boundaries = new List<ItemContainerMutationBoundary>();
        foreach (var participant in participants)
        {
            ArgumentNullException.ThrowIfNull(participant);
            if (!seen.Add(participant)) continue;
            if (participant is not IItemContainerTransactionParticipantInternal implementation)
                throw new ArgumentException("Use the participant provided by a container's Mutations property.", nameof(participants));
            var contributions = implementation.Boundaries;
            if (contributions == null || contributions.Count == 0)
                throw new ArgumentException("A participant must contribute storage.", nameof(participants));
            foreach (var boundary in contributions)
            {
                if (boundary == null) throw new ArgumentException("A participant contributed a null boundary.", nameof(participants));
                if (storages.Add(boundary.Storage)) boundaries.Add(boundary);
            }
        }
        return boundaries.ToArray();
    }

    private enum TransactionState { Active, Committed, Completed, Disposed }
    private sealed record StorageSnapshot(ItemContainerStorage Storage, IItem?[] Items, int[] Counts, int Revision);
}
