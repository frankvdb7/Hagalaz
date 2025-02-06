using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class DrawFrameComponentMessage : RaidoMessage
    {
        public required int Id { get; init; }
        // TODO - this should be true if going back from world map
        public required bool ForceRedraw { get; init; }
    }
}
