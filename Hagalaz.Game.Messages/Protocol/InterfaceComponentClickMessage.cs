using Hagalaz.Game.Abstractions.Model.Widgets;
using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class InterfaceComponentClickMessage : RaidoMessage
    {
        public required int InterfaceId { get; init; }
        public required int ChildId { get; init; }
        public required ComponentClickType ClickType { get; init; }
        public required int ExtraData1 { get; init; }
        public required int ExtraData2 { get; init; }
    }
}
