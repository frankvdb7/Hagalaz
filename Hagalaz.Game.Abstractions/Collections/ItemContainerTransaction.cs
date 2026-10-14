using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Coordinates synchronous atomic item-container mutations and rollback.</summary>
/// <remarks>
/// This is a synchronous scope. Begin acquires and binds all participating mutation boundaries; mutations affect live
/// storage eagerly while the Active scope owns their locks. Commit makes storage irreversible, releases physical
/// locks, and completes registered domain owners while retaining logical bindings until disposal. Overlapping callers
/// that own no storage wait; a caller already
/// owning storage rejects contended additional acquisition. Same-thread reentrant overlap is rejected.
/// Dispose without Commit restores storage before releasing the scope. Begin, mutations, Commit,
/// publication, and Dispose must run on the originating thread. Rollback restores slot topology, item references, item
/// counts, and storage revision; it does not deep-snapshot arbitrary mutable item metadata such as
/// <c>ExtraData</c>. Replace an item transactionally when rollback-sensitive metadata changes. Do not cross
/// await boundaries.
/// </remarks>
public sealed class ItemContainerTransaction : IDisposable
{
    private readonly int _threadId = Environment.CurrentManagedThreadId;
    private readonly ItemContainerMutationBoundary[] _publicationOrder;
    private readonly IItemContainerCompletionOwner[] _completionOwners;
    private readonly List<StorageSnapshot> _snapshots = [];
    private ItemContainerLockSet? _lockSet;
    private TransactionState _state;

    private ItemContainerTransaction(ItemContainerMutationBoundary[] publicationOrder)
    {
        _publicationOrder = publicationOrder;
        var completionOwners = new List<IItemContainerCompletionOwner>();
        var seenOwners = new HashSet<IItemContainerCompletionOwner>(ReferenceEqualityComparer.Instance);
        foreach (var boundary in publicationOrder)
            if (boundary.CompletionOwner is { } owner && seenOwners.Add(owner)) completionOwners.Add(owner);
        _completionOwners = completionOwners.ToArray();
    }

    /// <summary>Resolves every participant, acquires ordered boundary locks and captures rollback state before binding the scope.</summary>
    /// <exception cref="ArgumentException">Participants are empty, unsupported, or contribute invalid storage.</exception>
    /// <exception cref="InvalidOperationException">A participant already belongs to a scope on the current thread.</exception>
    /// <remarks>
    /// Use a container directly for a single-container operation. <c>TryTransferTo</c> is already atomic for one
    /// transfer. For several operations that must succeed together, pass their domain participants to <c>Begin</c>,
    /// perform ordinary container operations, then call <see cref="Commit"/>. Composite participants may contribute
    /// multiple storage boundaries.
    /// </remarks>
    public static ItemContainerTransaction Begin(params IItemTransactional[] participants)
        => BeginResolved(Resolve(participants));

    internal static ItemContainerMutationBoundary ResolveSingleBoundary(IItemTransactional participant)
    {
        ArgumentNullException.ThrowIfNull(participant);
        if (participant is not IItemTransactionSource source)
            throw new ArgumentException("Unsupported item-container participant.", nameof(participant));

        var boundaries = source.Boundaries;
        if (boundaries == null || boundaries.Count != 1 || boundaries[0] == null)
            throw new ArgumentException("This operation requires exactly one item storage.", nameof(participant));

        return boundaries[0];
    }

    private static ItemContainerTransaction BeginResolved(ItemContainerMutationBoundary[] boundaries)
    {
        var transaction = new ItemContainerTransaction(boundaries);
        try
        {
            transaction._lockSet = ItemContainerLockSet.Acquire(boundaries);
            foreach (var boundary in boundaries)
            {
                var storage = boundary.Storage;
                var items = storage.ToArray();
                transaction._snapshots.Add(new StorageSnapshot(boundary, items,
                    items.Select(item => item?.Count ?? 0).ToArray(), storage.MutationRevision));
            }
            foreach (var boundary in boundaries) boundary.Transaction = transaction;
            return transaction;
        }
        catch
        {
            foreach (var boundary in boundaries)
                if (ReferenceEquals(boundary.Transaction, transaction)) boundary.Transaction = null;
            transaction._snapshots.Clear();
            transaction._lockSet?.Dispose();
            throw;
        }
    }

    /// <summary>Makes live storage irreversible and releases physical mutation locks while retaining scope ownership until disposal.</summary>
    /// <remarks>
    /// Mutations already affect live storage under locks; Commit discards rollback ability rather than applying staged data.
    /// After releasing physical locks, Commit completes domain effects and publishes committed changes. If either phase
    /// throws, storage remains committed and disposal releases ownership without rolling back. A second Commit is invalid.
    /// </remarks>
    public void Commit()
    {
        EnsureActive();
        _state = TransactionState.Committed;
        _snapshots.Clear();
        try
        {
            foreach (var boundary in _publicationOrder)
                boundary.ChangeTracker.PromotePending();
        }
        finally
        {
            _lockSet!.ReleaseLocksPreservingOwnership();
        }

        foreach (var owner in _completionOwners) owner.CompleteCommittedEffects();
        foreach (var owner in _completionOwners) owner.PublishCommittedChanges();
    }

    /// <summary>Restores all captured storage if still active; disposal after commit or disposal is inert.</summary>
    public void Dispose()
    {
        EnsureThread();
        if (_state == TransactionState.Committed)
        {
            CompleteCommittedScope();
            return;
        }
        if (_state != TransactionState.Active) return;
        _state = TransactionState.Disposed;
        List<Exception>? failures = null;
        try
        {
            foreach (var snapshot in _snapshots)
            {
                try { snapshot.Boundary.RestoreTransactionState(this, snapshot.Items, snapshot.Counts, snapshot.Revision); }
                catch (Exception exception) { (failures ??= []).Add(exception); }
            }
        }
        finally
        {
            _snapshots.Clear();
            foreach (var boundary in _publicationOrder)
                boundary.ChangeTracker.DiscardPending();
            try { DiscardPendingCompletion(); }
            finally
            {
                try { ClearScopeBindings(); }
                finally { _lockSet!.Dispose(); }
            }
        }
        ThrowFailures(failures);
    }

    private void CompleteCommittedScope()
    {
        foreach (var boundary in _publicationOrder)
        {
            boundary.ChangeTracker.DiscardPending();
            if (boundary.CompletionOwner != null) boundary.ChangeTracker.DiscardCommitted();
        }
        try { DiscardPendingCompletion(); }
        finally
        {
            try { ReleaseCommittedScopeBindings(); }
            finally
            {
                _lockSet!.ReleaseOwnership();
                _state = TransactionState.Completed;
            }
        }
    }

    private void DiscardPendingCompletion()
    {
        foreach (var owner in _completionOwners) owner.DiscardPendingCompletion();
    }

    internal bool IsOwnedByCurrentThread => Environment.CurrentManagedThreadId == _threadId;
    internal bool IsCommittedByCurrentThread => IsOwnedByCurrentThread && _state == TransactionState.Committed;

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

    private void ClearScopeBindings()
    {
        foreach (var boundary in _publicationOrder)
            if (ReferenceEquals(boundary.Transaction, this)) boundary.Transaction = null;
    }

    private void ReleaseCommittedScopeBindings()
    {
        using (ItemContainerLockSet.AcquireForTransactionFinalization(_publicationOrder, this))
        {
            foreach (var boundary in _publicationOrder)
            {
                if (ReferenceEquals(boundary.Transaction, this)) boundary.Transaction = null;
                Monitor.PulseAll(boundary.MutationLock);
            }
        }
    }

    private static void ThrowFailures(List<Exception>? failures)
    {
        if (failures is { Count: 1 }) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures is { Count: > 1 })
            throw new AggregateException(failures).Flatten();
    }

    private static ItemContainerMutationBoundary[] Resolve(IItemTransactional[] participants)
    {
        ArgumentNullException.ThrowIfNull(participants);
        if (participants.Length == 0) throw new ArgumentException("At least one participant is required.", nameof(participants));
        var boundariesByStorage = new Dictionary<ItemContainerStorage, ItemContainerMutationBoundary>();
        var boundaries = new List<ItemContainerMutationBoundary>();
        foreach (var participant in participants)
        {
            ArgumentNullException.ThrowIfNull(participant);
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
    private sealed record StorageSnapshot(ItemContainerMutationBoundary Boundary, IItem?[] Items, int[] Counts, int Revision);
}
