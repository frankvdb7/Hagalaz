namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// A data transfer object containing the definition for spinning an item on a spinning wheel.
    /// </summary>
    public record SpinDto
    {
        /// <summary>
        /// The item ID of the resource to be spun (e.g., wool, flax).
        /// </summary>
        public required int ResourceID;

        /// <summary>
        /// The item ID of the final spun product (e.g., ball of wool, bow string).
        /// </summary>
        public required int ProductID;

        /// <summary>
        /// The required Crafting level to spin this item.
        /// </summary>
        public required int RequiredLevel;

        /// <summary>
        /// The Crafting experience gained for spinning this item.
        /// </summary>
        public required double CraftingExperience;
    }
}