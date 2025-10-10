using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Audio
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating a sound effect where the effect's ID must be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IAudioBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ISoundId
    {
        /// <summary>
        /// Sets the unique identifier for the sound effect being built.
        /// </summary>
        /// <param name="id">The unique identifier for the sound effect.</param>
        /// <returns>The next step in the fluent builder chain, allowing for optional parameters to be set.</returns>
        ISoundOptional WithId(int id);
    }
}