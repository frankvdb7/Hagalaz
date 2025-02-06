using Hagalaz.Game.Abstractions.Model.GameObjects;
using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol.Map
{
    public class RemoveGameObjectMessage : RaidoMessage
    {
        public int PartLocalX { get; init; }
        public int PartLocalY { get; init; }
        public int Rotation { get; init; }
        public ShapeType Shape { get; init; }
    }
}
