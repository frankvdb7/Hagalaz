using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Abstractions.Types.Providers;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Microsoft.EntityFrameworkCore;

namespace Hagalaz.Services.GameWorld.Data
{
    public class MapRegionTemplateLoader : IMapRegionTemplateLoader
    {
        private const int RegionSize = 64;

        private readonly IGameObjectSpawnRepository _objectSpawnRepository;
        private readonly IGameObjectService _gameObjectService;
        private readonly IMapProvider _mapProvider;

        public MapRegionTemplateLoader(
            IGameObjectSpawnRepository objectSpawnRepository,
            IGameObjectService gameObjectService,
            IMapProvider mapProvider)
        {
            _objectSpawnRepository = objectSpawnRepository;
            _gameObjectService = gameObjectService;
            _mapProvider = mapProvider;
        }

        public async Task<MapRegionTemplate> LoadAsync(int regionId, int[] xteaKeys, CancellationToken cancellationToken = default)
        {
            var baseX = (regionId >> 8) * RegionSize;
            var baseY = (regionId & 0xff) * RegionSize;
            var objectSpawns = await _objectSpawnRepository
                .FindByBounds(baseX, baseY, baseX + RegionSize - 1, baseY + RegionSize - 1)
                .Select(spawn => new
                {
                    spawn.GameobjectId,
                    spawn.CoordX,
                    spawn.CoordY,
                    spawn.CoordZ,
                    spawn.Face,
                    spawn.Type
                })
                .ToArrayAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            var decodedRegion = _mapProvider.DecodeRegion(regionId, xteaKeys, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var objectIds = decodedRegion.RawObjectPlacements.Select(placement => placement.Id)
                .Concat(objectSpawns.Select(spawn => (int)spawn.GameobjectId))
                .Distinct()
                .ToArray();
            IReadOnlyDictionary<int, IGameObjectDefinition> definitions = objectIds.Length == 0
                ? new Dictionary<int, IGameObjectDefinition>()
                : await _gameObjectService.FindGameObjectDefinitionsByIdsAsync(objectIds, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            MapRegionTemplateObject FromPlacement(IMapObject placement) => new(
                placement.Id,
                placement.X,
                placement.Y,
                placement.Z,
                placement.Rotation,
                (ShapeType)placement.ShapeType,
                definitions[placement.Id],
                true);

            var spawns = objectSpawns.Select(spawn => new MapRegionTemplateObject(
                    (int)spawn.GameobjectId,
                    spawn.CoordX - baseX,
                    spawn.CoordY - baseY,
                    spawn.CoordZ,
                    spawn.Face,
                    (ShapeType)spawn.Type,
                    definitions[(int)spawn.GameobjectId],
                    false))
                .ToArray();
            var terrainFlags = new sbyte[MapTerrain.MapPlanes, MapTerrain.MapDimension, MapTerrain.MapDimension];
            for (var plane = 0; plane < MapTerrain.MapPlanes; plane++)
            {
                for (var localX = 0; localX < MapTerrain.MapDimension; localX++)
                {
                    for (var localY = 0; localY < MapTerrain.MapDimension; localY++)
                    {
                        terrainFlags[plane, localX, localY] = decodedRegion.GetTerrainFlags(plane, localX, localY);
                    }
                }
            }

            // Standard placements: a cache object is clipped on its collision plane; where that
            // falls below plane 0 the client still draws it on its raw plane without collision.
            // Entries the bridge rule leaves unchanged are shared with the raw (source) view.
            var rawPlacements = decodedRegion.RawObjectPlacements.ToArray();
            var rawObjects = new MapRegionTemplateObject[rawPlacements.Length];
            var standardObjects = new MapRegionTemplateObject[rawPlacements.Length];
            for (var index = 0; index < rawPlacements.Length; index++)
            {
                var placement = rawPlacements[index];
                var raw = FromPlacement(placement);
                var plane = MapTerrain.GetCollisionPlane(placement.Z, MapTerrain.IsBridge(terrainFlags[1, placement.X, placement.Y]));
                rawObjects[index] = raw;
                standardObjects[index] = plane < 0 ? raw with { HasCollision = false } : plane != raw.Z ? raw with { Z = plane } : raw;
            }

            return new MapRegionTemplate(
                regionId,
                standardObjects.Concat(spawns),
                decodedRegion.BlockedTiles,
                rawObjects.Concat(spawns),
                terrainFlags);
        }
    }
}
