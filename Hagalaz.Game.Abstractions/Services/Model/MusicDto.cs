namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// A data transfer object containing the definition of a music track.
    /// </summary>
    public record MusicDto
    {
        /// <summary>
        /// Gets the unique identifier for the music track.
        /// </summary>
        public int Id { get; init; }

        /// <summary>
        /// Gets the name of the music track.
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// Gets the hint for how to unlock the music track.
        /// </summary>
        public required string Hint { get; init; }
    }
}