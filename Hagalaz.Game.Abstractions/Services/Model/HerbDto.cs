namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// 
    /// </summary>
    public record HerbDto
    {
        /// <summary>
        /// The grimy herb id.
        /// </summary>
        public required int GrimyHerbId;

        /// <summary>
        /// The clean herb id.
        /// </summary>
        public required int CleanHerbId;

        /// <summary>
        /// The level required to clean this herb.
        /// </summary>
        public required int CleanLevel;

        /// <summary>
        /// The experience rewarded when the grimy herb is cleaned. 
        /// </summary>
        public required double CleanExperience;
    }
}