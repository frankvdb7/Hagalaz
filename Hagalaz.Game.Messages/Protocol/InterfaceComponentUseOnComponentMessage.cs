using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class InterfaceComponentUseOnComponentMessage : RaidoMessage
    {
        public required int InterfaceId { get; init; }
        public required int ComponentId { get; init; }
        public required int ExtraData1 { get; init; }
        public required int ExtraData2 { get; init; }

        public required int OnInterfaceId { get; init; }
        public required int OnComponentId { get; init; }
        public required int OnExtraData1 { get; init; }
        public required int OnExtraData2 { get; init; }
    }
}
