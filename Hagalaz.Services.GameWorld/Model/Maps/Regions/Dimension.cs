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
        /// <summary>
        /// Contains dimension Id.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Regions that are currently alive and in use.
        /// </summary>
        public IDictionary<int, IMapRegion> Regions { get; } = new ConcurrentDictionary<int, IMapRegion>();

        /// <summary>
        /// Regions that are currently idle.
        /// </summary>
        public IDictionary<int, IMapRegion> IdleRegions { get; } = new ConcurrentDictionary<int, IMapRegion>();

        /// <summary>
        /// Constructs new dimension with given Id.
        /// </summary>
        /// <param name="id"></param>
        public Dimension(int id) => Id = id;

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
            return Regions.Count <= 0 && IdleRegions.Count <= 0;
        }
    }
}