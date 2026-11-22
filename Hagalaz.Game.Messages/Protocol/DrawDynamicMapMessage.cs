using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model;
using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    /// <summary>
    /// Draws a viewport that contains dynamic (instanced) region parts.
    /// </summary>
    public class DrawDynamicMapMessage : RaidoMessage
    {
        public required bool RenderViewport { get; init; }
        public required bool ForceUpdate { get; init; }
        public required int CharacterIndex { get; init; }
        public required ILocation CharacterLocation { get; init; } = default!;
        public required int MapSizeIndex { get; init; }
        public required int RegionPartX { get; init; }
        public required int RegionPartY { get; init; }

        /// <summary>
        /// Gets the viewport's 8x8 chunk slots in client order: plane, then viewport part X, then viewport part Y.
        /// </summary>
        public required IReadOnlyList<DynamicMapChunk> Chunks { get; init; } = default!;

        /// <summary>
        /// Gets the XTEA keys of each distinct source region referenced by a present chunk, in first-seen chunk order.
        /// </summary>
        public required IReadOnlyList<int[]> SourceRegionXteaKeys { get; init; } = default!;
    }

    /// <summary>
    /// One 8x8 viewport slot. An absent slot is distinct from a present slot whose source is part (0, 0), plane 0, rotation 0.
    /// </summary>
    public readonly record struct DynamicMapChunk(bool IsPresent, int SourcePartX, int SourcePartY, int SourcePlane, int Rotation)
    {
        public static DynamicMapChunk Absent => default;

        public static DynamicMapChunk Present(int sourcePartX, int sourcePartY, int sourcePlane, int rotation) =>
            new(true, sourcePartX, sourcePartY, sourcePlane, rotation);
    }
}
