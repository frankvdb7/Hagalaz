using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
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
        private readonly IMapRegionTemplateLoader _templateLoader;
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
            IMapRegionTemplateLoader templateLoader,
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
            _templateLoader = templateLoader;
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

            using var activity = ActivitySource.StartActivity(LoadActivityName);
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
            var template = await _templateLoader.LoadAsync(region.Id, region.XteaKeys, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var staticPlacementCount = template.Objects.Count(entry => entry.IsStatic);
            activity?.SetTag("map.region.static_gameobject_placement_count", staticPlacementCount);
            activity?.SetTag("map.region.database_gameobject_spawn_count", template.Objects.Count - staticPlacementCount);
            activity?.SetTag("map.region.gameobject_placement_count", template.Objects.Count);
            activity?.SetTag("map.region.gameobject.definition_id_count", template.Objects.Select(entry => entry.Id).Distinct().Count());
            activity?.SetTag("map.region.npc_spawn_count", npcSpawns.Length);

            var objectsWithoutCollision = new HashSet<IGameObject>(ReferenceEqualityComparer.Instance);
            var collisionTiles = template.BlockedTiles
                .Select(tile => new CollisionTile(tile.LocalX, tile.LocalY, tile.Z))
                .ToList();
            foreach (var entry in template.Objects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var location = Location.Create(
                    region.BaseLocation.RegionX * region.Size.X + entry.LocalX,
                    region.BaseLocation.RegionY * region.Size.Y + entry.LocalY,
                    entry.Z,
                    region.BaseLocation.Dimension);
                var builder = _gameObjectBuilder
                    .Create()
                    .WithId(entry.Id)
                    .WithLocation(location)
                    .WithDefinition(entry.Definition)
                    .WithRotation(entry.Rotation)
                    .WithShape(entry.Shape);
                if (entry.IsStatic)
                {
                    builder = builder.AsStatic();
                }

                var gameObject = builder.Build();
                createdGameObjects.Add(gameObject);
                if (!entry.HasCollision)
                {
                    objectsWithoutCollision.Add(gameObject);
                }
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

            return new PreparedRegion(npcSpawns, collisionTiles, objectsWithoutCollision);
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
                if (prepared.ObjectsWithoutCollision.Contains(gameObject))
                {
                    region.Add(gameObject, false);
                }
                else
                {
                    region.Add(gameObject);
                }
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
            List<CollisionTile> CollisionTiles,
            IReadOnlySet<IGameObject> ObjectsWithoutCollision);

        private readonly record struct CollisionTile(int LocalX, int LocalY, int Z);
    }
}
