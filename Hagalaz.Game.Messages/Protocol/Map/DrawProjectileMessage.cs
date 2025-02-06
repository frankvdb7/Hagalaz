using Hagalaz.Game.Abstractions.Model;
using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol.Map
{
    public class DrawProjectileMessage : RaidoMessage
    {
        public int GraphicId { get; init; }
        public int FromHeight { get; init; }
        public int ToHeight { get; init; }
        public int Delay { get; init; }
        public int Duration { get; init; }
        public int Slope { get; init; }
        public int Angle { get; init; }
        public ILocation FromLocation { get; init; } = default!;
        public ILocation ToLocation { get; init; } = default!;
        public bool AdjustFromFlyingHeight { get; init; }
        public bool AdjustToFlyingHeight { get; init; }
        public int FromBodyPartId { get; init; }
        public int? FromIndex { get; init; }
        public int? ToIndex { get; init; }
        public bool FromIsCharacter { get; init; } 
        public bool ToIsCharacter { get; init; }
    }
}
