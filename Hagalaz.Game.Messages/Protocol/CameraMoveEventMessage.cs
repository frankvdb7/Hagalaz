using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class CameraMoveEventMessage : RaidoMessage
    {
        public required int X { get; init; }
        public required int Y { get; init; }
    }
}
