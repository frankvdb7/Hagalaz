namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// A data transfer object containing the definition of a hatchet.
    /// </summary>
    public record HatchetDto
    {
        /// <summary>
        /// The type of the hatchet.
        /// </summary>
        public required HatchetType Type;

        /// <summary>
        /// The item ID of the hatchet.
        /// </summary>
        public required int ItemId;

        /// <summary>
        /// The ID of the animation played when chopping with this hatchet.
        /// </summary>
        public required int ChopAnimationId;

        /// <summary>
        /// The ID of the animation played when making a canoe with this hatchet.
        /// </summary>
        public required int CanoeAnimationId;

        /// <summary>
        /// The required Woodcutting level to use this hatchet.
        /// </summary>
        public required int RequiredLevel;

        /// <summary>
        /// The base chance of successfully harvesting a log with this hatchet.
        /// </summary>
        public required double BaseHarvestChance;
    }
}