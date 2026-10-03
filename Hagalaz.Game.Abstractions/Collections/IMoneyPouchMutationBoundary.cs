namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Provides explicit money-pouch mutations inside an already-owned item-container transaction.</summary>
public interface IMoneyPouchMutationBoundary : IItemContainerTransactionParticipant
{
    bool TryAddExact(int count);

    bool TryRemoveExact(int count);
}
