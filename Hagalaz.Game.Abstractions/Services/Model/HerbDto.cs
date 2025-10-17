namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// A data transfer object containing the definition of a herb.
    /// </summary>
    public record HerbDto
    {
        /// <summary>
        /// The item ID of the grimy herb.
        /// </summary>
        public required int GrimyHerbId;

        /// <summary>
        /// The item ID of the clean herb.
        /// </summary>
        public required int CleanHerbId;

        /// <summary>
        /// The required Herblore level to clean this herb.
        /// </summary>
        public required int CleanLevel;

        /// <summary>
        /// The Herblore experience gained for cleaning this herb.
        /// </summary>
        public required double CleanExperience;
    }
}