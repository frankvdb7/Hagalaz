namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// 
    /// </summary>
    public record OreDto
    {
        /// <summary>
        /// The ore identifier.
        /// </summary>
        public required int ItemId;

        /// <summary>
        /// The level requirement.
        /// </summary>
        public required int RequiredLevel;

        /// <summary>
        /// The experience.
        /// </summary>
        public required double Experience;

        /// <summary>
        /// The respawn time.
        /// </summary>
        public required double RespawnTime;

        /// <summary>
        /// The base harvest chance
        /// </summary>
        public required double BaseHarvestChance;

        /// <summary>
        /// The exhaust chance
        /// </summary>
        public required double ExhaustChance;
    }
}