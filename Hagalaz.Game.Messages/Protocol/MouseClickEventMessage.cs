using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class MouseClickEventMessage : RaidoMessage
    {
        public required int DeltaTime { get; init; }
        public required bool LeftClick { get; init; }
        public required int ScreenX { get; init; }
        public required int ScreenY { get; init; }
    }
}
