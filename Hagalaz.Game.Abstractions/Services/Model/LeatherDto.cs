namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// 
    /// </summary>
    public record LeatherDto
    {
        /// <summary>
        /// The product identifier.
        /// </summary>
        public required int ProductId;

        /// <summary>
        /// The leather identifier.
        /// </summary>
        public required int ResourceID;

        /// <summary>
        /// The required resource count.
        /// </summary>
        public required int RequiredResourceCount;

        /// <summary>
        /// The required level.
        /// </summary>
        public required int RequiredLevel;

        /// <summary>
        /// The experience.
        /// </summary>
        public required double Experience;
    }
}