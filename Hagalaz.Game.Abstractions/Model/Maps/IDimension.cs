using System.Collections.Generic;

namespace Hagalaz.Game.Abstractions.Model.Maps
{
    /// <summary>
    /// Class for holding dimension regions and idle regions.
    /// Each dimension has it's Id.
    /// Global World dimension Id is always 0.
    /// </summary>
    public interface IDimension
    {
        /// <summary>
        /// Contains dimension Id.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Regions that are currently alive and in use.
        /// </summary>
        public IDictionary<int, IMapRegion> Regions { get; }

        /// <summary>
        /// Regions that are currently idle.
        /// </summary>
        public IDictionary<int, IMapRegion> IdleRegions { get; }

        /// <summary>
        /// Determines whether this instance can be destroyed.
        /// </summary>
        /// <returns></returns>
        public bool CanDestroy();
    }
}
