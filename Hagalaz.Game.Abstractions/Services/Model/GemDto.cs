namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// A data transfer object containing the definition for cutting a specific type of gem.
    /// </summary>
    public record GemDto
    {
        /// <summary>
        /// The item ID of the uncut gem.
        /// </summary>
        public required int UncutGemID;

        /// <summary>
        /// The item ID of the cut gem.
        /// </summary>
        public required int CutGemID;

        /// <summary>
        /// The ID of the animation played when cutting the gem.
        /// </summary>
        public required int AnimationID;

        /// <summary>
        /// The required Crafting level to cut this gem.
        /// </summary>
        public required int RequiredLevel;

        /// <summary>
        /// The Crafting experience gained for cutting this gem.
        /// </summary>
        public required double CraftingExperience;
    }
}