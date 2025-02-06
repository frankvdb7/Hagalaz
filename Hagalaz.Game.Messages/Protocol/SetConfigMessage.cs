using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class SetConfigMessage : RaidoMessage
    {
        public required int Id { get; init; }
        public required int Value { get; init; }
    }
}
