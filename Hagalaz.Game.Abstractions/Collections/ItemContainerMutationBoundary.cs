using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Coordinates mutations and publication for one owned item storage.</summary>
internal sealed class ItemContainerMutationBoundary : IItemContainerMutationBoundary, IItemContainerTransactionParticipantInternal
{
    private readonly ItemContainerStorage _storage;
    private readonly IItemContainerCompletionOwner? _completion;
    private readonly Action<HashSet<int>?>? _publishChanges;

    internal ItemContainerStorage Storage => _storage;
    IReadOnlyList<ItemContainerMutationBoundary> IItemContainerTransactionParticipantInternal.Boundaries => [this];

    internal void EnsureUnbound()
    {
        if (_storage.Transaction != null)
            throw new InvalidOperationException("This operation requires storage outside an active transaction.");
    }

    internal ItemContainerMutationBoundary(ItemContainerStorage storage, Action<HashSet<int>?>? publishChanges, IItemContainerCompletionOwner? completion = null)
    {
        ArgumentNullException.ThrowIfNull(storage);
        _storage = storage;
        _publishChanges = publishChanges;
        _completion = completion;
    }

    /// <summary>Transfers an exact quantity to another boundary and publishes both committed sides.</summary>
    public bool TryTransferTo(
        IItemContainerMutationBoundary destination,
        IItem item,
        int count,
        int preferredSourceSlot = -1,
        int destinationSlot = -1,
        IItem? destinationItem = null)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (destination is not ItemContainerMutationBoundary target)
            throw new ArgumentException("Unsupported mutation boundary.", nameof(destination));
        using var transaction = ItemContainerTransaction.BeginIfNeeded(this, target);
        if (!ItemContainerStorage.TryTransfer(_storage, target._storage, item, count,
                preferredSourceSlot, destinationSlot, destinationItem, out var sourceSlots, out var destinationSlots)) return false;
        NotifyChanges(sourceSlots);
        target.NotifyChanges(destinationSlots);
        transaction?.Commit();
        return true;
    }

    internal void NotifyChanges(HashSet<int>? slots)
    {
        if (_storage.Transaction is { } transaction) transaction.RecordChanges(_storage, slots);
        else PublishCommittedChanges(slots);
    }

    internal void PublishCommittedChanges(HashSet<int>? slots) => _publishChanges?.Invoke(slots);

    internal void DiscardDeferredCompletion(ItemContainerTransaction transaction) => _completion?.DiscardDeferredCompletion(transaction);
    internal void CompleteBeforePublication(ItemContainerTransaction transaction, int order) => _completion?.CompleteBeforePublication(transaction, order);
    internal void CompleteAfterPublication(ItemContainerTransaction transaction, int order) => _completion?.CompleteAfterPublication(transaction, order);
}
