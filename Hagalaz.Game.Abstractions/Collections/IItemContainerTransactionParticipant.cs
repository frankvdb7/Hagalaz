using System.Collections.Generic;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Opaque participation marker. Obtain participants from container Mutations properties.</summary>
/// <remarks>External implementations are not supported. Transaction mechanics are internal to the owning containers.</remarks>
public interface IItemContainerTransactionParticipant { }

internal interface IItemContainerTransactionParticipantInternal : IItemContainerTransactionParticipant
{
    IReadOnlyList<ItemContainerMutationBoundary> Boundaries { get; }
}

// Fixed owner behavior only; no callback registration or public lifecycle SPI.
internal interface IItemContainerCompletionOwner
{
    void DiscardPendingCompletion(ItemContainerTransaction transaction);
    void CompleteBeforePublication(ItemContainerTransaction transaction, int order);
    void CompleteAfterPublication(ItemContainerTransaction transaction, int order);
}
