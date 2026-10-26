using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Cache.Abstractions.Types.Providers
{
    /// <summary>
    /// Contains the decoded object placements and blocked tiles for a map region.
    /// </summary>
    public sealed class DecodedMapRegion
    {
        public static DecodedMapRegion Empty { get; } = new([], []);

        public DecodedMapRegion(IEnumerable<IMapObject> objectPlacements, IEnumerable<BlockedTerrainTile> blockedTiles)
        {
            ObjectPlacements = Array.AsReadOnly(objectPlacements.ToArray());
            BlockedTiles = Array.AsReadOnly(blockedTiles.ToArray());
        }

        public IReadOnlyList<IMapObject> ObjectPlacements { get; }

        public IReadOnlyList<BlockedTerrainTile> BlockedTiles { get; }
    }

    /// <summary>
    /// Identifies a blocked terrain tile in region-local coordinates.
    /// </summary>
    public readonly record struct BlockedTerrainTile(int LocalX, int LocalY, int Z);
}
