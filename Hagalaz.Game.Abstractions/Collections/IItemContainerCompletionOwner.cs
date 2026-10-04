namespace Hagalaz.Game.Abstractions.Collections;

// Fixed owner behavior only; no callback registration or public lifecycle SPI.
internal interface IItemContainerCompletionOwner
{
    void DiscardPendingCompletion(ItemContainerTransaction transaction);
    void CompleteBeforePublication(ItemContainerTransaction transaction, int order);
    void CompleteAfterPublication(ItemContainerTransaction transaction, int order);
}
