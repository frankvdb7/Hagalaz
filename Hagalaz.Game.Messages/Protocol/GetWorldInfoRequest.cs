using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class GetWorldInfoRequest : RaidoMessage
    {
        public required int Checksum { get; init; }
    }
}
