namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// A data transfer object containing the definition for a specific type of log.
    /// </summary>
    public record LogDto
    {
        /// <summary>
        /// The item ID of the log.
        /// </summary>
        public required int ItemID;

        /// <summary>
        /// The time in game ticks it takes for the tree to respawn after being cut down.
        /// </summary>
        public required double RespawnTime;

        /// <summary>
        /// The chance of the tree falling after successfully chopping a log.
        /// </summary>
        public required double FallChance;

        /// <summary>
        /// The base chance of successfully harvesting this log.
        /// </summary>
        public required double BaseHarvestChance;

        /// <summary>
        /// The required Woodcutting level to chop this log.
        /// </summary>
        public required int RequiredLevel;

        /// <summary>
        /// The Woodcutting experience gained for chopping this log.
        /// </summary>
        public required double WoodcuttingExperience;
    }
}