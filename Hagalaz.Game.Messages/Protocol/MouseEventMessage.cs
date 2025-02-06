using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class MouseEventMessage : RaidoMessage
    {
        public required int DeltaTime { get; init; }
        public required int EventCode { get; init; }
        public required int ScreenX { get; init; }
        public required int ScreenY { get; init; }
    }
}
