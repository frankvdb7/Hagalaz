using Raido.Common.Protocol;

namespace Hagalaz.Game.Abstractions.Model.Sound
{
    /// <summary>
    /// 
    /// </summary>
    public interface ISound
    {
        /// <summary>
        /// The id of the sound
        /// </summary>
        int Id { get; }
        /// <summary>
        /// The distance of the sound
        /// </summary>
        int Distance { get; }
        /// <summary>
        /// The delay of the sound
        /// </summary>
        int Delay { get; }
        /// <summary>
        /// The volume of the sound
        /// </summary>
        int Volume { get; }
        /// <summary>
        /// The repeat count of the sound
        /// </summary>
        int RepeatCount { get; }
        /// <summary>
        /// The playback speed of the sound
        /// </summary>
        int PlaybackSpeed { get; }
        /// <summary>
        /// Creates a message for this sound
        /// </summary>
        /// <returns></returns>
        RaidoMessage ToMessage();
    }
}
