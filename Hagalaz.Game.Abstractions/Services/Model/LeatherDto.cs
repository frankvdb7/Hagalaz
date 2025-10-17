namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// A data transfer object containing the definition for crafting a leather item.
    /// </summary>
    public record LeatherDto
    {
        /// <summary>
        /// The item ID of the final crafted product.
        /// </summary>
        public required int ProductId;

        /// <summary>
        /// The item ID of the primary resource required (e.g., leather, dragonhide).
        /// </summary>
        public required int ResourceID;

        /// <summary>
        /// The amount of the primary resource required.
        /// </summary>
        public required int RequiredResourceCount;

        /// <summary>
        /// The required Crafting level to make this item.
        /// </summary>
        public required int RequiredLevel;

        /// <summary>
        /// The Crafting experience gained for making this item.
        /// </summary>
        public required double Experience;
    }
}