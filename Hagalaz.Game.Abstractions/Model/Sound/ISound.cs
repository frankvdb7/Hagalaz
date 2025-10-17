using Raido.Common.Protocol;

namespace Hagalaz.Game.Abstractions.Model.Sound
{
    /// <summary>
    /// Defines the contract for a sound effect to be played by the client.
    /// </summary>
    public interface ISound
    {
        /// <summary>
        /// Gets the ID of the sound effect.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// Gets the distance from which the sound can be heard.
        /// </summary>
        int Distance { get; }

        /// <summary>
        /// Gets the delay in game ticks before the sound is played.
        /// </summary>
        int Delay { get; }

        /// <summary>
        /// Gets the volume of the sound.
        /// </summary>
        int Volume { get; }

        /// <summary>
        /// Gets the number of times the sound should repeat.
        /// </summary>
        int RepeatCount { get; }

        /// <summary>
        /// Gets the playback speed of the sound.
        /// </summary>
        int PlaybackSpeed { get; }

        /// <summary>
        /// Creates a network message for this sound effect.
        /// </summary>
        /// <returns>A <see cref="RaidoMessage"/> containing the sound data.</returns>
        RaidoMessage ToMessage();
    }
}