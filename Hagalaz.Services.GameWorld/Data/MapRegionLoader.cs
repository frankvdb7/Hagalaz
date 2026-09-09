using System;
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
using Hagalaz.Services.GameWorld.Model.Maps.Regions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Data
{
    public class MapRegionLoader : IMapRegionLoader
    {
        private readonly INpcService _npcService;
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
            if (region.IsLoaded)
            {
                return;
            }

            var watch = Stopwatch.StartNew();
            var min = _locationBuilder.Create().FromLocation(region.BaseLocation).WithZ(0).ToRegionCoordinates(0, 0, region.Size.X, region.Size.Y).Build();
            var max = _locationBuilder.Create()
                .FromLocation(region.BaseLocation)
                .WithZ(region.Size.Z)
                .ToRegionCoordinates(region.Size.X - 1, region.Size.Y - 1, region.Size.X, region.Size.Y)
                .Build();
            try
            {
                await LoadAllNpcsAsync(region, min, max, cancellationToken);
                await LoadAllGroundItemsAsync(region, min, max, cancellationToken);
                await LoadAllStaticGameObjectsAsync(region, cancellationToken);
                await LoadAllNonStaticGameObjectsAsync(region, min, max, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                region.Load();

                _logger.LogDebug("Region[{id}] was loaded in {ms} ms", region.Id, watch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                try
                {
                    if (region is not IMapRegionLoadRollback rollback)
                    {
                        throw new InvalidOperationException($"Region '{region.Id}' does not support unpublished-load rollback.");
                    }

                    await rollback.ResetUnpublishedLoadAsync(CancellationToken.None);
                }
                catch (Exception rollbackException)
                {
                    _logger.LogError(
                        rollbackException,
                        "Region[{id}] failed to roll back an unpublished load attempt",
                        region.Id);
                    throw new AggregateException(
                        $"Region[{region.Id}] failed to load and its rollback also failed.",
                        ex,
                        rollbackException);
                }

                _logger.LogError(ex, "Region[{id}] failed to load", region.Id);
                throw;
            }
        }

        private async Task LoadAllNpcsAsync(IMapRegion region, ILocation min, ILocation max, CancellationToken cancellationToken)
        {
            var spawnsInRegion = await _mapper.ProjectTo<NpcSpawnDto>(_npcSpawnRepository.FindByBounds(min.X, min.Y, max.X, max.Y)).ToArrayAsync(cancellationToken);
            var npcsInRegion = spawnsInRegion.Select(spawn =>
            {
                var location = spawn.Location.Copy(region.BaseLocation.Dimension);
                var minBounds = spawn.MinimumBounds.Copy(region.BaseLocation.Dimension);
                var maxBounds = spawn.MaximumBounds.Copy(region.BaseLocation.Dimension);
                var faceDirection = spawn.SpawnDirection.HasValue ? DirectionHelper.GetNpcFaceDirection(spawn.SpawnDirection.Value) : DirectionFlag.None;
                var npc = _npcBuilder
                    .Create()
                    .WithId(spawn.NpcId)
                    .WithLocation(location)
                    .WithMinimumBounds(minBounds)
                    .WithMaximumBounds(maxBounds)
                    .WithFaceDirection(faceDirection)
                    .Build();
                return npc;
            });

            foreach (var npc in npcsInRegion)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _npcService.RegisterAsync(npc);
            }
        }

        private async Task LoadAllGroundItemsAsync(IMapRegion region, ILocation min, ILocation max, CancellationToken cancellationToken)
        {
            var spawnsInRegion = await _mapper.ProjectTo<GroundItemSpawnDto>(_itemSpawnRepository.FindByBounds(min.X, min.Y, max.X, max.Y)).ToArrayAsync(cancellationToken);
            var itemsInRegion = spawnsInRegion.Select(spawn =>
            {
                var location = spawn.Location.Copy(region.BaseLocation.Dimension);
                var groundItem = _groundItemBuilder
                    .Create()
                    .WithItem(builder => builder.Create().WithId(spawn.ItemID).WithCount(spawn.ItemCount))
                    .WithLocation(location)
                    .WithRespawnTicks(spawn.RespawnTicks)
                    .Build();
                return groundItem;
            });

            foreach (var groundItem in itemsInRegion)
            {
                cancellationToken.ThrowIfCancellationRequested();
                region.Add(groundItem);
            }
        }

        private async Task LoadAllNonStaticGameObjectsAsync(IMapRegion region, ILocation min, ILocation max, CancellationToken cancellationToken)
        {
            var spawnsInRegion = await _objectSpawnRepository.FindByBounds(min.X, min.Y, max.X, max.Y)
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
            var objectsInRegion = spawnsInRegion.Select(spawn =>
            {
                var location = new Location(spawn.CoordX, spawn.CoordY, spawn.CoordZ, region.BaseLocation.Dimension);
                var gameObject = _gameObjectBuilder
                    .Create()
                    .WithId((int)spawn.GameobjectId)
                    .WithLocation(location)
                    .WithRotation(spawn.Face)
                    .WithShape((ShapeType)spawn.Type)
                    .Build();
                return gameObject;
            });

            foreach (var gameObject in objectsInRegion)
            {
                cancellationToken.ThrowIfCancellationRequested();
                region.Add(gameObject);
            }
        }

        private Task LoadAllStaticGameObjectsAsync(IMapRegion region, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _mapProvider.DecodeRegion(
                region.Id,
                region.XteaKeys,
                (objectId, shapeType, rotation, localX, localY, z) =>
                {
                    var location = _locationBuilder.Create()
                        .FromLocation(region.BaseLocation)
                        .WithZ(z)
                        .ToRegionCoordinates(localX, localY, region.Size.X, region.Size.Y)
                        .Build();
                    var gameObject = _gameObjectBuilder
                        .Create()
                        .WithId(objectId)
                        .WithLocation(location)
                        .WithRotation(rotation)
                        .WithShape((ShapeType)shapeType)
                        .AsStatic()
                        .Build();
                    region.Add(gameObject);
                },
                (localX, localY, z) => region.FlagCollision(localX, localY, z, CollisionFlag.FloorBlock));
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }
}
