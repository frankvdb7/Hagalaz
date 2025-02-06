using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class SetVarpBitMessage : RaidoMessage
    {
        public required int Id { get; init; }
        public required int Value { get; init; }
    }
}
