namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// A data transfer object containing the definition for a specific type of ore.
    /// </summary>
    public record OreDto
    {
        /// <summary>
        /// The item ID of the ore.
        /// </summary>
        public required int ItemId;

        /// <summary>
        /// The required Mining level to mine this ore.
        /// </summary>
        public required int RequiredLevel;

        /// <summary>
        /// The Mining experience gained for mining this ore.
        /// </summary>
        public required double Experience;

        /// <summary>
        /// The time in game ticks it takes for the rock to respawn after being depleted.
        /// </summary>
        public required double RespawnTime;

        /// <summary>
        /// The base chance of successfully mining this ore.
        /// </summary>
        public required double BaseHarvestChance;

        /// <summary>
        /// The chance of the rock becoming depleted after successfully mining an ore.
        /// </summary>
        public required double ExhaustChance;
    }
}