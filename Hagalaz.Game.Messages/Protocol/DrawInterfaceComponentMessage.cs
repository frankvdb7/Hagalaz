using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class DrawInterfaceComponentMessage : RaidoMessage
    {
        public required int Id { get; init; }
        public required int ParentId { get; init; }
        public required int ParentSlot { get; init; }
        public required int Transparency { get; init; }
    }
}
