using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// A data transfer object containing the definition of a seed for the Farming skill.
    /// </summary>
    public record SeedDto
    {
        /// <summary>
        /// The item ID of the seed.
        /// </summary>
        public required int ItemID;

        /// <summary>
        /// The item ID of the harvested product.
        /// </summary>
        public required int ProductID;

        /// <summary>
        /// The minimum number of products yielded per harvest.
        /// </summary>
        public required int MinimumProductCount;

        /// <summary>
        /// The maximum number of products yielded per harvest.
        /// </summary>
        public required int MaximumProductCount;

        /// <summary>
        /// The required Farming level to plant this seed.
        /// </summary>
        public required int RequiredLevel;

        /// <summary>
        /// The Farming experience gained for planting this seed.
        /// </summary>
        public required double PlantingExperience;

        /// <summary>
        /// The Farming experience gained for harvesting the final product.
        /// </summary>
        public required double HarvestExperience;

        /// <summary>
        /// The client variable bit index used to track the state of this plant type.
        /// </summary>
        public required int VarpBitIndex;

        /// <summary>
        /// The total number of growth cycles for this plant.
        /// </summary>
        public required int MaxCycles;

        /// <summary>
        /// The number of game ticks per growth cycle.
        /// </summary>
        public required int CycleTicks;

        /// <summary>
        /// The type of patch this seed can be planted in.
        /// </summary>
        public required PatchType Type;
    }
}