using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class SetCS2IntMessage : RaidoMessage
    {
        public required int Id { get; init; }
        public required int Value { get; init; }
    }
}
