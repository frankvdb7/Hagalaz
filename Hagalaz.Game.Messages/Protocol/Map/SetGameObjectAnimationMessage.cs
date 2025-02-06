using Hagalaz.Game.Abstractions.Model.GameObjects;
using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol.Map
{
    public class SetGameObjectAnimationMessage : RaidoMessage
    {
        public int AnimationId { get; init; }
        public ShapeType Shape { get; init; }
        public int Rotation { get; init; }
        public int PartLocalX { get; init; }
        public int PartLocalY { get; init; }
    }
}
