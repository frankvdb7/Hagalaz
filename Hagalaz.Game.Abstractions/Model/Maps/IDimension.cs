using System.Collections.Generic;

namespace Hagalaz.Game.Abstractions.Model.Maps
{
    /// <summary>
    /// Defines the contract for a dimension, which is a separate instance of the game world.
    /// The global world dimension ID is always 0.
    /// </summary>
    public interface IDimension
    {
        /// <summary>
        /// Gets the unique identifier for this dimension.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Gets a dictionary of the map regions that are currently active in this dimension.
        /// </summary>
        public IDictionary<int, IMapRegion> Regions { get; }

        /// <summary>
        /// Gets a dictionary of the map regions that are currently idle (not being processed) in this dimension.
        /// </summary>
        public IDictionary<int, IMapRegion> IdleRegions { get; }

        /// <summary>
        /// Determines whether this dimension can be destroyed (e.g., when it is empty).
        /// </summary>
        /// <returns><c>true</c> if the dimension can be destroyed; otherwise, <c>false</c>.</returns>
        public bool CanDestroy();
    }
}