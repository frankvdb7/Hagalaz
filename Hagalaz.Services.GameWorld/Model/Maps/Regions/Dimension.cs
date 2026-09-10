using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Hagalaz.Game.Abstractions.Model.Maps;

namespace Hagalaz.Services.GameWorld.Model.Maps.Regions
{
    /// <summary>
    /// Class for holding dimension regions and idle regions.
    /// Each dimension has it's Id.
    /// Global World dimension Id is always 0.
    /// </summary>
    public class Dimension : IDimension
    {
        private readonly ConcurrentDictionary<int, IMapRegion> _regions = new();
        private readonly ConcurrentDictionary<int, IMapRegion> _idleRegions = new();
        private readonly ReadOnlyDictionary<int, IMapRegion> _regionsView;
        private readonly ReadOnlyDictionary<int, IMapRegion> _idleRegionsView;
        internal object ResidencySyncRoot { get; } = new();

        /// <summary>
        /// Contains dimension Id.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Regions that are currently alive and in use.
        /// </summary>
        public IReadOnlyDictionary<int, IMapRegion> Regions => _regionsView;

        internal ConcurrentDictionary<int, IMapRegion> ActiveRegions => _regions;

        /// <summary>
        /// Regions that are currently idle.
        /// </summary>
        public IReadOnlyDictionary<int, IMapRegion> IdleRegions => _idleRegionsView;

        internal ConcurrentDictionary<int, IMapRegion> IdleRegionStore => _idleRegions;

        /// <summary>
        /// Constructs new dimension with given Id.
        /// </summary>
        /// <param name="id"></param>
        public Dimension(int id)
        {
            Id = id;
            _regionsView = new ReadOnlyDictionary<int, IMapRegion>(_regions);
            _idleRegionsView = new ReadOnlyDictionary<int, IMapRegion>(_idleRegions);
        }

        /// <summary>
        /// Determines whether this instance can be destroyed.
        /// </summary>
        /// <returns></returns>
        public bool CanDestroy()
        {
            if (Id == 0)
            {
                return false;
            }
            return _regions.Count <= 0 && _idleRegions.Count <= 0;
        }

    }
}
