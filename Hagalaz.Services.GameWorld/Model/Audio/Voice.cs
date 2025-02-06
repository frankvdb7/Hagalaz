using Hagalaz.Game.Abstractions.Model.Sound;
using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Model.Audio
{
    public class Voice : ISound, IVoice
    {
        public int Id { get; set; }
        public int RepeatCount { get; set; }
        public int Delay { get; set; }
        public int Volume { get; set; }
        public int Distance => 8;
        public int PlaybackSpeed => 1;

        public RaidoMessage ToMessage() => new PlayVoiceMessage
        {
            Id = Id,
            RepeatCount = RepeatCount,
            Delay = Delay,
            Volume = Volume,
        };
    }
}