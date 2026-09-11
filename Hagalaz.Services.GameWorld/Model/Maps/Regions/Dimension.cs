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
        private readonly Dictionary<int, IMapRegion> _regions = new();
        private readonly Dictionary<int, IMapRegion> _idleRegions = new();
        internal object ResidencySyncRoot { get; } = new();

        /// <summary>
        /// Contains dimension Id.
        /// </summary>
        public int Id { get; }

        internal Dictionary<int, IMapRegion> ActiveRegions => _regions;

        internal Dictionary<int, IMapRegion> IdleRegionStore => _idleRegions;

        /// <summary>
        /// Constructs new dimension with given Id.
        /// </summary>
        /// <param name="id"></param>
        public Dimension(int id)
        {
            Id = id;
        }

    }
}
