using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class RemoveInterfaceComponentMessage : RaidoMessage
    {
        public required int ParentId { get; init; }
        public required int ParentSlot { get; init; }
    }
}
