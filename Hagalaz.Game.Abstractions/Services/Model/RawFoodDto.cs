namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// A data transfer object containing the definition for cooking a raw food item.
    /// </summary>
    public record RawFoodDto
    {
        /// <summary>
        /// Gets the item ID of the raw food.
        /// </summary>
        public required int ItemId { get; init; }

        /// <summary>
        /// Gets the item ID of the successfully cooked food.
        /// </summary>
        public required int CookedItemId { get; init; }

        /// <summary>
        /// Gets the item ID of the burnt food.
        /// </summary>
        public required int BurntItemId { get; init; }

        /// <summary>
        /// Gets the required Cooking level to cook this food.
        /// </summary>
        public required int RequiredLevel { get; init; }

        /// <summary>
        /// Gets the Cooking level at which this food no longer burns.
        /// </summary>
        public required int StopBurningLevel { get; init; }

        /// <summary>
        /// Gets the Cooking experience gained for successfully cooking this food.
        /// </summary>
        public required double Experience { get; init; }
    }
}