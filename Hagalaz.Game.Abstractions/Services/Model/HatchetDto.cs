namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// Holds information of hatchets.
    /// </summary>
    public record HatchetDto
    {
        /// <summary>
        /// The type of hatchet this is.
        /// </summary>
        public required HatchetType Type;

        /// <summary>
        /// The item npcID of the hatchet.
        /// </summary>
        public required int ItemId;

        /// <summary>
        /// The chopping animation of the hatchet.
        /// </summary>
        public required int ChopAnimationId;

        /// <summary>
        /// The canoueing animation of the hatchet.
        /// </summary>
        public required int CanoeAnimationId;

        /// <summary>
        /// Level requirement to use the hatchet.
        /// </summary>
        public required int RequiredLevel;

        /// <summary>
        /// The base cut chance.
        /// </summary>
        public required double BaseHarvestChance;
    }
}