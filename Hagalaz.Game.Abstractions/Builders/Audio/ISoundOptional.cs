using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Audio
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating a sound effect where optional
    /// parameters like volume, repeat count, delay, and playback speed can be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IAudioBuilder"/>.
    /// It also inherits from <see cref="ISoundBuild"/>, allowing the build process to be finalized at this stage.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ISoundOptional : ISoundBuild
    {
        /// <summary>
        /// Sets the volume for the sound effect.
        /// </summary>
        /// <param name="volume">The volume level for the sound effect.</param>
        /// <returns>The same builder instance to allow for further optional configuration or to build the sound object.</returns>
        ISoundOptional WithVolume(int volume);

        /// <summary>
        /// Sets the number of times the sound effect should repeat.
        /// </summary>
        /// <param name="count">The number of times to repeat the sound effect.</param>
        /// <returns>The same builder instance to allow for further optional configuration or to build the sound object.</returns>
        ISoundOptional WithRepeatCount(int count);

        /// <summary>
        /// Sets the delay before the sound effect starts playing.
        /// </summary>
        /// <param name="delay">The delay in game ticks.</param>
        /// <returns>The same builder instance to allow for further optional configuration or to build the sound object.</returns>
        ISoundOptional WithDelay(int delay);

        /// <summary>
        /// Sets the playback speed for the sound effect.
        /// </summary>
        /// <param name="playbackSpeed">The playback speed modifier.</param>
        /// <returns>The same builder instance to allow for further optional configuration or to build the sound object.</returns>
        ISoundOptional WithPlaybackSpeed(int playbackSpeed);
    }
}