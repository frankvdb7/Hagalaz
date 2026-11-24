namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// A data transfer object containing the persisted definition of an Agility course.
    /// </summary>
    public record AgilityCourseDto
    {
        /// <summary>
        /// The course identity.
        /// </summary>
        public required AgilityCourseId Id;

        /// <summary>
        /// The Agility experience gained for completing the whole course.
        /// </summary>
        public required double CompletionExperience;
    }
}
