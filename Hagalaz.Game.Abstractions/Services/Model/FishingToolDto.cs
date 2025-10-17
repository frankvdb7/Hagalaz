namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// A data transfer object containing the definition of a fishing tool.
    /// </summary>
    public record FishingToolDto
    {
        /// <summary>
        /// The item ID of the fishing tool (e.g., net, harpoon).
        /// </summary>
        public required int ItemId;

        /// <summary>
        /// The ID of the animation played while fishing with this tool.
        /// </summary>
        public required int FishAnimationId;

        /// <summary>
        /// The ID of the animation played when casting with this tool (if applicable).
        /// </summary>
        public required int CastAnimationId;
    }
}