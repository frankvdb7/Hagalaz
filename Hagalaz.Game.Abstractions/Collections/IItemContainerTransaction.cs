using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Stages one synchronous, rollback-capable mutation across item-container boundaries.</summary>
public interface IItemContainerTransaction
{
    void Include(IItemContainerMutationBoundary boundary);
    bool TryExecute(Func<IItemContainerTransaction, bool> operation);
    bool TryAddRange(IItemContainerMutationBoundary boundary, IEnumerable<IItem?> items);
    bool TryAddRange(IItemContainerMutationBoundary boundary, IEnumerable<IItem?> items, out HashSet<int> changedSlots);
    bool TryRemoveExact(IItemContainerMutationBoundary boundary, IItem item, int preferredSlot = -1);
    bool TryRemoveExact(IItemContainerMutationBoundary boundary, IItem item, int preferredSlot, out HashSet<int> changedSlots);
    bool TryTransfer(IItemContainerMutationBoundary source, IItemContainerMutationBoundary destination, IItem item,
        int count, int preferredSourceSlot = -1, int destinationSlot = -1, IItem? destinationItem = null);
    void TransferAll(IItemContainerMutationBoundary source, IItemContainerMutationBoundary destination);
    bool Clear(IItemContainerMutationBoundary boundary);
    void RecordChangedSlots(IItemContainerMutationBoundary boundary, IEnumerable<int> slots);
    void RecordFullChange(IItemContainerMutationBoundary boundary);
    void OnCommitted(Action action);
}
