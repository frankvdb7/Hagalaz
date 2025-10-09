namespace Hagalaz.Game.Abstractions.Builders.Audio
{
    /// <summary>
    /// Defines the contract for an audio builder, serving as the main entry point
    /// for constructing various audio objects like sound effects and music tracks using a fluent API.
    /// </summary>
    public interface IAudioBuilder
    {
        /// <summary>
        /// Begins the process of building a new audio object.
        /// </summary>
        /// <returns>
        /// The next step in the fluent builder chain, which allows specifying the type of sound to create (e.g., sound effect, music).
        /// </returns>
        ISoundType Create();
    }
}