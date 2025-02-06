using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class MovementMessage : RaidoMessage
    {
        public required int AbsX { get; init; }
        public required int AbsY { get; init; }
        public required bool ForceRun { get; init; }
    }
}
