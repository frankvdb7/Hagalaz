namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// A data transfer object containing the definition of a food item.
    /// </summary>
    public record FoodDto
    {
        /// <summary>
        /// Gets the item ID of the food.
        /// </summary>
        public required int ItemId { get; init; }

        /// <summary>
        /// Gets the amount of health this food restores.
        /// </summary>
        public required int HealAmount { get; init; }

        /// <summary>
        /// Gets the item ID of what is left after eating (e.g., a pie dish), or -1 if nothing is left.
        /// </summary>
        public required int LeftItemId { get; init; }

        /// <summary>
        /// Gets the time in game ticks it takes to eat this food.
        /// </summary>
        public required int EatingTime { get; init; }
    }
}