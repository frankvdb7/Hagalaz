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
        private readonly sbyte[,,] _terrainFlags;
        private readonly Lazy<IReadOnlyList<IMapObject>> _objectPlacements;

        public static DecodedMapRegion Empty { get; } = new([], []);

        /// <summary>
        /// Creates a region without bridges, where raw planes equal collision planes.
        /// </summary>
        public DecodedMapRegion(IEnumerable<IMapObject> objectPlacements, IEnumerable<BlockedTerrainTile> blockedTiles)
            : this(objectPlacements.ToArray(), blockedTiles.ToArray())
        {
        }

        private DecodedMapRegion(IMapObject[] objectPlacements, BlockedTerrainTile[] blockedTiles)
            : this(objectPlacements, blockedTiles, objectPlacements, MapTerrain.FromBlockedTiles(blockedTiles))
        {
        }

        /// <summary>
        /// Creates a decoded region.
        /// </summary>
        /// <param name="objectPlacements">Objects on their collision plane (bridge-adjusted), as standard regions use them.</param>
        /// <param name="blockedTiles">Blocked tiles on their collision plane (bridge-adjusted).</param>
        /// <param name="rawObjectPlacements">Objects on their raw landscape plane, as the client selects them for dynamic chunks.</param>
        /// <param name="terrainFlags">Raw terrain flags indexed [plane, x, y].</param>
        public DecodedMapRegion(
            IEnumerable<IMapObject> objectPlacements,
            IEnumerable<BlockedTerrainTile> blockedTiles,
            IEnumerable<IMapObject> rawObjectPlacements,
            sbyte[,,] terrainFlags)
        {
            var adjusted = Array.AsReadOnly(objectPlacements.ToArray());
            _objectPlacements = new Lazy<IReadOnlyList<IMapObject>>(adjusted);
            BlockedTiles = Array.AsReadOnly(blockedTiles.ToArray());
            RawObjectPlacements = Array.AsReadOnly(rawObjectPlacements.ToArray());
            _terrainFlags = (sbyte[,,])terrainFlags.Clone();
        }

        private DecodedMapRegion(
            IEnumerable<IMapObject> rawObjectPlacements,
            IEnumerable<BlockedTerrainTile> blockedTiles,
            sbyte[,,] terrainFlags,
            Func<IReadOnlyList<IMapObject>, sbyte[,,], IEnumerable<IMapObject>> toCollisionPlanes)
        {
            BlockedTiles = Array.AsReadOnly(blockedTiles.ToArray());
            RawObjectPlacements = Array.AsReadOnly(rawObjectPlacements.ToArray());
            _terrainFlags = (sbyte[,,])terrainFlags.Clone();
            // Computed from the snapshots this region owns, and only when a consumer asks for it.
            _objectPlacements = new Lazy<IReadOnlyList<IMapObject>>(
                () => Array.AsReadOnly(toCollisionPlanes(RawObjectPlacements, _terrainFlags).ToArray()));
        }

        /// <summary>
        /// Creates a region whose bridge-adjusted <see cref="ObjectPlacements"/> are derived on first access
        /// from the raw placements and terrain this region owns.
        /// </summary>
        /// <param name="rawObjectPlacements">Objects on their raw landscape plane.</param>
        /// <param name="blockedTiles">Blocked tiles on their collision plane.</param>
        /// <param name="terrainFlags">Raw terrain flags indexed [plane, x, y].</param>
        /// <param name="toCollisionPlanes">Maps the owned raw placements and terrain to the bridge-adjusted objects.</param>
        public static DecodedMapRegion CreateWithDerivedObjectPlacements(
            IEnumerable<IMapObject> rawObjectPlacements,
            IEnumerable<BlockedTerrainTile> blockedTiles,
            sbyte[,,] terrainFlags,
            Func<IReadOnlyList<IMapObject>, sbyte[,,], IEnumerable<IMapObject>> toCollisionPlanes) =>
            new(rawObjectPlacements, blockedTiles, terrainFlags, toCollisionPlanes);

        public IReadOnlyList<IMapObject> ObjectPlacements => _objectPlacements.Value;

        public IReadOnlyList<BlockedTerrainTile> BlockedTiles { get; }

        public IReadOnlyList<IMapObject> RawObjectPlacements { get; }

        /// <summary>
        /// Gets the raw terrain flags of a tile on a raw plane.
        /// </summary>
        public sbyte GetTerrainFlags(int plane, int localX, int localY) => _terrainFlags[plane, localX, localY];
    }

    /// <summary>
    /// Identifies a blocked terrain tile in region-local coordinates.
    /// </summary>
    public readonly record struct BlockedTerrainTile(int LocalX, int LocalY, int Z);
}
