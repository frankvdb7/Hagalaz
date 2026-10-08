using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Describes the operations and shape of an item container.</summary>
public interface IItemContainer : IReadOnlyItemContainer, IItemTransactional
{
    StorageType Type { get; }

    bool Add(IItem item);

    bool Add(int slot, IItem item);

    int Remove(IItem item, int preferredSlot = -1, bool publishChanges = true);

    bool TryRemoveExact(IItem item, int preferredSlot = -1);

    /// <summary>
    /// Atomically transfers an exact quantity to another item container. If neither storage is already held by the current
    /// thread's item transaction, this operation owns and commits a short transaction over both containers. If both storages
    /// belong to the same active current-thread transaction, this operation participates and leaves commit or rollback to its caller.
    /// Partial or conflicting transaction participation is rejected.
    /// </summary>
    /// <exception cref="ArgumentNullException">The destination or item is null.</exception>
    /// <exception cref="ArgumentException">The destination is not a supported transaction source or does not contribute exactly one storage boundary.</exception>
    /// <exception cref="InvalidOperationException">The storages are only partially enlisted or belong to different transactions.</exception>
    /// <returns><see langword="true"/> when the transfer succeeds; otherwise, no transfer is performed.</returns>
    bool TryTransferTo(IItemContainer destination, IItem item, int count,
        int preferredSourceSlot = -1, int destinationSlot = -1, IItem? destinationItem = null);

    void Replace(int slot, IItem item);

    void Swap(int fromSlot, int toSlot);

    void Move(int fromSlot, int toSlot);

    bool AddRange(IEnumerable<IItem?> items);

    void Sort();

    void Clear(bool publishChanges);
}
