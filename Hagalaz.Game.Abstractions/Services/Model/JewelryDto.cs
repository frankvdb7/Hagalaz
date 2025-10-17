namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// A data transfer object containing the definition for crafting a piece of jewelry.
    /// </summary>
    public partial class JewelryDto
    {
        /// <summary>
        /// The widget component ID for this jewelry item in the crafting interface.
        /// </summary>
        public int ChildID;

        /// <summary>
        /// The type of jewelry (e.g., ring, necklace).
        /// </summary>
        public JewelryType Type;

        /// <summary>
        /// The item ID of the primary resource required (e.g., a cut gem).
        /// </summary>
        public int ResourceID;

        /// <summary>
        /// The item ID of the final crafted product.
        /// </summary>
        public int ProductID;

        /// <summary>
        /// The required Crafting level to make this item.
        /// </summary>
        public int RequiredLevel;

        /// <summary>
        /// The Crafting experience gained for making this item.
        /// </summary>
        public double Experience;
    }
}