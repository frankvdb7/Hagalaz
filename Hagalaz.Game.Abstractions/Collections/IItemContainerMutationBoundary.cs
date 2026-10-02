using System;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Provides instance-based mutation coordination for one item-container storage.</summary>
public interface IItemContainerMutationBoundary
{
    bool TryTransferTo(IItemContainerMutationBoundary destination, IItem item, int count,
        int preferredSourceSlot = -1, int destinationSlot = -1, IItem? destinationItem = null);

    // Internal bridge: implementations enlist their owned storage and publisher with the transaction.
    internal void Enlist(ItemContainerTransaction transaction);
}
