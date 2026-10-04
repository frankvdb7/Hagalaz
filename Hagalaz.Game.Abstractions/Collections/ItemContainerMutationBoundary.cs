using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Coordinates mutations and publication for one owned item storage.</summary>
internal sealed class ItemContainerMutationBoundary : IItemContainerTransactionParticipantInternal
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
    IReadOnlyList<ItemContainerMutationBoundary> IItemContainerTransactionParticipantInternal.Boundaries => [this];

    internal ItemContainerMutationBoundary(ItemContainerStorage storage, Action<HashSet<int>?>? publishChanges, IItemContainerCompletionOwner? completion = null)
    {
        ArgumentNullException.ThrowIfNull(storage);
        _storage = storage;
        _publishChanges = publishChanges;
        _completion = completion;
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
        NotifyChanges(sourceSlots);
        destination.NotifyChanges(destinationSlots);
        return true;
    }

    internal void NotifyChanges(HashSet<int>? slots)
    {
        if (_storage.Transaction is { } transaction) transaction.RecordChanges(_storage, slots);
        else PublishCommittedChanges(slots);
    }

    internal void PublishCommittedChanges(HashSet<int>? slots) => _publishChanges?.Invoke(slots);

    internal void DiscardPendingCompletion(ItemContainerTransaction transaction) => _completion?.DiscardPendingCompletion(transaction);
    internal void CompleteBeforePublication(ItemContainerTransaction transaction, int order) => _completion?.CompleteBeforePublication(transaction, order);
    internal void CompleteAfterPublication(ItemContainerTransaction transaction, int order) => _completion?.CompleteAfterPublication(transaction, order);
}
