using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Cache.Abstractions.Types.Providers;
using Hagalaz.Game.Abstractions.Model.GameObjects;

namespace Hagalaz.Game.Abstractions.Model.Maps
{
    /// <summary>
    /// The base map content of one region: cache object placements, configured
    /// object spawns and terrain, in region-local coordinates.
    /// It does not depend on a dimension and holds no runtime state.
    /// </summary>
    public sealed class MapRegionTemplate
    {
        private readonly sbyte[,,] _terrainFlags;

        /// <summary>
        /// Creates a template without bridges, where source planes equal collision planes.
        /// </summary>
        public MapRegionTemplate(int regionId, IEnumerable<MapRegionTemplateObject> objects, IEnumerable<BlockedTerrainTile> blockedTiles)
            : this(regionId, objects.ToArray(), blockedTiles.ToArray())
        {
        }

        private MapRegionTemplate(int regionId, MapRegionTemplateObject[] objects, BlockedTerrainTile[] blockedTiles)
            : this(regionId, objects, blockedTiles, objects, TerrainFromBlockedTiles(blockedTiles))
        {
        }

        /// <summary>
        /// Creates a template.
        /// </summary>
        /// <param name="regionId">The region id.</param>
        /// <param name="objects">Objects on their collision plane, as a standard region holds them.</param>
        /// <param name="blockedTiles">Blocked tiles on their collision plane, as a standard region flags them.</param>
        /// <param name="sourceObjects">Objects as a dynamic chunk selects them: cache objects on their raw landscape plane, configured spawns on their plane.</param>
        /// <param name="terrainFlags">Raw terrain flags indexed [plane, x, y].</param>
        public MapRegionTemplate(
            int regionId,
            IEnumerable<MapRegionTemplateObject> objects,
            IEnumerable<BlockedTerrainTile> blockedTiles,
            IEnumerable<MapRegionTemplateObject> sourceObjects,
            sbyte[,,] terrainFlags)
        {
            RegionId = regionId;
            Objects = Array.AsReadOnly(objects.ToArray());
            BlockedTiles = Array.AsReadOnly(blockedTiles.ToArray());
            SourceObjects = Array.AsReadOnly(sourceObjects.ToArray());
            _terrainFlags = (sbyte[,,])terrainFlags.Clone();
        }

        public int RegionId { get; }

        /// <summary>
        /// Objects on their collision plane, as a standard region holds them.
        /// </summary>
        public IReadOnlyList<MapRegionTemplateObject> Objects { get; }

        /// <summary>
        /// Blocked tiles on their collision plane, as a standard region flags them.
        /// </summary>
        public IReadOnlyList<BlockedTerrainTile> BlockedTiles { get; }

        /// <summary>
        /// Objects as a dynamic chunk selects them: static cache objects on their raw
        /// landscape plane, configured spawns on their plane.
        /// </summary>
        public IReadOnlyList<MapRegionTemplateObject> SourceObjects { get; }

        /// <summary>
        /// Gets the raw terrain flags of a tile on a raw plane.
        /// </summary>
        public sbyte GetTerrainFlags(int plane, int localX, int localY) => _terrainFlags[plane, localX, localY];

        private static sbyte[,,] TerrainFromBlockedTiles(IEnumerable<BlockedTerrainTile> blockedTiles)
        {
            var terrain = new sbyte[MapTerrain.MapPlanes, MapTerrain.MapDimension, MapTerrain.MapDimension];
            foreach (var tile in blockedTiles)
            {
                terrain[tile.Z, tile.LocalX, tile.LocalY] |= MapTerrain.BlockedFlag;
            }

            return terrain;
        }
    }

    /// <summary>
    /// A template object in region-local coordinates. Cache placements are static;
    /// configured spawns are not.
    /// </summary>
    public sealed record MapRegionTemplateObject(
        int Id,
        int LocalX,
        int LocalY,
        int Z,
        int Rotation,
        ShapeType Shape,
        IGameObjectDefinition Definition,
        bool IsStatic);
}
