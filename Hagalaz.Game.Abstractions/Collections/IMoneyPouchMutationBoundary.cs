namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Provides coin-pouch participation in an item-container transaction.</summary>
public interface IMoneyPouchMutationBoundary
{
    void EnlistIn(IItemContainerTransaction transaction);

    bool TryStageAddExact(IItemContainerTransaction transaction, int count);

    bool TryStageRemoveExact(IItemContainerTransaction transaction, int count);
}
