using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Coordinates one exact transfer between two storage owners.</summary>
internal static class ItemContainerTransfer
{
    internal static bool TryTransferTo(ItemContainerSynchronization source,
        ItemContainerSynchronization destination, IItem item, int count, int preferredSourceSlot = -1,
        int destinationSlot = -1, IItem? destinationItem = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(item);

        if (ReferenceEquals(source, destination) || count <= 0 || destinationSlot < -1) return false;
        var transaction = source.GetSharedCurrentThreadTransaction(destination);
        return transaction != null
            ? TryTransferInTransaction(source, destination, transaction, item, count, preferredSourceSlot,
                destinationSlot, destinationItem)
            : TryTransferStandalone(source, destination, item, count, preferredSourceSlot, destinationSlot,
                destinationItem);
    }

    private static bool TryTransferInTransaction(ItemContainerSynchronization source,
        ItemContainerSynchronization destination, ItemContainerTransaction transaction, IItem item, int count,
        int preferredSourceSlot, int destinationSlot, IItem? destinationItem)
    {
        source.EnsureOwnedBy(transaction);
        destination.EnsureOwnedBy(transaction);
        if (!source.Storage.TryTransferTo(destination.Storage, item, count, preferredSourceSlot, destinationSlot,
                destinationItem, out var sourceSlots, out var destinationSlots))
            return false;

        source.RecordChanges(transaction, sourceSlots);
        destination.RecordChanges(transaction, destinationSlots);
        return true;
    }

    private static bool TryTransferStandalone(ItemContainerSynchronization source,
        ItemContainerSynchronization destination, IItem item, int count, int preferredSourceSlot,
        int destinationSlot, IItem? destinationItem)
    {
        source.ThrowIfOwnedByCurrentThread();
        destination.ThrowIfOwnedByCurrentThread();
        if (source.IsSynchronizationLockHeldByCurrentThread || destination.IsSynchronizationLockHeldByCurrentThread)
            throw new InvalidOperationException("A standalone transfer cannot borrow a mutation lock.");

        var synchronizations = new[] { source, destination };
        ItemContainerLockSet lockSet;
        if (ItemContainerSynchronization.CurrentThreadOwnsStorage)
        {
            if (!ItemContainerLockSet.TryAcquire(synchronizations, out var acquiredLockSet)) return false;
            lockSet = acquiredLockSet!;
        }
        else
        {
            lockSet = ItemContainerLockSet.Acquire(synchronizations);
        }

        using (lockSet)
        {
            ItemContainerSynchronization.AddCurrentThreadStorageOwnership();
            var retainedOwnership = false;
            try
            {
                if (!source.Storage.TryTransferTo(destination.Storage, item, count, preferredSourceSlot,
                        destinationSlot, destinationItem, out var sourceSlots, out var destinationSlots))
                    return false;

                source.ChangeTracker.RecordCommitted(sourceSlots);
                destination.ChangeTracker.RecordCommitted(destinationSlots);

                var owners = GetCompletionOwners(source, destination);
                if (owners.Count == 0) return true;

                source.ClaimStandalonePublicationOwnership();
                destination.ClaimStandalonePublicationOwnership();
                retainedOwnership = true;
                lockSet.ReleaseLocks();

                foreach (var owner in owners) owner.CompleteCommittedEffects();
                foreach (var owner in owners) owner.PublishCommittedChanges();

                return true;
            }
            finally
            {
                try
                {
                    if (retainedOwnership) CompleteStandalonePublications(synchronizations);
                }
                finally
                {
                    ItemContainerSynchronization.RemoveCurrentThreadStorageOwnership();
                }
            }
        }
    }

    private static List<IItemContainerCompletionOwner> GetCompletionOwners(ItemContainerSynchronization source,
        ItemContainerSynchronization destination)
    {
        var owners = new List<IItemContainerCompletionOwner>(2);
        if (source.CompletionOwner is { } sourceOwner) owners.Add(sourceOwner);
        if (destination.CompletionOwner is { } destinationOwner &&
            (owners.Count == 0 || !ReferenceEquals(owners[0], destinationOwner)))
            owners.Add(destinationOwner);
        return owners;
    }

    private static void CompleteStandalonePublications(
        IReadOnlyCollection<ItemContainerSynchronization> synchronizations)
    {
        using var lockSet = ItemContainerLockSet.AcquireForCurrentOwners(synchronizations);
        foreach (var synchronization in synchronizations)
        {
            synchronization.EnsureStandalonePublicationOwnedByCurrentThread();
            if (synchronization.CompletionOwner != null)
                synchronization.ChangeTracker.DiscardCommitted();
            synchronization.ReleaseStandalonePublicationOwnership();
        }
    }
}
