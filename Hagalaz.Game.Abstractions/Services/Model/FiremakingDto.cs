namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// This structure represents a firemaking definition.
    /// </summary>
    public record FiremakingDto
    {
        /// <summary>
        /// The item npcID
        /// </summary>
        public required int ItemId;

        /// <summary>
        /// The required level
        /// </summary>
        public required int RequiredLevel;

        /// <summary>
        /// The fire object npcID
        /// </summary>
        public required int FireObjectId;

        /// <summary>
        /// The experience
        /// </summary>
        public required double Experience;

        /// <summary>
        /// The ticks.
        /// </summary>
        public required int Ticks;
    }
}