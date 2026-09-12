using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AutoMapper;
using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Builders.Location;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Model.Maps.Updates;
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
        private readonly object _residencyGate = new();
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
            var mapRegion = FindMapRegion(regionId, 0);
            if (mapRegion is null)
            {
                return CollisionFlag.FloorBlock;
            }
            if (mapRegion.State != MapRegionState.Ready)
            {
                return CollisionFlag.FloorBlock;
            }

            return mapRegion.GetCollision(absX & 0x3F, absY & 0x3F, z);
        }

        /// <summary>
        /// Finds a map region by its Id without creating or resuming it.
        /// </summary>
        /// <param name="id">Region Id.</param>
        /// <param name="dimension">Dimension Id, 0 for global world.</param>
        /// <returns>Returns the active or idle map region, or <c>null</c> when it is not known.</returns>
        /// <exception cref="Exception"></exception>
        public IMapRegion? FindMapRegion(int id, int dimension)
        {
            lock (_residencyGate)
            {
                var dim = _dimensions[dimension] ?? throw new Exception("'" + dimension + "' is not an existing dimension!");
                if (dim.ActiveRegions.TryGetValue(id, out var activeRegion))
                {
                    return activeRegion;
                }

                return dim.IdleRegionStore.TryGetValue(id, out var idleRegion) ? idleRegion : null;
            }
        }

        public IMapRegion GetOrCreateMapRegion(int id, int dimension)
        {
            Dimension dim;
            lock (_residencyGate)
            {
                dim = _dimensions[dimension] ?? throw new Exception("'" + dimension + "' is not an existing dimension!");
                if (dim.ActiveRegions.TryGetValue(id, out var activeRegion))
                {
                    return activeRegion;
                }

                if (dim.IdleRegionStore.TryGetValue(id, out var idleRegion))
                {
                    return ResumeIdleRegion(dim, id, idleRegion);
                }
            }

            var newRegion = CreateMapRegion(id, dimension);
            lock (_residencyGate)
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
                    return ResumeIdleRegion(dim, id, idleRegion);
                }

                dim.ActiveRegions.Add(id, newRegion);
            }

            _loadScheduler.RequestLoad(newRegion);
            return newRegion;
        }

        public IMapRegion AttachCharacter(ICharacter character)
        {
            ArgumentNullException.ThrowIfNull(character);
            var location = character.Location;
            var requestedRegion = GetOrCreateMapRegion(location.RegionId, location.Dimension);

            lock (_residencyGate)
            {
                var dimension = _dimensions[location.Dimension] ?? throw new InvalidOperationException($"Dimension[{location.Dimension}] no longer exists.");
                var region = ResolveActiveRegionForMutation(dimension, requestedRegion.Id);
                region.Add(character);
                return region;
            }
        }

        public void DetachCharacter(ICharacter character, IMapRegion expectedRegion)
        {
            ArgumentNullException.ThrowIfNull(character);
            ArgumentNullException.ThrowIfNull(expectedRegion);

            lock (_residencyGate)
            {
                if (IsCanonicalRegion(expectedRegion))
                {
                    expectedRegion.Remove(character);
                }
            }
        }

        public IMapRegion AttachNpc(INpc npc)
        {
            ArgumentNullException.ThrowIfNull(npc);
            var canSuspend = npc.CanSuspend();
            var location = npc.Location;
            var requestedRegion = GetOrCreateMapRegion(location.RegionId, location.Dimension);

            lock (_residencyGate)
            {
                var dimension = _dimensions[location.Dimension] ?? throw new InvalidOperationException($"Dimension[{location.Dimension}] no longer exists.");
                var region = ResolveActiveRegionForMutation(dimension, requestedRegion.Id);
                region.Add(npc, canSuspend);
                return region;
            }
        }

        public void DetachNpc(INpc npc, IMapRegion expectedRegion)
        {
            ArgumentNullException.ThrowIfNull(npc);
            ArgumentNullException.ThrowIfNull(expectedRegion);

            lock (_residencyGate)
            {
                if (IsCanonicalRegion(expectedRegion))
                {
                    expectedRegion.Remove(npc);
                }
            }
        }

        public void AddGroundItem(IGroundItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            // Item lifecycle callbacks belong to the GameWorker boundary and
            // must not run while the residency gate is held.
            var region = GetOrCreateMapRegion(item.Location.RegionId, item.Location.Dimension);
            region.Add(item);
        }

        public bool RemoveGroundItem(IGroundItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            // Removal may destroy or schedule the item, so keep it outside
            // the residency gate on the serialized GameWorker boundary.
            var region = GetOrCreateMapRegion(item.Location.RegionId, item.Location.Dimension);
            return region.Remove(item);
        }

        public void AddGameObject(IGameObject gameObject)
        {
            ArgumentNullException.ThrowIfNull(gameObject);
            // Object lifecycle callbacks belong to the GameWorker boundary
            // and must not run while the residency gate is held.
            var region = GetOrCreateMapRegion(gameObject.Location.RegionId, gameObject.Location.Dimension);
            region.Add(gameObject);
        }

        public void RemoveGameObject(IGameObject gameObject)
        {
            ArgumentNullException.ThrowIfNull(gameObject);
            // Removal may invoke object lifecycle code; the GameWorker owns
            // this callback boundary rather than the residency gate.
            var region = GetOrCreateMapRegion(gameObject.Location.RegionId, gameObject.Location.Dimension);
            region.Remove(gameObject);
        }

        public void FlagCollision(IGameObject gameObject)
        {
            ArgumentNullException.ThrowIfNull(gameObject);
            var region = GetOrCreateMapRegion(gameObject.Location.RegionId, gameObject.Location.Dimension);

            lock (_residencyGate)
            {
                var dimension = _dimensions[gameObject.Location.Dimension] ?? throw new InvalidOperationException($"Dimension[{gameObject.Location.Dimension}] no longer exists.");
                ResolveActiveRegionForMutation(dimension, region.Id).FlagCollision(gameObject);
            }
        }

        public void UnFlagCollision(IGameObject gameObject)
        {
            ArgumentNullException.ThrowIfNull(gameObject);
            var region = GetOrCreateMapRegion(gameObject.Location.RegionId, gameObject.Location.Dimension);

            lock (_residencyGate)
            {
                var dimension = _dimensions[gameObject.Location.Dimension] ?? throw new InvalidOperationException($"Dimension[{gameObject.Location.Dimension}] no longer exists.");
                ResolveActiveRegionForMutation(dimension, region.Id).UnFlagCollision(gameObject);
            }
        }

        public void QueueUpdate(IRegionPartUpdate update)
        {
            ArgumentNullException.ThrowIfNull(update);
            var region = GetOrCreateMapRegion(update.Location.RegionId, update.Location.Dimension);

            lock (_residencyGate)
            {
                var dimension = _dimensions[update.Location.Dimension] ?? throw new InvalidOperationException($"Dimension[{update.Location.Dimension}] no longer exists.");
                ResolveActiveRegionForMutation(dimension, region.Id).QueueUpdate(update);
            }
        }

        private IMapRegion ResolveActiveRegionForMutation(Dimension dimension, int id)
        {
            if (dimension.ActiveRegions.TryGetValue(id, out var activeRegion))
            {
                return activeRegion;
            }

            if (dimension.IdleRegionStore.TryGetValue(id, out var idleRegion))
            {
                return ResumeIdleRegion(dimension, id, idleRegion);
            }

            throw new InvalidOperationException($"Region[{id}] is no longer resident.");
        }

        private bool IsCanonicalRegion(IMapRegion expectedRegion)
        {
            var dimension = _dimensions[expectedRegion.BaseLocation.Dimension];
            return dimension is not null
                && ((dimension.ActiveRegions.TryGetValue(expectedRegion.Id, out var activeRegion) && ReferenceEquals(activeRegion, expectedRegion))
                    || (dimension.IdleRegionStore.TryGetValue(expectedRegion.Id, out var idleRegion) && ReferenceEquals(idleRegion, expectedRegion)));
        }

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

            lock (_residencyGate)
            {
                var mapDimension = _dimensions[dimension];
                if (mapDimension is null)
                {
                    return false;
                }

                return ((ICollection<KeyValuePair<int, IMapRegion>>)mapDimension.ActiveRegions)
                    .Remove(new KeyValuePair<int, IMapRegion>(id, expectedRegion));
            }
        }

        public bool TrySuspendMapRegion(IMapRegion expectedRegion)
        {
            ArgumentNullException.ThrowIfNull(expectedRegion);

            // Script-backed eligibility is evaluated at the GameWorker
            // boundary, before taking the residency gate. Membership changes
            // are committed by the same owner and are checked structurally
            // below before the active-to-idle transfer.
            if (!expectedRegion.CanSuspend())
            {
                return false;
            }

            lock (_residencyGate)
            {
                var dimension = _dimensions[expectedRegion.BaseLocation.Dimension];
                if (dimension is null)
                {
                    return false;
                }

                if (!dimension.ActiveRegions.TryGetValue(expectedRegion.Id, out var currentRegion)
                    || !ReferenceEquals(currentRegion, expectedRegion)
                    || dimension.IdleRegionStore.ContainsKey(expectedRegion.Id)
                    || expectedRegion.FindAllCharacters().Any()
                    || expectedRegion.HasNonSuspendableNpcs)
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

            lock (_residencyGate)
            {
                var mapDimension = _dimensions[dimension];
                if (mapDimension is null)
                {
                    return false;
                }

                return mapDimension.IdleRegionStore.TryGetValue(id, out var currentRegion)
                    && ReferenceEquals(currentRegion, expectedRegion)
                    && mapDimension.IdleRegionStore.Remove(id);
            }
        }

        public bool IsCurrentMapRegion(int id, int dimension, IMapRegion expectedRegion)
        {
            ArgumentNullException.ThrowIfNull(expectedRegion);

            lock (_residencyGate)
            {
                var mapDimension = _dimensions[dimension];
                if (mapDimension is null)
                {
                    return false;
                }

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
            var requestedStandardRegion = GetOrCreateMapRegion(source.RegionId, source.Dimension);
            var requestedDynamicRegion = GetOrCreateMapRegion(destination.RegionId, destination.Dimension);
            IMapRegion standardRegion;
            IMapRegion dynamicRegion;

            lock (_residencyGate)
            {
                var sourceDimension = _dimensions[source.Dimension] ?? throw new InvalidOperationException($"Dimension[{source.Dimension}] no longer exists.");
                var destinationDimension = _dimensions[destination.Dimension] ?? throw new InvalidOperationException($"Dimension[{destination.Dimension}] no longer exists.");
                standardRegion = ResolveActiveRegionForMutation(sourceDimension, requestedStandardRegion.Id);
                dynamicRegion = ResolveActiveRegionForMutation(destinationDimension, requestedDynamicRegion.Id);
                standardRegion.MakeStandard();
                dynamicRegion.MakeDynamic();
            }

            // Dynamic block population can invoke object scripts while loading
            // copied objects. It runs on the serialized GameWorker boundary,
            // after which housekeeping is allowed to inspect the region.
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
            var region = GetOrCreateMapRegion(location.RegionId, location.Dimension);
            lock (_residencyGate)
            {
                var dimension = _dimensions[location.Dimension] ?? throw new InvalidOperationException($"Dimension[{location.Dimension}] no longer exists.");
                ResolveActiveRegionForMutation(dimension, region.Id)
                    .FlagCollision(location.RegionLocalX, location.RegionLocalY, location.Z, flag);
            }
        }

        /// <summary>
        /// UnFlag's specific clip to specific location,
        /// creates regions if needed.
        /// </summary>
        /// <param name="location">Location where clip should be unflaged.</param>
        /// <param name="flag">The flag.</param>
        public void UnFlagCollision(ILocation location, CollisionFlag flag)
        {
            var region = GetOrCreateMapRegion(location.RegionId, location.Dimension);
            lock (_residencyGate)
            {
                var dimension = _dimensions[location.Dimension] ?? throw new InvalidOperationException($"Dimension[{location.Dimension}] no longer exists.");
                ResolveActiveRegionForMutation(dimension, region.Id)
                    .UnFlagCollision(location.RegionLocalX, location.RegionLocalY, location.Z, flag);
            }
        }

        public void CreateDimension(int id)
        {
            lock (_residencyGate)
            {
                if (_dimensions[id] != null) throw new Exception("Dimension already exists!");
                _dimensions[id] = new Dimension(id);
            }
        }

        public bool TryCreateDimension([NotNullWhen(true)] out IDimension? dimension)
        {
            lock (_residencyGate)
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
        }

        public IReadOnlyList<IMapRegion> FindRegionsByDimension(int dimensionId)
        {
            lock (_residencyGate)
            {
                var dimension = _dimensions[dimensionId];
                return dimension is null ? [] : dimension.ActiveRegions.Values.ToArray();
            }
        }

        public IReadOnlyList<IMapRegion> FindIdleRegionsByDimension(int dimensionId)
        {
            lock (_residencyGate)
            {
                var dimension = _dimensions[dimensionId];
                return dimension is null ? [] : dimension.IdleRegionStore.Values.ToArray();
            }
        }

        public IReadOnlyList<IMapRegion> FindAllRegions()
        {
            lock (_residencyGate)
            {
                var regions = new List<IMapRegion>();
                foreach (var dimension in _dimensions)
                {
                    if (dimension is not null)
                    {
                        regions.AddRange(dimension.ActiveRegions.Values);
                    }
                }

                return regions;
            }
        }

        public IReadOnlyList<IMapRegion> FindReadyRegions()
        {
            lock (_residencyGate)
            {
                var regions = new List<IMapRegion>();
                foreach (var dimension in _dimensions)
                {
                    if (dimension is null)
                    {
                        continue;
                    }

                    regions.AddRange(dimension.ActiveRegions.Values.Where(region => region.State == MapRegionState.Ready));
                }

                return regions;
            }
        }

        public IReadOnlyList<IDimension> FindAllDimensions()
        {
            lock (_residencyGate)
            {
                return _dimensions.Where(dimension => dimension != null).Cast<IDimension>().ToArray();
            }
        }

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

            lock (_residencyGate)
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

        public IEnumerable<IMapRegion> GetMapRegionsWithinRange(ILocation location, IMapSize mapSize)
        {
            var boundsSize = mapSize.Size >> 4;
            var partX = location.RegionX * 8 + 4; // middle of region
            var partY = location.RegionY * 8 + 4; // middle of region

            for (var regionX = (partX - boundsSize) / 8; regionX <= (partX + boundsSize) / 8; regionX++)
            {
                for (var regionY = (partY - boundsSize) / 8; regionY <= (partY + boundsSize) / 8; regionY++)
                {
                    var regionID = regionY + (regionX << 8);
                    yield return GetOrCreateMapRegion(regionID, location.Dimension);
                }
            }
        }
    }
}
