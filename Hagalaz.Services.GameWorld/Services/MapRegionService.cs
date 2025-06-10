using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Builders.Location;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Data;
using Hagalaz.Services.GameWorld.Model.Maps.Regions;
using Hagalaz.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Regions_MapRegion = Hagalaz.Services.GameWorld.Model.Maps.Regions.MapRegion;

namespace Hagalaz.Services.GameWorld.Services
{
    /// <summary>
    /// Manages the regions within the game world.
    /// </summary>
    public class MapRegionService : IMapRegionService
    {
        private static readonly int[] _defaultXtea =
        [
            0, 0, 0, 0
        ];

        public const int MaxDimensions = byte.MaxValue;
        private readonly Dictionary<int, int[]> _xteaKeys = new();
        private readonly IDimension?[] _dimensions = new IDimension?[MaxDimensions];
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly IServiceScope _serviceScope;
        private readonly ILocationBuilder _locationBuilder;
        private readonly IGameObjectBuilder _gameObjectBuilder;
        private readonly IGroundItemBuilder _groundItemBuilder;
        private readonly ILogger<MapRegionService> _logger;

        public MapRegionService(
            IBackgroundTaskQueue taskQueue,
            IServiceProvider serviceProvider,
            ILocationBuilder locationBuilder,
            IGameObjectBuilder gameObjectBuilder,
            IGroundItemBuilder groundItemBuilder,
            ILogger<MapRegionService> logger)
        {
            CreateDimension(0); // create global world dimension.
            _taskQueue = taskQueue;
            _serviceScope = serviceProvider.CreateScope();
            _locationBuilder = locationBuilder;
            _gameObjectBuilder = gameObjectBuilder;
            _groundItemBuilder = groundItemBuilder;
            _logger = logger;
        }

        /// <summary>
        /// Does the test stuff.
        /// </summary>
        public async Task DoTestStuff()
        {
            // test code

            if (!TryCreateDimension(out var dimension))
            {
                return;
            }

            var lumbyloc = Location.Create(3222, 3222, 0, 0);
            var coolCoords = Location.Create(5312, 4800, 0, 0);
            //Location.Create(1952, 5716, 0, 0); // stealing creation
            var lumby = GetOrCreateMapRegion(lumbyloc.RegionId, 0, true);
            lumby.MakeDynamic();
            var coolRegion = GetOrCreateMapRegion(coolCoords.RegionId, 0, true);
            coolRegion.MakeStandard();

            for (var z = 0; z < 4; z++)
            {
                for (var xWrite = 0; xWrite < 8; xWrite++)
                {
                    for (var yWrite = 0; yWrite < 8; yWrite++)
                    {
                        var zombypart = coolRegion.GetRegionPartData(xWrite, yWrite, z);
                        lumby.WriteBlock(xWrite, yWrite, z, zombypart.DrawRegionPartX, zombypart.DrawRegionPartY, zombypart.DrawRegionZ);
                    }
                }
            }

            foreach (var character in lumby.FindAllCharacters())
            {
                await character.UpdateMapAsync(true);
            }
        }

        public bool IsAccessible(ILocation location) => ((int)GetClippingFlag(location.X, location.Y, location.Z) & 0x7fe40000) == 0;

        /// <summary>
        /// Gets the clipping flag.
        /// </summary>
        /// <param name="absX">The x.</param>
        /// <param name="absY">The y.</param>
        /// <param name="z">The z.</param>
        /// <returns></returns>
        public CollisionFlag GetClippingFlag(int absX, int absY, int z)
        {
            var regionId = absX >> 6 << 8 | absY >> 6;
            var mapRegion = GetOrCreateMapRegion(regionId, 0, false);
            return mapRegion.GetCollision(absX & 0x3F, absY & 0x3F, z);
        }

        /// <summary>
        /// Get's a map region by its Id.
        /// </summary>
        /// <param name="id">Region Id.</param>
        /// <param name="dimension">Dimension Id, 0 for global world.</param>
        /// <param name="create">Create region if not within the active regions.</param>
        /// <param name="resume">Resume region if it's suspended.</param>
        /// <returns>Returns the map region.</returns>
        /// <exception cref="Exception"></exception>
        public IMapRegion? GetMapRegion(int id, int dimension, bool create, bool resume)
        {
            var dim = _dimensions[dimension] ?? throw new Exception("'" + dimension + "' is not an existing dimension!");
            if (dim.Regions.TryGetValue(id, out var activeRegion))
            {
                return activeRegion;
            }

            if (dim.IdleRegions.TryGetValue(id, out var idleRegion))
            {
                if (!resume)
                {
                    return idleRegion;
                }

                dim.IdleRegions.Remove(id);
                idleRegion.Resume();
                dim.Regions.Add(id, idleRegion);
                _logger.LogDebug("Region[{id}] was resumed.", id);
                return idleRegion;
            }

            if (!create)
            {
                return null;
            }

            var baseLocation = _locationBuilder.Create().FromRegionId(id).Build();
            var region = new Regions_MapRegion(
                baseLocation,
                GetXtea(id),
                _serviceScope.ServiceProvider.GetRequiredService<INpcService>(),
                this,
                _gameObjectBuilder,
                _groundItemBuilder,
            dim.Regions.Add(id, region);
            return region;
        }

        public IMapRegion GetOrCreateMapRegion(int id, int dimension, bool resume) => GetMapRegion(id, dimension, true, resume)!;

        /// <summary>
        /// Creates the dynamic region.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="destination">The destination.</param>
        /// <returns></returns>
        public void CreateDynamicRegion(ILocation source, ILocation destination)
        {
            var standardRegion = GetOrCreateMapRegion(source.RegionId, source.Dimension, false);
            standardRegion.MakeStandard();
            var dynamicRegion = GetOrCreateMapRegion(destination.RegionId, destination.Dimension, false);
            dynamicRegion.MakeDynamic();

            for (var z = 0; z < 4; z++)
            {
                for (var xIndex = 0; xIndex < 8; xIndex++)
                {
                    for (var yIndex = 0; yIndex < 8; yIndex++)
                    {
                        var part = standardRegion.GetRegionPartData(xIndex, yIndex, z);
                        dynamicRegion.WriteBlock(xIndex, yIndex, z, part.DrawRegionPartX, part.DrawRegionPartY, part.DrawRegionZ);
                    }
                }
            }
        }

        /// <summary>
        /// Get's xtea of given region.
        /// </summary>
        /// <param name="regionID">The region's Id.</param>
        /// <returns>Returns the xtea keys.</returns>
        public int[] GetXtea(int regionID) => _xteaKeys.TryGetValue(regionID, out var value) ? value : _defaultXtea;

        /// <summary>
        /// Flag's specific clip to specific location,
        /// creates regions if needed.
        /// </summary>
        /// <param name="location">Location where clip should be flaged.</param>
        /// <param name="flag">The flag.</param>
        public void FlagCollision(ILocation location, CollisionFlag flag)
        {
            var region = GetOrCreateMapRegion(location.RegionId, location.Dimension, false);
            region.FlagCollision(location.RegionLocalX, location.RegionLocalY, location.Z, flag);
        }

        /// <summary>
        /// UnFlag's specific clip to specific location,
        /// creates regions if needed.
        /// </summary>
        /// <param name="location">Location where clip should be unflaged.</param>
        /// <param name="flag">The flag.</param>
        public void UnFlagCollision(ILocation location, CollisionFlag flag)
        {
            var region = GetOrCreateMapRegion(location.RegionId, location.Dimension, false);
            region.UnFlagCollision(location.RegionLocalX, location.RegionLocalY, location.Z, flag);
        }

        public void CreateDimension(int id)
        {
            if (_dimensions[id] != null) throw new Exception("Dimension already exists!");
            _dimensions[id] = new Dimension(id);
        }

        public bool TryCreateDimension([NotNullWhen(true)] out IDimension? dimension)
        {
            for (var i = 0; i < _dimensions.Length; i++)
            {
                if (_dimensions[i] != null)
                {
                    continue;
                }

                dimension = _dimensions[i] = new Dimension(i);
                return true;
            }

            dimension = default;
            return false;
        }

        public IEnumerable<IMapRegion> FindRegionsByDimension(int dimensionId) =>
            _dimensions[dimensionId] != null ? _dimensions[dimensionId]!.Regions.Values : [];

        public IEnumerable<IMapRegion> FindAllRegions() => FindAllDimensions().SelectMany(d => d.Regions.Values);
        public IEnumerable<IDimension> FindAllDimensions() => _dimensions.Where(dimension => dimension != null)!;

        public void RemoveDimension(IDimension dimension)
        {
            if (_dimensions[dimension.Id] == dimension)
            {
                _dimensions[dimension.Id] = null;
            }
        }

        public async Task LoadRegionAsync(IMapRegion region) =>
            await _taskQueue.QueueBackgroundWorkItemAsync(async (cancellationToken) =>
                await _serviceScope.ServiceProvider.GetRequiredService<IMapRegionLoader>().LoadAsync(region, cancellationToken));

        public IEnumerable<IMapRegion> GetMapRegionsWithinRange(ILocation location, bool create, bool resume, IMapSize mapSize)
        {
            var boundsSize = mapSize.Size >> 4;
            var partX = location.RegionX * 8 + 4; // middle of region
            var partY = location.RegionY * 8 + 4; // middle of region

            for (var regionX = (partX - boundsSize) / 8; regionX <= (partX + boundsSize) / 8; regionX++)
            {
                for (var regionY = (partY - boundsSize) / 8; regionY <= (partY + boundsSize) / 8; regionY++)
                {
                    var regionID = regionY + (regionX << 8);
                    var mr = GetMapRegion(regionID, location.Dimension, create, resume);
                    if (mr != null)
                    {
                        yield return mr;
                    }
                }
            }
        }
    }
}