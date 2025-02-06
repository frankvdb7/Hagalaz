using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Common.Events.Character
{
    /// <summary>
    /// 
    /// </summary>
    public class MusicPlayEvent : CharacterEvent
    {
        /// <summary>
        /// Contains the index of the music.
        /// </summary>
        public int MusicIndex { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MusicPlayEvent"/> class.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <param name="musicIndex">Index of the music.</param>
        public MusicPlayEvent(ICharacter c, int musicIndex) : base(c) => MusicIndex = musicIndex;
    }
}