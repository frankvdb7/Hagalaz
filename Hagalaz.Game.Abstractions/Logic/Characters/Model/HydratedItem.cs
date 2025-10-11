namespace Hagalaz.Game.Abstractions.Logic.Characters.Model
{
    /// <summary>
    /// Represents the active, in-memory state of an item within a character's container.
    /// "Hydrated" refers to this being a live game object state, as opposed to a dehydrated, persisted DTO.
    /// </summary>
    /// <param name="ItemId">The unique identifier of the item type.</param>
    /// <param name="Count">The number of items in the stack.</param>
    /// <param name="SlotId">The slot index where the item is located within its container.</param>
    /// <param name="ExtraData">Optional string data associated with the item, for custom properties or states.</param>
    public record HydratedItem(int ItemId, int Count, int SlotId, string? ExtraData);
}
