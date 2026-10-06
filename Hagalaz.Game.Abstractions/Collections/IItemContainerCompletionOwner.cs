namespace Hagalaz.Game.Abstractions.Collections;

// Fixed owner behavior only; no callback registration or public lifecycle SPI.
internal interface IItemContainerCompletionOwner
{
    void DiscardPendingCompletion();
    void CompleteBeforePublication();
    void CompleteAfterPublication();
}
