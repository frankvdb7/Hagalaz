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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Data
{
    public class MapRegionLoader : IMapRegionLoader
    {
        private readonly INpcService _npcService;
        private readonly IMapRegionService _regionService;
        private readonly INpcSpawnRepository _npcSpawnRepository;
        private readonly IGroundItemSpawnRepository _itemSpawnRepository;
        private readonly IGameObjectSpawnRepository _objectSpawnRepository;
        private readonly IMapProvider _mapProvider;
        private readonly ILocationBuilder _locationBuilder;
        private readonly IGroundItemBuilder _groundItemBuilder;
        private readonly IGameObjectBuilder _gameObjectBuilder;
        private readonly INpcBuilder _npcBuilder;
        private readonly IMapper _mapper;
        private readonly ILogger<MapRegionLoader> _logger;

        public MapRegionLoader(
            INpcService npcService,
            IMapRegionService regionService,
            INpcSpawnRepository npcSpawnRepository,
            IGroundItemSpawnRepository itemSpawnRepository,
            IGameObjectSpawnRepository objectSpawnRepository,
            IMapProvider mapProvider,
            ILocationBuilder locationBuilder,
            IGroundItemBuilder groundItemBuilder,
            IGameObjectBuilder gameObjectBuilder,
            INpcBuilder npcBuilder,
            IMapper mapper,
            ILogger<MapRegionLoader> logger)
        {
            _npcService = npcService;
            _regionService = regionService;
            _npcSpawnRepository = npcSpawnRepository;
            _itemSpawnRepository = itemSpawnRepository;
            _objectSpawnRepository = objectSpawnRepository;
            _mapProvider = mapProvider;
            _locationBuilder = locationBuilder;
            _groundItemBuilder = groundItemBuilder;
            _gameObjectBuilder = gameObjectBuilder;
            _npcBuilder = npcBuilder;
            _mapper = mapper;
            _logger = logger;
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

            var watch = Stopwatch.StartNew();
            var registeredNpcs = new List<INpc>();

            try
            {
                var min = _locationBuilder.Create().FromLocation(region.BaseLocation).WithZ(0).ToRegionCoordinates(0, 0, region.Size.X, region.Size.Y).Build();
                var max = _locationBuilder.Create()
                    .FromLocation(region.BaseLocation)
                    .WithZ(region.Size.Z)
                    .ToRegionCoordinates(region.Size.X - 1, region.Size.Y - 1, region.Size.X, region.Size.Y)
                    .Build();
                var prepared = await PrepareAsync(region, min, max, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                ApplyPreparedRegion(region, prepared);
                cancellationToken.ThrowIfCancellationRequested();

                await RegisterNpcsAsync(region, prepared.NpcSpawns, registeredNpcs, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                region.MarkReady();

                _logger.LogDebug("Region[{id}] was loaded in {ms} ms", region.Id, watch.ElapsedMilliseconds);
            }
            catch (Exception exception)
            {
                region.MarkDiscarded();
                await UnregisterRegisteredNpcsAsync(registeredNpcs, region);
                try
                {
                    _regionService.TryRemoveMapRegion(region.Id, region.BaseLocation.Dimension, region);
                }
                catch (Exception removalException)
                {
                    _logger.LogError(removalException, "Region[{id}] could not be removed after load failure", region.Id);
                }

                _logger.LogError(exception, "Region[{id}] failed to load and was discarded", region.Id);
                throw;
            }
        }

        private async Task<PreparedRegion> PrepareAsync(IMapRegion region, ILocation min, ILocation max, CancellationToken cancellationToken)
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
            var staticObjects = staticObjectSpawns.Select(spawn =>
            {
                var location = _locationBuilder.Create()
                    .FromLocation(region.BaseLocation)
                    .WithZ(spawn.Z)
                    .ToRegionCoordinates(spawn.LocalX, spawn.LocalY, region.Size.X, region.Size.Y)
                    .Build();
                return _gameObjectBuilder
                    .Create()
                    .WithId(spawn.Id)
                    .WithLocation(location)
                    .WithRotation(spawn.Rotation)
                    .WithShape((ShapeType)spawn.ShapeType)
                    .AsStatic()
                    .Build();
            }).ToArray();
            var groundItems = itemSpawns.Select(spawn =>
            {
                var location = spawn.Location.Copy(region.BaseLocation.Dimension);
                return _groundItemBuilder
                    .Create()
                    .WithItem(builder => builder.Create().WithId(spawn.ItemID).WithCount(spawn.ItemCount))
                    .WithLocation(location)
                    .WithRespawnTicks(spawn.RespawnTicks)
                    .Build();
            }).ToArray();
            var nonStaticObjects = objectSpawns.Select(spawn =>
            {
                var location = new Location(spawn.CoordX, spawn.CoordY, spawn.CoordZ, region.BaseLocation.Dimension);
                return _gameObjectBuilder
                    .Create()
                    .WithId((int)spawn.GameobjectId)
                    .WithLocation(location)
                    .WithRotation(spawn.Face)
                    .WithShape((ShapeType)spawn.Type)
                    .Build();
            }).ToArray();

            return new PreparedRegion(npcSpawns, groundItems, staticObjects, nonStaticObjects, collisionTiles);
        }

        private static void ApplyPreparedRegion(IMapRegion region, PreparedRegion prepared)
        {
            foreach (var tile in prepared.CollisionTiles)
            {
                region.FlagCollision(tile.LocalX, tile.LocalY, tile.Z, CollisionFlag.FloorBlock);
            }

            foreach (var gameObject in prepared.StaticObjects)
            {
                region.Add(gameObject);
            }

            foreach (var gameObject in prepared.NonStaticObjects)
            {
                region.Add(gameObject);
            }

            foreach (var groundItem in prepared.GroundItems)
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

        private sealed record PreparedRegion(
            NpcSpawnDto[] NpcSpawns,
            IGroundItem[] GroundItems,
            IGameObject[] StaticObjects,
            IGameObject[] NonStaticObjects,
            List<CollisionTile> CollisionTiles);

        private readonly record struct StaticObjectSpawn(int Id, int ShapeType, int Rotation, int LocalX, int LocalY, int Z);
        private readonly record struct CollisionTile(int LocalX, int LocalY, int Z);
    }
}
