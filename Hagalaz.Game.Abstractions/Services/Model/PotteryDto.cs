namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// A data transfer object containing the definition for crafting a piece of pottery.
    /// </summary>
    public record PotteryDto
    {
        /// <summary>
        /// The item ID of the unfired pottery item.
        /// </summary>
        public required int FormedProductID;

        /// <summary>
        /// The item ID of the final, fired pottery item.
        /// </summary>
        public required int BakedProductID;

        /// <summary>
        /// The required Crafting level to make this item.
        /// </summary>
        public required int RequiredLevel;

        /// <summary>
        /// The Crafting experience gained for forming the unfired item.
        /// </summary>
        public required double FormExperience;

        /// <summary>
        /// The Crafting experience gained for firing the item in a kiln.
        /// </summary>
        public required double BakeExperience;
    }
}