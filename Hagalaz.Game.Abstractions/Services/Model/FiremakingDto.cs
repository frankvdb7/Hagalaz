namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// A data transfer object containing the definition for lighting a specific type of log.
    /// </summary>
    public record FiremakingDto
    {
        /// <summary>
        /// The item ID of the logs.
        /// </summary>
        public required int ItemId;

        /// <summary>
        /// The required Firemaking level to light these logs.
        /// </summary>
        public required int RequiredLevel;

        /// <summary>
        /// The object ID of the fire that is created.
        /// </summary>
        public required int FireObjectId;

        /// <summary>
        /// The Firemaking experience gained for lighting these logs.
        /// </summary>
        public required double Experience;

        /// <summary>
        /// The number of game ticks the fire will last.
        /// </summary>
        public required int Ticks;
    }
}