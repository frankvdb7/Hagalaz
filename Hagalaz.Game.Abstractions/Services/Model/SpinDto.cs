namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// 
    /// </summary>
    public record SpinDto
    {
        /// <summary>
        /// The resource Id
        /// </summary>
        public required int ResourceID;

        /// <summary>
        /// The product Id
        /// </summary>
        public required int ProductID;

        /// <summary>
        /// The required crafting level.
        /// </summary>
        public required int RequiredLevel;

        /// <summary>
        /// The crafting experience.
        /// </summary>
        public required double CraftingExperience;
    }
}