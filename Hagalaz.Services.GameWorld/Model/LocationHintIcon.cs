using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Model
{
    public class LocationHintIcon : IHintIcon
    {
        public int Index { get; set; }
        public int ModelId { get; init; }
        public ILocation Target { get; init; } = default!;
        public int Height { get; init; }
        public HintIconDirection Direction { get; init; }
        public int ArrowId { get; init; }
        /// <summary>
        /// Contains the view distance.
        /// Distance to start showing on minimap, 0 doesnt show, -1 infinite
        /// </summary>
        public int ViewDistance { get; init; }

        public RaidoMessage ToMessage() => new DrawLocationHintIconMessage
        {
            IconIndex = Index,
            IconModelId = ModelId,
            ArrowId = ArrowId,
            Height = Height,
            X = Target.X,
            Y = Target.Y,
            Z = Target.Z,
            ViewDistance = ViewDistance,
            Direction = Direction,
        };
    }
}