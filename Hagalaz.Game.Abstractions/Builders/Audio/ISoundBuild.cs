using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model.Sound;

namespace Hagalaz.Game.Abstractions.Builders.Audio
{
    /// <summary>
    /// Represents the final step in the fluent builder pattern for creating a sound object.
    /// This interface provides the method to construct the final <see cref="ISound"/> object.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IAudioBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ISoundBuild
    {
        /// <summary>
        /// Builds and returns the configured <see cref="ISound"/> instance.
        /// </summary>
        /// <returns>A new <see cref="ISound"/> object configured with the specified properties.</returns>
        ISound Build();
    }
}