using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Provides instance-based mutation coordination for one item-container storage.</summary>
public interface IItemContainerMutationBoundary : IItemContainerTransactionParticipant
{
    /// <summary>Attempts an exact transfer between storage boundaries already enlisted in one caller-owned transaction.</summary>
    /// <exception cref="InvalidOperationException">Both boundaries do not belong to the same active transaction.</exception>
    bool TryTransferTo(IItemContainerMutationBoundary destination, IItem item, int count,
        int preferredSourceSlot = -1, int destinationSlot = -1, IItem? destinationItem = null);
}
