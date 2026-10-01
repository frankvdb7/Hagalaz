using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Coordinates mutations and publication for one owned item storage.</summary>
internal sealed class ItemContainerMutationBoundary : IItemContainerMutationBoundary
{
    private readonly ItemContainerStorage _storage;
    private readonly Action<HashSet<int>?>? _publishChanges;

    internal ItemContainerMutationBoundary(ItemContainerStorage storage, Action<HashSet<int>?>? publishChanges)
    {
        ArgumentNullException.ThrowIfNull(storage);
        _storage = storage;
        _publishChanges = publishChanges;
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
        var transaction = new ItemContainerTransaction(this, destination);
        return transaction.TryExecute(tx => tx.TryTransfer(this, destination, item, count,
            preferredSourceSlot, destinationSlot, destinationItem));
    }

    void IItemContainerMutationBoundary.Enlist(ItemContainerTransaction transaction) =>
        transaction.RegisterParticipant(this, _storage, _publishChanges);

}
