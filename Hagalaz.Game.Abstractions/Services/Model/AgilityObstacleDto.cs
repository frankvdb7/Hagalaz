namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// A data transfer object containing the persisted definition of an Agility obstacle.
    /// </summary>
    public record AgilityObstacleDto
    {
        /// <summary>
        /// The game object id of the obstacle.
        /// </summary>
        public required int ObjectId;

        /// <summary>
        /// The Agility experience gained for completing the obstacle.
        /// </summary>
        public required double Experience;

        /// <summary>
        /// The Agility level required to attempt the obstacle.
        /// </summary>
        public required int RequiredLevel;
    }
}
