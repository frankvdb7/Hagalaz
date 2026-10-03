using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Provides instance-based mutation coordination for one item-container storage.</summary>
public interface IItemContainerMutationBoundary : IItemContainerTransactionParticipant
{
    /// <summary>Adds an item through the active transaction that enlisted this boundary.</summary>
    bool TryAdd(IItem item);

    /// <summary>Adds a range through the active transaction that enlisted this boundary.</summary>
    bool TryAddRange(IEnumerable<IItem?> items);

    /// <summary>Removes an exact item through the active transaction that enlisted this boundary.</summary>
    bool TryRemoveExact(IItem item, int preferredSlot = -1);

    /// <summary>Sorts this storage through the active transaction that enlisted this boundary.</summary>
    void Sort();

    /// <summary>Clears this storage through the active transaction that enlisted this boundary.</summary>
    void Clear();

    /// <summary>Ensures the storage is not part of an active item-container transaction.</summary>
    /// <exception cref="InvalidOperationException">The storage belongs to an active transaction.</exception>
    void EnsureOutsideTransaction();

    /// <summary>Attempts an exact transfer between storage boundaries already enlisted in one caller-owned transaction.</summary>
    /// <exception cref="InvalidOperationException">Both boundaries do not belong to the same active transaction.</exception>
    bool TryTransferTo(IItemContainerMutationBoundary destination, IItem item, int count,
        int preferredSourceSlot = -1, int destinationSlot = -1, IItem? destinationItem = null);
}
