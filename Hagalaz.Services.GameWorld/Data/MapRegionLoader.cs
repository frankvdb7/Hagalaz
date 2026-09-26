using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Cache.Abstractions.Types.Providers;
using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.Location;
using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Services.GameWorld.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Data
{
    public class MapRegionLoader : IMapRegionLoader
    {
        private const string LoadActivityName = "Hagalaz.Services.GameWorld.MapRegionLoader.Load";
        private static readonly ActivitySource ActivitySource = new("Hagalaz.Services.GameWorld");

        private readonly INpcService _npcService;
        private readonly IMapRegionService _regionService;
        private readonly INpcSpawnRepository _npcSpawnRepository;
        private readonly IGroundItemSpawnRepository _itemSpawnRepository;
        private readonly IGameObjectSpawnRepository _objectSpawnRepository;
        private readonly IGameObjectService _gameObjectService;
        private readonly IMapProvider _mapProvider;
        private readonly ILocationBuilder _locationBuilder;
        private readonly IGroundItemBuilder _groundItemBuilder;
        private readonly IGameObjectBuilder _gameObjectBuilder;
        private readonly INpcBuilder _npcBuilder;
        private readonly IMapper _mapper;
        private readonly ILogger<MapRegionLoader> _logger;
        private readonly IEntityStore _entityStore;

        public MapRegionLoader(
            INpcService npcService,
            IMapRegionService regionService,
            INpcSpawnRepository npcSpawnRepository,
            IGroundItemSpawnRepository itemSpawnRepository,
            IGameObjectSpawnRepository objectSpawnRepository,
            IGameObjectService gameObjectService,
            IMapProvider mapProvider,
            ILocationBuilder locationBuilder,
            IGroundItemBuilder groundItemBuilder,
            IGameObjectBuilder gameObjectBuilder,
            INpcBuilder npcBuilder,
            IMapper mapper,
            ILogger<MapRegionLoader> logger,
            IEntityStore entityStore)
        {
            _npcService = npcService;
            _regionService = regionService;
            _npcSpawnRepository = npcSpawnRepository;
            _itemSpawnRepository = itemSpawnRepository;
            _objectSpawnRepository = objectSpawnRepository;
            _gameObjectService = gameObjectService;
            _mapProvider = mapProvider;
            _locationBuilder = locationBuilder;
            _groundItemBuilder = groundItemBuilder;
            _gameObjectBuilder = gameObjectBuilder;
            _npcBuilder = npcBuilder;
            _mapper = mapper;
            _logger = logger;
            _entityStore = entityStore;
        }

        public async Task LoadAsync(IMapRegion region, CancellationToken cancellationToken = default)
        {
            if (region.State == MapRegionState.Ready)
            {
                return;
            }

            if (region.State == MapRegionState.Discarded)
            {
                throw new InvalidOperationException($"Region[{region.Id}] is discarded and cannot be loaded.");
            }

            if (!_regionService.IsCurrentMapRegion(region.Id, region.BaseLocation.Dimension, region))
            {
                throw new InvalidOperationException($"Region[{region.Id}] is no longer the current region instance.");
            }

            using var activity = ActivitySource.StartActivity(LoadActivityName, ActivityKind.Internal);
            activity?.SetTag("map.region.id", region.Id);
            activity?.SetTag("map.region.dimension", region.BaseLocation.Dimension);

            var loadStart = Stopwatch.GetTimestamp();
            var outcome = "failure";
            var registeredNpcs = new List<INpc>();
            var createdGroundItems = new List<IGroundItem>();
            var createdGameObjects = new List<IGameObject>();

            try
            {
                var min = _locationBuilder.Create().FromLocation(region.BaseLocation).WithZ(0).ToRegionCoordinates(0, 0, region.Size.X, region.Size.Y).Build();
                var max = _locationBuilder.Create()
                    .FromLocation(region.BaseLocation)
                    .WithZ(region.Size.Z)
                    .ToRegionCoordinates(region.Size.X - 1, region.Size.Y - 1, region.Size.X, region.Size.Y)
                    .Build();
                var prepared = await PrepareAsync(
                    region,
                    min,
                    max,
                    createdGroundItems,
                    createdGameObjects,
                    activity,
                    cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                ApplyPreparedRegion(region, prepared, createdGroundItems, createdGameObjects);
                cancellationToken.ThrowIfCancellationRequested();

                await RegisterNpcsAsync(region, prepared.NpcSpawns, registeredNpcs, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                region.MarkReady();

                activity?.SetTag("map.region.load.outcome", "success");
                activity?.SetStatus(ActivityStatusCode.Ok);
                outcome = "success";
            }
            catch (Exception exception)
            {
                outcome = exception is OperationCanceledException ? "cancelled" : "failure";
                activity?.SetTag("map.region.load.outcome", outcome);
                if (exception is not OperationCanceledException)
                {
                    activity?.SetTag("error.type", exception.GetType().FullName);
                    activity?.SetStatus(ActivityStatusCode.Error);
                }

                if (region.State == MapRegionState.Initializing)
                {
                    region.MarkDiscarded();
                }

                _regionService.TryRemoveMapRegion(region.Id, region.BaseLocation.Dimension, region);

                await UnregisterRegisteredNpcsAsync(registeredNpcs, region);
                RollbackPreparedResources(region, createdGroundItems, createdGameObjects);

                _logger.LogError(exception, "Region[{id}] failed to load and was discarded", region.Id);
                throw;
            }
            finally
            {
                GameWorldMetrics.RecordRegionLoad(outcome, Stopwatch.GetElapsedTime(loadStart).TotalSeconds);
            }
        }

        private async Task<PreparedRegion> PrepareAsync(
            IMapRegion region,
            ILocation min,
            ILocation max,
            List<IGroundItem> createdGroundItems,
            List<IGameObject> createdGameObjects,
            Activity? activity,
            CancellationToken cancellationToken)
        {
            var npcSpawns = await _mapper.ProjectTo<NpcSpawnDto>(_npcSpawnRepository.FindByBounds(min.X, min.Y, max.X, max.Y)).ToArrayAsync(cancellationToken);
            var itemSpawns = await _mapper.ProjectTo<GroundItemSpawnDto>(_itemSpawnRepository.FindByBounds(min.X, min.Y, max.X, max.Y)).ToArrayAsync(cancellationToken);
            var objectSpawns = await _objectSpawnRepository.FindByBounds(min.X, min.Y, max.X, max.Y)
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

            var staticObjectSpawns = new List<StaticObjectSpawn>();
            var collisionTiles = new List<CollisionTile>();
            cancellationToken.ThrowIfCancellationRequested();
            _mapProvider.DecodeRegion(
                region.Id,
                region.XteaKeys,
                (objectId, shapeType, rotation, localX, localY, z) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    staticObjectSpawns.Add(new StaticObjectSpawn(objectId, shapeType, rotation, localX, localY, z));
                },
                (localX, localY, z) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    collisionTiles.Add(new CollisionTile(localX, localY, z));
                });

            cancellationToken.ThrowIfCancellationRequested();
            var objectIds = staticObjectSpawns.Select(spawn => spawn.Id)
                .Concat(objectSpawns.Select(spawn => (int)spawn.GameobjectId))
                .Distinct()
                .ToArray();
            activity?.SetTag("map.region.static_gameobject_placement_count", staticObjectSpawns.Count);
            activity?.SetTag("map.region.database_gameobject_spawn_count", objectSpawns.Length);
            activity?.SetTag("map.region.gameobject_placement_count", staticObjectSpawns.Count + objectSpawns.Length);
            activity?.SetTag("map.region.gameobject.definition_id_count", objectIds.Length);
            activity?.SetTag("map.region.npc_spawn_count", npcSpawns.Length);

            IReadOnlyDictionary<int, IGameObjectDefinition> definitions;
            if (objectIds.Length == 0)
            {
                definitions = new Dictionary<int, IGameObjectDefinition>();
            }
            else
            {
                definitions = await _gameObjectService.FindGameObjectDefinitionsByIdsAsync(objectIds, cancellationToken);
            }
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var spawn in staticObjectSpawns)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var location = _locationBuilder.Create()
                    .FromLocation(region.BaseLocation)
                    .WithZ(spawn.Z)
                    .ToRegionCoordinates(spawn.LocalX, spawn.LocalY, region.Size.X, region.Size.Y)
                    .Build();
                var gameObject = _gameObjectBuilder
                    .Create()
                    .WithId(spawn.Id)
                    .WithLocation(location)
                    .WithDefinition(definitions[spawn.Id])
                    .WithRotation(spawn.Rotation)
                    .WithShape((ShapeType)spawn.ShapeType)
                    .AsStatic()
                    .Build();
                createdGameObjects.Add(gameObject);
            }

            foreach (var spawn in itemSpawns)
            {
                var location = spawn.Location.Copy(region.BaseLocation.Dimension);
                var groundItem = _groundItemBuilder
                    .Create()
                    .WithItem(builder => builder.Create().WithId(spawn.ItemID).WithCount(spawn.ItemCount))
                    .WithLocation(location)
                    .WithRespawnTicks(spawn.RespawnTicks)
                    .Build();
                createdGroundItems.Add(groundItem);
            }

            foreach (var spawn in objectSpawns)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var location = new Location(spawn.CoordX, spawn.CoordY, spawn.CoordZ, region.BaseLocation.Dimension);
                var gameObject = _gameObjectBuilder
                    .Create()
                    .WithId((int)spawn.GameobjectId)
                    .WithLocation(location)
                    .WithDefinition(definitions[(int)spawn.GameobjectId])
                    .WithRotation(spawn.Face)
                    .WithShape((ShapeType)spawn.Type)
                    .Build();
                createdGameObjects.Add(gameObject);
            }

            return new PreparedRegion(npcSpawns, collisionTiles);
        }

        private static void ApplyPreparedRegion(
            IMapRegion region,
            PreparedRegion prepared,
            IReadOnlyList<IGroundItem> groundItems,
            IReadOnlyList<IGameObject> gameObjects)
        {
            foreach (var tile in prepared.CollisionTiles)
            {
                region.FlagCollision(tile.LocalX, tile.LocalY, tile.Z, CollisionFlag.FloorBlock);
            }

            foreach (var gameObject in gameObjects)
            {
                region.Add(gameObject);
            }

            foreach (var groundItem in groundItems)
            {
                region.Add(groundItem);
            }
        }

        private async Task RegisterNpcsAsync(IMapRegion region, IReadOnlyList<NpcSpawnDto> spawns, List<INpc> registeredNpcs, CancellationToken cancellationToken)
        {
            foreach (var spawn in spawns)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var npc = BuildNpc(region, spawn);
                await _npcService.RegisterAsync(npc);
                registeredNpcs.Add(npc);
            }
        }

        private INpc BuildNpc(IMapRegion region, NpcSpawnDto spawn)
        {
            var location = spawn.Location.Copy(region.BaseLocation.Dimension);
            var minBounds = spawn.MinimumBounds.Copy(region.BaseLocation.Dimension);
            var maxBounds = spawn.MaximumBounds.Copy(region.BaseLocation.Dimension);
            var faceDirection = spawn.SpawnDirection.HasValue ? DirectionHelper.GetNpcFaceDirection(spawn.SpawnDirection.Value) : DirectionFlag.None;
            return _npcBuilder
                .Create()
                .WithId(spawn.NpcId)
                .WithLocation(location)
                .WithMinimumBounds(minBounds)
                .WithMaximumBounds(maxBounds)
                .WithFaceDirection(faceDirection)
                .Build();
        }

        private async Task UnregisterRegisteredNpcsAsync(IReadOnlyList<INpc> npcs, IMapRegion region)
        {
            foreach (var npc in npcs)
            {
                try
                {
                    await _npcService.UnregisterAsync(npc);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Region[{id}] could not clean up NPC '{npc}' after load failure", region.Id, npc);
                }
            }
        }

        private void RollbackPreparedResources(
            IMapRegion region,
            IReadOnlyList<IGroundItem> groundItems,
            IReadOnlyList<IGameObject> gameObjects)
        {
            foreach (var item in groundItems)
            {
                try
                {
                    item.Destroy();
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Region[{id}] could not clean up ground item after load failure", region.Id);
                }
                finally
                {
                    _entityStore.Remove(item);
                }
            }

            foreach (var gameObject in gameObjects)
            {
                try
                {
                    gameObject.Destroy();
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Region[{id}] could not clean up game object after load failure", region.Id);
                }
                finally
                {
                    _entityStore.Remove(gameObject);
                }
            }
        }

        private sealed record PreparedRegion(
            NpcSpawnDto[] NpcSpawns,
            List<CollisionTile> CollisionTiles);

        private readonly record struct StaticObjectSpawn(int Id, int ShapeType, int Rotation, int LocalX, int LocalY, int Z);
        private readonly record struct CollisionTile(int LocalX, int LocalY, int Z);
    }
}
