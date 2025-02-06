using Hagalaz.Game.Abstractions.Model.Sound;
using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Model.Audio
{
    /// <summary>
    /// Represents a single sound info.
    /// </summary>
    public class Sound : ISound
    {
        /// <summary>
        /// Contains sound Id.
        /// </summary>
        /// <value>The id.</value>
        public int Id { get; set; }

        /// <summary>
        /// Contains sound volume.
        /// </summary>
        /// <value>The volume.</value>
        public int Volume { get; set; }

        /// <summary>
        /// Contains sound delay.
        /// </summary>
        /// <value>The delay.</value>
        public int Delay { get; set; }

        /// <summary>
        /// Contains sound speed.
        /// </summary>
        /// <value>The speed.</value>
        public int PlaybackSpeed { get; set; }

        /// <summary>
        /// Contains amount of times this sound needs to
        /// be repeated.
        /// </summary>
        /// <value>The times to play.</value>
        public int RepeatCount { get; set; }

        public int Distance => 8;

        public RaidoMessage ToMessage() => new PlaySoundMessage
        {
            Id = Id,
            Volume = Volume,
            Delay = Delay,
            PlaybackSpeed = PlaybackSpeed,
            RepeatCount = RepeatCount
        };
    }
}