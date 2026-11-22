using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Cache.Abstractions.Types.Providers;
using Hagalaz.Game.Abstractions.Model.GameObjects;

namespace Hagalaz.Game.Abstractions.Model.Maps
{
    /// <summary>
    /// The base map content of one region: cache object placements, configured
    /// object spawns and blocked floor tiles, in region-local coordinates.
    /// It does not depend on a dimension and holds no runtime state.
    /// </summary>
    public sealed class MapRegionTemplate
    {
        public MapRegionTemplate(int regionId, IEnumerable<MapRegionTemplateObject> objects, IEnumerable<BlockedTerrainTile> blockedTiles)
        {
            RegionId = regionId;
            Objects = Array.AsReadOnly(objects.ToArray());
            BlockedTiles = Array.AsReadOnly(blockedTiles.ToArray());
        }

        public int RegionId { get; }

        public IReadOnlyList<MapRegionTemplateObject> Objects { get; }

        public IReadOnlyList<BlockedTerrainTile> BlockedTiles { get; }
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
