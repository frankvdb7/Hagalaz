namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Provides coin-pouch participation in an item-container transaction.</summary>
public interface IMoneyPouchMutationBoundary
{
    void EnlistIn(IItemContainerTransaction transaction);

    MoneyPouchChange? StageAddExact(IItemContainerTransaction transaction, int count);

    MoneyPouchChange? StageRemoveExact(IItemContainerTransaction transaction, int count);

    internal void PublishChange(int previousCount, int changeCount);
}
