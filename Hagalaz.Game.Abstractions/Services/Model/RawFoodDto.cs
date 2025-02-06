namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// 
    /// </summary>
    public record RawFoodDto
    {
        /// <summary>
        /// The raw food id
        /// </summary>
        public required int ItemId { get; init; }

        /// <summary>
        /// The cooked food id
        /// </summary>
        public required int CookedItemId { get; init; }

        /// <summary>
        /// The burnt food id
        /// </summary>
        public required int BurntItemId { get; init; }

        /// <summary>
        /// The required cooking level
        /// </summary>
        public required int RequiredLevel { get; init; }

        /// <summary>
        /// The stop burning level.
        /// </summary>
        public required int StopBurningLevel { get; init; }

        /// <summary>
        /// The cooking experience
        /// </summary>
        public required double Experience { get; init; }
    }
}