namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// A data transfer object containing the definition of an item that can be enchanted.
    /// </summary>
    public record EnchantingSpellProductDto
    {
        /// <summary>
        /// Gets the item ID of the unenchanted item.
        /// </summary>
        public required int ResourceId { get; init; }

        /// <summary>
        /// Gets the widget button ID associated with enchanting this item.
        /// </summary>
        public required int ButtonId { get; init; }

        /// <summary>
        /// Gets the item ID of the enchanted item.
        /// </summary>
        public required int ProductId { get; init; }
    }
}