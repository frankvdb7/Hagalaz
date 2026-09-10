using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private readonly ConcurrentDictionary<int, IMapRegion> _pendingDestructionRegions = new();
        private readonly IReadOnlyDictionary<int, IMapRegion> _regionsView;
        private readonly IReadOnlyDictionary<int, IMapRegion> _idleRegionsView;
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

        internal ConcurrentDictionary<int, IMapRegion> PendingDestructionRegionStore => _pendingDestructionRegions;

        /// <summary>
        /// Constructs new dimension with given Id.
        /// </summary>
        /// <param name="id"></param>
        public Dimension(int id)
        {
            Id = id;
            _regionsView = new ReadOnlyDictionaryView(_regions);
            _idleRegionsView = new ReadOnlyDictionaryView(_idleRegions);
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
            return _regions.Count <= 0 && _idleRegions.Count <= 0 && _pendingDestructionRegions.Count <= 0;
        }

        private sealed class ReadOnlyDictionaryView(ConcurrentDictionary<int, IMapRegion> source)
            : IReadOnlyDictionary<int, IMapRegion>
        {
            public IMapRegion this[int key] => source[key];
            public IEnumerable<int> Keys => source.Keys;
            public IEnumerable<IMapRegion> Values => source.Values;
            public int Count => source.Count;
            public bool ContainsKey(int key) => source.ContainsKey(key);
            public bool TryGetValue(int key, out IMapRegion value) => source.TryGetValue(key, out value!);
            public IEnumerator<KeyValuePair<int, IMapRegion>> GetEnumerator() => source.GetEnumerator();
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
