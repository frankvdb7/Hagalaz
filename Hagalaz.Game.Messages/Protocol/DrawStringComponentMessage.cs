using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class DrawStringComponentMessage : RaidoMessage
    {
        public required int ComponentId { get; init; }
        public required int ChildId { get; init; }
        public required string Value { get; init; } = default!;
    }
}
