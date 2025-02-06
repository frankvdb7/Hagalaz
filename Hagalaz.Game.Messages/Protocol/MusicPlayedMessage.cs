using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class MusicPlayedMessage : RaidoMessage
    {
        public required int MusicId { get; init; }
    }
}
