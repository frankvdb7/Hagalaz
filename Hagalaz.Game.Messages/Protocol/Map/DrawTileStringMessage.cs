using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol.Map
{
    public class DrawTileStringMessage : RaidoMessage
    {
        public string Value { get; init; } = string.Empty;
        public int Color { get; init; }
        public int Duration { get; init; }
        public int Height { get; init; }
        public int PartLocalX { get; init; }
        public int PartLocalY { get; init; }
    }
}
