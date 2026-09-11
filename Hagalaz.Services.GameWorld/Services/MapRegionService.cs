using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AutoMapper;
using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Builders.Location;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Model.Maps.Regions;
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
        private readonly Dimension?[] _dimensions = new Dimension?[MaxDimensions];
        private readonly IServiceScope _serviceScope;
        private readonly ILocationBuilder _locationBuilder;
        private readonly IGameObjectBuilder _gameObjectBuilder;
        private readonly IGroundItemBuilder _groundItemBuilder;
        private readonly ILogger<MapRegionService> _logger;
        private readonly IMapper _mapper;
        private readonly IMapRegionLoadScheduler _loadScheduler;
        public MapRegionService(
            IServiceProvider serviceProvider,
            ILocationBuilder locationBuilder,
            IGameObjectBuilder gameObjectBuilder,
            IGroundItemBuilder groundItemBuilder,
            ILogger<MapRegionService> logger,
            IMapper mapper,
            IMapRegionLoadScheduler loadScheduler)
        {
            CreateDimension(0); // create global world dimension.
            _serviceScope = serviceProvider.CreateScope();
            _locationBuilder = locationBuilder;
            _gameObjectBuilder = gameObjectBuilder;
            _groundItemBuilder = groundItemBuilder;
            _logger = logger;
            _mapper = mapper;
            _loadScheduler = loadScheduler;
        }

        /// <summary>
        /// Does the test stuff.
        /// </summary>
        public void DoTestStuff()
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
                        lumby.WriteBlock(xWrite, yWrite, z, zombypart.DrawRegionPartX, zombypart.DrawRegionPartY, zombypart.DrawRegionZ, zombypart.DrawRegionDimension);
                    }
                }
            }

            foreach (var character in lumby.FindAllCharacters())
            {
                character.UpdateMap(true);
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
            if (mapRegion.State != MapRegionState.Ready)
            {
                return CollisionFlag.FloorBlock;
            }

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
            IMapRegion result;
            var created = false;
            lock (dim.ResidencySyncRoot)
            {
                if (dim.ActiveRegions.TryGetValue(id, out var activeRegion))
                {
                    return activeRegion;
                }

                if (dim.IdleRegionStore.TryGetValue(id, out var idleRegion))
                {
                    if (!resume)
                    {
                        return idleRegion;
                    }

                    return ResumeIdleRegion(dim, id, idleRegion);
                }

                if (!create)
                {
                    return null;
                }
            }

            var newRegion = CreateMapRegion(id, dimension);
            lock (dim.ResidencySyncRoot)
            {
                if (!ReferenceEquals(_dimensions[dimension], dim))
                {
                    throw new InvalidOperationException($"Dimension[{dimension}] was removed while region[{id}] was being created.");
                }

                if (dim.ActiveRegions.TryGetValue(id, out var activeRegion))
                {
                    return activeRegion;
                }

                if (dim.IdleRegionStore.TryGetValue(id, out var idleRegion))
                {
                    return resume ? ResumeIdleRegion(dim, id, idleRegion) : idleRegion;
                }

                if (dim.ActiveRegions.TryGetValue(id, out var currentRegion))
                {
                    result = currentRegion;
                }
                else
                {
                    dim.ActiveRegions.Add(id, newRegion);
                    result = newRegion;
                    created = true;
                }
            }

            if (created)
            {
                _loadScheduler.RequestLoad(result);
            }

            return result;
        }

        public IMapRegion GetOrCreateMapRegion(int id, int dimension, bool resume) => GetMapRegion(id, dimension, true, resume)!;

        private IMapRegion CreateMapRegion(int id, int dimension)
        {
            var baseLocation = _locationBuilder.Create().FromRegionId(id).WithDimension(dimension).Build();
            return new Regions_MapRegion(
                baseLocation,
                GetXtea(id),
                _serviceScope.ServiceProvider.GetRequiredService<INpcService>(),
                this,
                _gameObjectBuilder,
                _groundItemBuilder,
                _mapper);
        }

        private IMapRegion ResumeIdleRegion(Dimension dimension, int id, IMapRegion idleRegion)
        {
            if (!dimension.IdleRegionStore.TryGetValue(id, out var currentIdleRegion)
                || !ReferenceEquals(currentIdleRegion, idleRegion)
                || !dimension.IdleRegionStore.Remove(id))
            {
                if (dimension.ActiveRegions.TryGetValue(id, out var currentRegion))
                {
                    return currentRegion;
                }

                throw new InvalidOperationException($"Idle region[{id}] lost canonical ownership before it could be resumed.");
            }

            idleRegion.Resume();
            dimension.ActiveRegions[id] = idleRegion;
            _logger.LogDebug("Region[{id}] was resumed.", id);
            return idleRegion;
        }

        public bool TryRemoveMapRegion(int id, int dimension, IMapRegion expectedRegion)
        {
            ArgumentNullException.ThrowIfNull(expectedRegion);

            var mapDimension = _dimensions[dimension];
            if (mapDimension is null)
            {
                return false;
            }

            lock (mapDimension.ResidencySyncRoot)
            {
                return ((ICollection<KeyValuePair<int, IMapRegion>>)mapDimension.ActiveRegions)
                    .Remove(new KeyValuePair<int, IMapRegion>(id, expectedRegion));
            }
        }

        public bool TrySuspendMapRegion(IMapRegion expectedRegion)
        {
            ArgumentNullException.ThrowIfNull(expectedRegion);

            var dimension = _dimensions[expectedRegion.BaseLocation.Dimension];
            if (dimension is null)
            {
                return false;
            }

            lock (dimension.ResidencySyncRoot)
            {
                if (!dimension.ActiveRegions.TryGetValue(expectedRegion.Id, out var currentRegion)
                    || !ReferenceEquals(currentRegion, expectedRegion)
                    || dimension.IdleRegionStore.ContainsKey(expectedRegion.Id))
                {
                    return false;
                }

                if (!dimension.ActiveRegions.Remove(expectedRegion.Id))
                {
                    return false;
                }

                expectedRegion.Suspend();
                dimension.IdleRegionStore[expectedRegion.Id] = expectedRegion;
                return true;
            }
        }

        public bool TryRemoveIdleMapRegion(int id, int dimension, IMapRegion expectedRegion)
        {
            ArgumentNullException.ThrowIfNull(expectedRegion);

            var mapDimension = _dimensions[dimension];
            if (mapDimension is null)
            {
                return false;
            }

            lock (mapDimension.ResidencySyncRoot)
            {
                return mapDimension.IdleRegionStore.TryGetValue(id, out var currentRegion)
                    && ReferenceEquals(currentRegion, expectedRegion)
                    && mapDimension.IdleRegionStore.Remove(id);
            }
        }

        public bool IsCurrentMapRegion(int id, int dimension, IMapRegion expectedRegion)
        {
            ArgumentNullException.ThrowIfNull(expectedRegion);

            var mapDimension = _dimensions[dimension];
            if (mapDimension is null)
            {
                return false;
            }

            lock (mapDimension.ResidencySyncRoot)
            {
                return mapDimension.ActiveRegions.TryGetValue(id, out var currentRegion)
                    && ReferenceEquals(currentRegion, expectedRegion);
            }
        }

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
                        dynamicRegion.WriteBlock(xIndex, yIndex, z, part.DrawRegionPartX, part.DrawRegionPartY, part.DrawRegionZ, part.DrawRegionDimension);
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

        public IReadOnlyList<IMapRegion> FindRegionsByDimension(int dimensionId)
        {
            var dimension = _dimensions[dimensionId];
            if (dimension is null)
            {
                return [];
            }

            lock (dimension.ResidencySyncRoot)
            {
                return dimension.ActiveRegions.Values.ToArray();
            }
        }

        public IReadOnlyList<IMapRegion> FindIdleRegionsByDimension(int dimensionId)
        {
            var dimension = _dimensions[dimensionId];
            if (dimension is null)
            {
                return [];
            }

            lock (dimension.ResidencySyncRoot)
            {
                return dimension.IdleRegionStore.Values.ToArray();
            }
        }

        public IReadOnlyList<IMapRegion> FindAllRegions()
        {
            var regions = new List<IMapRegion>();
            foreach (var dimension in _dimensions)
            {
                if (dimension is null)
                {
                    continue;
                }

                lock (dimension.ResidencySyncRoot)
                {
                    regions.AddRange(dimension.ActiveRegions.Values);
                }
            }

            return regions;
        }

        public IReadOnlyList<IDimension> FindAllDimensions() => _dimensions
            .Where(dimension => dimension != null)
            .Cast<IDimension>()
            .ToArray();

        public bool TryRemoveEmptyDimension(IDimension expectedDimension)
        {
            ArgumentNullException.ThrowIfNull(expectedDimension);

            if (expectedDimension is not Dimension dimension)
            {
                return false;
            }

            if (dimension.Id == 0 || dimension.Id < 0 || dimension.Id >= _dimensions.Length)
            {
                return false;
            }

            lock (dimension.ResidencySyncRoot)
            {
                if (!ReferenceEquals(_dimensions[dimension.Id], dimension)
                    || dimension.ActiveRegions.Count != 0
                    || dimension.IdleRegionStore.Count != 0)
                {
                    return false;
                }

                _dimensions[dimension.Id] = null;
                return true;
            }
        }

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
