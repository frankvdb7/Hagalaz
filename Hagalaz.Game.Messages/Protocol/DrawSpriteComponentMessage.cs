using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class DrawSpriteComponentMessage : RaidoMessage 
    {
        public required int ComponentId { get; init; }
        public required int ChildId { get; init; }
        public required int SpriteId { get; init; }
    }
}
