namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// A data transfer object containing the definition for crafting a silver item.
    /// </summary>
    public record SilverDto
    {
        /// <summary>
        /// The widget component ID for this item in the crafting interface.
        /// </summary>
        public required int ChildID;

        /// <summary>
        /// The item ID of the mould required to craft this item.
        /// </summary>
        public required int MouldID;

        /// <summary>
        /// The item ID of the final crafted product.
        /// </summary>
        public required int ProductID;

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