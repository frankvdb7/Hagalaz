using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Audio
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating a music effect where optional
    /// parameters like volume can be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IAudioBuilder"/>.
    /// It also inherits from <see cref="ISoundBuild"/>, allowing the build process to be finalized at this stage.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IMusicEffectOptional : ISoundBuild
    {
        /// <summary>
        /// Sets the volume for the music effect.
        /// </summary>
        /// <param name="volume">The volume level for the music effect.</param>
        /// <returns>The same builder instance to allow for further optional configuration or to build the sound object.</returns>
        IMusicEffectOptional WithVolume(int volume);
    }
}