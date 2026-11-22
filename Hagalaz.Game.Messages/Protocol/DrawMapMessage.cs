using Hagalaz.Game.Abstractions.Model;
using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    /// <summary>
    /// The viewport header shared by the standard and dynamic map messages.
    /// </summary>
    public abstract class DrawMapMessage : RaidoMessage
    {
        public required bool RenderViewport { get; init; }
        public required bool ForceUpdate { get; init; }
        public required int CharacterIndex { get; init; }
        public required ILocation CharacterLocation { get; init; } = default!;
        public required int MapSizeIndex { get; init; }
        public required int RegionPartX { get; init; }
        public required int RegionPartY { get; init; }
    }
}
