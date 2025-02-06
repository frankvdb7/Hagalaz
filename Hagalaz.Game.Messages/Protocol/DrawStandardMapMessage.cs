using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model;
using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class DrawStandardMapMessage : RaidoMessage
    {
        public required bool RenderViewport { get; init; }
        public required bool ForceUpdate { get; init; }
        public required int CharacterIndex { get; init; }
        public required ILocation CharacterLocation { get; init; } = default!;
        public required int MapSizeIndex { get; init; }
        public required int RegionPartX { get; init; }
        public required int RegionPartY { get; init; }
        public required IReadOnlyList<int[]> VisibleRegionXteaKeys { get; init; } = default!;
    }
}
