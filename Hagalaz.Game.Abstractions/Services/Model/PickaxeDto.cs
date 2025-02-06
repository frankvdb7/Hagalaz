namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// Holds information about pickaxes.
    /// </summary>
    public record PickaxeDto
    {
        /// <summary>
        /// Type of pickaxe.
        /// </summary>
        public required PickaxeType Type;

        /// <summary>
        /// The item id of this pickaxe.
        /// </summary>
        public required int ItemId;

        /// <summary>
        /// The animation id for this pickaxe.
        /// </summary>
        public required int AnimationId;

        /// <summary>
        /// The level requirement for this pickaxe.
        /// </summary>
        public required int RequiredLevel;

        /// <summary>
        /// The base harvest chance
        /// </summary>
        public required double BaseHarvestChance;
    }
}