using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Stages one synchronous, rollback-capable mutation across item-container boundaries.</summary>
public interface IItemContainerTransaction
{
    void Include(IItemContainerMutationBoundary boundary);
    bool TryAddRange(IItemContainerMutationBoundary boundary, IEnumerable<IItem?> items);
    bool TryRemoveExact(IItemContainerMutationBoundary boundary, IItem item, int preferredSlot = -1);
    bool TryTransfer(IItemContainerMutationBoundary source, IItemContainerMutationBoundary destination, IItem item,
        int count, int preferredSourceSlot = -1, int destinationSlot = -1, IItem? destinationItem = null);
    bool Clear(IItemContainerMutationBoundary boundary);
}
