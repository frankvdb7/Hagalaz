using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol.Map
{
    public class SetTileSoundMessage : RaidoMessage
    {
        public int SoundId { get; init; }
        public int Distance { get; init; }
        public int RepeatCount { get; init; }
        public int Delay { get; init; }
        public int Volume { get; init; }
        public int PlaybackSpeed { get; init; }
        public bool IsVoiceAudio { get; init; }
        public int PartLocalX { get; init; }
        public int PartLocalY { get; init; }
    }
}
