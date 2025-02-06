using Hagalaz.Game.Abstractions.Model;
using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class DrawLocationHintIconMessage : RaidoMessage   
    {
        public required int IconIndex { get; init; }
        public required int IconModelId { get; init; }
        public required int X { get; init; }
        public required int Y { get; init; }
        public required int Z { get; init; }
        public required int Height { get; init; }
        public required int ArrowId { get; init; }
        public required int ViewDistance { get; init; }
        public required HintIconDirection Direction { get; init; }
    }
}
