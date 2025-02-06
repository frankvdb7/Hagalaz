using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol.Map
{
    public class DrawGraphicMessage : RaidoMessage
    {
        public int GraphicId { get; init; }
        public int Height { get; init; }
        public int Delay { get; init; }
        public int Rotation { get; init; }
        public int PartLocalX { get; init; }
        public int PartLocalY { get; init; }
        public int TargetIndex { get; init; }
    }
}
