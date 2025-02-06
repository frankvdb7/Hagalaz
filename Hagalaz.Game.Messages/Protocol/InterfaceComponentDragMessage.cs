using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class InterfaceComponentDragMessage : RaidoMessage
    {
        public required int FromId { get; init; }
        public required int FromComponentId { get; init; }
        public required int FromExtraData1 { get; init; }
        public required int FromExtraData2 { get; init; }
        public required int ToId { get; init; }
        public required int ToComponentId { get; init; }
        public required int ToExtraData1 { get; init; }
        public required int ToExtraData2 { get; init; }
    }
}
