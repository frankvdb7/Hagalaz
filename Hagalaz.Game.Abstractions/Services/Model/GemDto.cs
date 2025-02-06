namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// 
    /// </summary>
    public record GemDto
    {
        /// <summary>
        /// The uncut gem id.
        /// </summary>
        public required int UncutGemID;

        /// <summary>
        /// The cut gem id.
        /// </summary>
        public required int CutGemID;

        /// <summary>
        /// The animation Id.
        /// </summary>
        public required int AnimationID;

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