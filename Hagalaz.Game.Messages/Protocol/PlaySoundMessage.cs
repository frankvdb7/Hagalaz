using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class PlaySoundMessage : RaidoMessage
    {
        public required int Id { get; init; }
        public required int Delay { get; init; }
        public required int Volume { get; init; }
        public required int RepeatCount { get; init; }
        public required int PlaybackSpeed { get; init; }
    }
}
