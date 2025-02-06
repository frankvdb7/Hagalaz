using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class SetCS2StringMessage : RaidoMessage
    {
        public required int Id { get; init; }
        public required string Value { get; init; } = default!;
    }
}
