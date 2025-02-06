using Hagalaz.Game.Abstractions.Model.Sound;
using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Model.Audio
{
    public class MusicEffect : ISound, IMusicEffect
    {
        public int Id { get; set; }
        public int Volume { get; set; }
        public int RepeatCount => 1;
        public int Distance => 8;
        public int Delay => 0;
        public int PlaybackSpeed => 1;

        public RaidoMessage ToMessage() => new PlayMusicEffectMessage
        {
            Id = Id,
            Volume = Volume,
        };
    }
}