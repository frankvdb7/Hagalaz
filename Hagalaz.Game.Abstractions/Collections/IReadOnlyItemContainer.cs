using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Provides indexed and enumerable read access to items and item-specific queries.</summary>
public interface IReadOnlyItemContainer : IContainer<IItem?>
{
    int FreeSlots { get; }

    int TakenSlots { get; }

    IItem? GetById(int id);

    int GetCount(IItem item);

    int GetCountById(int id);

    int GetInstanceSlot(IItem instance);

    int GetSlotByItem(IItem item, bool ignoreCount = true);

    bool Contains(int id);

    bool Contains(int id, int count);

    bool HasSpaceFor(IItem item);

    bool HasSpaceForRange(IEnumerable<IItem?> items);

    /// <summary>
    /// Determines how many requested items can be transferred into this container.
    /// </summary>
    /// <param name="item">The item to transfer.</param>
    /// <param name="requestedCount">The requested item count.</param>
    /// <param name="transferCount">The count that can be transferred.</param>
    /// <param name="wasLimitedByFreeSlots">Whether free slots limited the request.</param>
    /// <returns><c>true</c> if at least one item can be transferred; otherwise, <c>false</c>.</returns>
    bool TryGetTransferableItemCount(IItem item, int requestedCount, out int transferCount,
        out bool wasLimitedByFreeSlots);
}
