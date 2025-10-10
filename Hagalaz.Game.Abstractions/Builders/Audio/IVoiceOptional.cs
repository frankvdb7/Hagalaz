using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Audio
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating a voice sound where optional
    /// parameters like volume, repeat count, and delay can be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IAudioBuilder"/>.
    /// It also inherits from <see cref="ISoundBuild"/>, allowing the build process to be finalized at this stage.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IVoiceOptional : ISoundBuild
    {
        /// <summary>
        /// Sets the volume for the voice sound.
        /// </summary>
        /// <param name="volume">The volume level for the voice sound.</param>
        /// <returns>The same builder instance to allow for further optional configuration or to build the sound object.</returns>
        IVoiceOptional WithVolume(int volume);

        /// <summary>
        /// Sets the number of times the voice sound should repeat.
        /// </summary>
        /// <param name="repeat">The number of times to repeat the voice sound.</param>
        /// <returns>The same builder instance to allow for further optional configuration or to build the sound object.</returns>
        IVoiceOptional WithRepeatCount(int repeat);

        /// <summary>
        /// Sets the delay before the voice sound starts playing.
        /// </summary>
        /// <param name="delay">The delay in game ticks.</param>
        /// <returns>The same builder instance to allow for further optional configuration or to build the sound object.</returns>
        IVoiceOptional WithDelay(int delay);
    }
}