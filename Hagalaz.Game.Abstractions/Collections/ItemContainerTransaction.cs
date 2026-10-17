using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Coordinates synchronous atomic item-container mutations and rollback.</summary>
/// <remarks>
/// Begin acquires and binds all participating storage synchronizations; mutations affect live storage while the active
/// scope owns their locks. Commit makes storage irreversible, releases physical locks, and completes registered domain
/// owners while retaining logical ownership until Dispose. Dispose without Commit restores storage before releasing
/// the scope. Rollback restores slot topology, item references, item counts, and storage revision, but does not deep
/// snapshot arbitrary mutable item metadata such as <c>ExtraData</c>. Replace an item transactionally when
/// rollback-sensitive metadata changes. Do not cross await boundaries.
/// </remarks>
public sealed class ItemContainerTransaction : IDisposable
{
    private readonly int _threadId = Environment.CurrentManagedThreadId;
    private readonly ItemContainerSynchronization[] _publicationOrder;
    private readonly IItemContainerCompletionOwner[] _completionOwners;
    private readonly List<StorageSnapshot> _snapshots = [];
    private ItemContainerLockSet? _lockSet;
    private bool _ownsThreadStorageCount;
    private TransactionState _state;

    private ItemContainerTransaction(ItemContainerSynchronization[] publicationOrder)
    {
        _publicationOrder = publicationOrder;
        var completionOwners = new List<IItemContainerCompletionOwner>();
        var seenOwners = new HashSet<IItemContainerCompletionOwner>(ReferenceEqualityComparer.Instance);
        foreach (var synchronization in publicationOrder)
            if (synchronization.CompletionOwner is { } owner && seenOwners.Add(owner))
                completionOwners.Add(owner);
        _completionOwners = completionOwners.ToArray();
    }

    /// <summary>Resolves participants, captures rollback state, and starts an atomic synchronous mutation scope.</summary>
    /// <remarks>
    /// Use a container directly for a single-container operation. <c>TryTransferTo</c> is already atomic for one
    /// transfer. For several operations that must succeed together, pass their domain participants to <c>Begin</c>,
    /// perform ordinary container operations, then call <see cref="Commit"/>. Composite participants may contribute
    /// multiple storage synchronizations.
    /// </remarks>
    public static ItemContainerTransaction Begin(params IItemTransactional[] participants)
        => BeginResolved(Resolve(participants));

    internal static ItemContainerSynchronization ResolveSingleSynchronization(IItemTransactional participant)
    {
        ArgumentNullException.ThrowIfNull(participant);
        if (participant is not IItemTransactionSource source)
            throw new ArgumentException("Unsupported item-container participant.", nameof(participant));

        var synchronizations = source.Synchronizations;
        if (synchronizations == null || synchronizations.Count != 1 || synchronizations[0] == null)
            throw new ArgumentException("This operation requires exactly one item storage.", nameof(participant));

        return synchronizations[0];
    }

    private static ItemContainerTransaction BeginResolved(ItemContainerSynchronization[] synchronizations)
    {
        var transaction = new ItemContainerTransaction(synchronizations);
        try
        {
            transaction._lockSet = ItemContainerLockSet.Acquire(synchronizations);
            ItemContainerSynchronization.AddCurrentThreadStorageOwnership();
            transaction._ownsThreadStorageCount = true;

            foreach (var synchronization in synchronizations)
            {
                var storage = synchronization.Storage;
                var items = storage.ToArray();
                transaction._snapshots.Add(new StorageSnapshot(synchronization, items,
                    items.Select(item => item?.Count ?? 0).ToArray(), storage.MutationRevision));
            }

            foreach (var synchronization in synchronizations) synchronization.BindTransaction(transaction);
            return transaction;
        }
        catch
        {
            if (transaction._lockSet != null)
                foreach (var synchronization in synchronizations)
                    synchronization.UnbindTransaction(transaction);
            transaction._snapshots.Clear();
            transaction._lockSet?.Dispose();
            transaction.ReleaseThreadStorageCount();
            throw;
        }
    }

    /// <summary>Makes live storage irreversible, releases physical locks, then completes registered domain owners.</summary>
    /// <remarks>
    /// Mutations already affect live storage under locks; Commit discards rollback ability rather than applying staged
    /// data. An exception escaping a transaction-owned completion-effects callback is reported as
    /// <see cref="ItemContainerPostCommitCompletionException"/>; an exception escaping a publication-phase callback is
    /// reported as <see cref="ItemContainerPostCommitPublicationException"/>. These types identify the phase in which
    /// an owner reports a failure, not necessarily the underlying cause. For example, Equipment reports retained
    /// lifecycle-hook failures after attempting its state publication, so they surface as publication-phase failures.
    /// The inner exception is the exact exception reported by the owner, storage remains committed, and Dispose releases
    /// ownership without rolling it back. A second Commit is invalid.
    /// </remarks>
    public void Commit()
    {
        EnsureActive();
        _state = TransactionState.Committed;
        _snapshots.Clear();
        try
        {
            foreach (var synchronization in _publicationOrder)
                synchronization.ChangeTracker.PromotePending();
        }
        finally
        {
            _lockSet!.ReleaseLocks();
        }

        foreach (var owner in _completionOwners)
        {
            try
            {
                owner.CompleteCommittedEffects();
            }
            catch (Exception exception)
            {
                throw new ItemContainerPostCommitCompletionException(exception);
            }
        }

        foreach (var owner in _completionOwners)
        {
            try
            {
                owner.PublishCommittedChanges();
            }
            catch (Exception exception)
            {
                throw new ItemContainerPostCommitPublicationException(exception);
            }
        }
    }

    /// <summary>Restores captured storage if still active; disposal after commit or disposal is inert.</summary>
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
                try
                {
                    snapshot.Synchronization.RestoreTransactionState(this, snapshot.Items, snapshot.Counts,
                        snapshot.Revision);
                }
                catch (Exception exception)
                {
                    (failures ??= []).Add(exception);
                }
            }
        }
        finally
        {
            _snapshots.Clear();
            foreach (var synchronization in _publicationOrder)
                synchronization.ChangeTracker.DiscardPending();
            try { DiscardPendingCompletion(); }
            finally
            {
                try { ClearScopeBindings(); }
                finally
                {
                    _lockSet!.Dispose();
                    ReleaseThreadStorageCount();
                }
            }
        }

        ThrowFailures(failures);
    }

    private void CompleteCommittedScope()
    {
        try { DiscardPendingCompletion(); }
        finally
        {
            try
            {
                using var lockSet = ItemContainerLockSet.AcquireForCurrentOwners(_publicationOrder);
                foreach (var synchronization in _publicationOrder)
                {
                    synchronization.ChangeTracker.DiscardPending();
                    if (synchronization.CompletionOwner != null)
                        synchronization.ChangeTracker.DiscardCommitted();
                    synchronization.UnbindTransaction(this, pulseWaiters: true);
                }
            }
            finally
            {
                ReleaseThreadStorageCount();
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
        if (_state != TransactionState.Active)
            throw new InvalidOperationException("The transaction is no longer active.");
    }

    private void EnsureThread()
    {
        if (!IsOwnedByCurrentThread)
            throw new InvalidOperationException("Transaction use must occur on its originating thread.");
    }

    private void ClearScopeBindings()
    {
        foreach (var synchronization in _publicationOrder)
            synchronization.UnbindTransaction(this);
    }

    private void ReleaseThreadStorageCount()
    {
        if (!_ownsThreadStorageCount) return;
        ItemContainerSynchronization.RemoveCurrentThreadStorageOwnership();
        _ownsThreadStorageCount = false;
    }

    private static void ThrowFailures(List<Exception>? failures)
    {
        if (failures is { Count: 1 }) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures is { Count: > 1 })
            throw new AggregateException(failures).Flatten();
    }

    private static ItemContainerSynchronization[] Resolve(IItemTransactional[] participants)
    {
        ArgumentNullException.ThrowIfNull(participants);
        if (participants.Length == 0)
            throw new ArgumentException("At least one participant is required.", nameof(participants));

        var synchronizationsByStorage = new Dictionary<ItemContainerStorage, ItemContainerSynchronization>();
        var synchronizations = new List<ItemContainerSynchronization>();
        foreach (var participant in participants)
        {
            ArgumentNullException.ThrowIfNull(participant);
            if (participant is not IItemTransactionSource source)
                throw new ArgumentException("Use an item-transactional object provided by the item-container domain.",
                    nameof(participants));

            var contributions = source.Synchronizations;
            if (contributions == null || contributions.Count == 0)
                throw new ArgumentException("A participant must contribute storage.", nameof(participants));

            foreach (var synchronization in contributions)
            {
                if (synchronization == null)
                    throw new ArgumentException("A participant contributed null storage synchronization.",
                        nameof(participants));

                if (synchronizationsByStorage.TryGetValue(synchronization.Storage, out var existing))
                {
                    if (!ReferenceEquals(existing, synchronization))
                        throw new ArgumentException(
                            "Multiple synchronizations were contributed for the same storage.", nameof(participants));
                    continue;
                }

                synchronizationsByStorage.Add(synchronization.Storage, synchronization);
                synchronizations.Add(synchronization);
            }
        }

        return synchronizations.ToArray();
    }

    private enum TransactionState { Active, Committed, Completed, Disposed }

    private sealed record StorageSnapshot(ItemContainerSynchronization Synchronization, IItem?[] Items,
        int[] Counts, int Revision);
}
