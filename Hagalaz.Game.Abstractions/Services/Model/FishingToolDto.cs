namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// 
    /// </summary>
    public record FishingToolDto
    {
        /// <summary>
        /// The item identifier.
        /// </summary>
        public required int ItemId;

        /// <summary>
        /// The fish animation identifier.
        /// </summary>
        public required int FishAnimationId;

        /// <summary>
        /// The cast animation identifier.
        /// </summary>
        public required int CastAnimationId;
    }
}