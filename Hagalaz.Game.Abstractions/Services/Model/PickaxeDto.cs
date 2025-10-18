namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// A data transfer object containing the definition of a pickaxe.
    /// </summary>
    public record PickaxeDto
    {
        /// <summary>
        /// The type of the pickaxe.
        /// </summary>
        public required PickaxeType Type;

        /// <summary>
        /// The item ID of this pickaxe.
        /// </summary>
        public required int ItemId;

        /// <summary>
        /// The ID of the animation played when mining with this pickaxe.
        /// </summary>
        public required int AnimationId;

        /// <summary>
        /// The required Mining level to use this pickaxe.
        /// </summary>
        public required int RequiredLevel;

        /// <summary>
        /// The base chance of successfully mining an ore with this pickaxe.
        /// </summary>
        public required double BaseHarvestChance;
    }
}