namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// 
    /// </summary>
    public record FoodDto
    {
        /// <summary>
        /// The item npcID
        /// </summary>
        public required int ItemId { get; init; }

        /// <summary>
        /// The heal amount
        /// </summary>
        public required int HealAmount { get; init; }

        /// <summary>
        /// The left item npcID
        /// </summary>
        public required int LeftItemId { get; init; }

        /// <summary>
        /// The eating time
        /// </summary>
        public required int EatingTime { get; init; }
    }
}