using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// This structure represents a seed definition.
    /// </summary>
    public record SeedDto
    {
        /// <summary>
        /// The item identifier
        /// </summary>
        public required int ItemID;

        /// <summary>
        /// The product identifier
        /// </summary>
        public required int ProductID;

        /// <summary>
        /// The minimum product count
        /// </summary>
        public required int MinimumProductCount;

        /// <summary>
        /// The maximum product count
        /// </summary>
        public required int MaximumProductCount;

        /// <summary>
        /// The required level
        /// </summary>
        public required int RequiredLevel;

        /// <summary>
        /// The planting experience
        /// </summary>
        public required double PlantingExperience;

        /// <summary>
        /// The harvest experience
        /// </summary>
        public required double HarvestExperience;

        /// <summary>
        /// The varp bit configuration index
        /// </summary>
        public required int VarpBitIndex;

        /// <summary>
        /// The maximum cycles.
        /// </summary>
        public required int MaxCycles;

        /// <summary>
        /// The ticks.
        /// </summary>
        public required int CycleTicks;

        /// <summary>
        /// The patch type.
        /// </summary>
        public required PatchType Type;
    }
}