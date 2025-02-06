namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// 
    /// </summary>
    public record LogDto
    {
        /// <summary>
        /// Gets the item identifier.
        /// </summary>
        public required int ItemID;

        /// <summary>
        /// The respawn time.
        /// </summary>
        public required double RespawnTime;

        /// <summary>
        /// The fall chance.
        /// </summary>
        public required double FallChance;

        /// <summary>
        /// The base cut chance
        /// </summary>
        public required double BaseHarvestChance;

        /// <summary>
        /// The level requirement.
        /// </summary>
        public required int RequiredLevel;

        /// <summary>
        /// The experience.
        /// </summary>
        public required double WoodcuttingExperience;
    }
}