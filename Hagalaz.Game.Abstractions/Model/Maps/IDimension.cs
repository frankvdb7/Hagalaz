using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Services;

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
        /// Gets the map regions currently active in this dimension for read/enumeration.
        /// Residency mutation is owned by <see cref="IMapRegionService"/>.
        /// </summary>
        public IReadOnlyDictionary<int, IMapRegion> Regions { get; }

        /// <summary>
        /// Gets the map regions currently idle in this dimension for read/enumeration.
        /// Residency mutation is owned by <see cref="IMapRegionService"/>.
        /// </summary>
        public IReadOnlyDictionary<int, IMapRegion> IdleRegions { get; }

        /// <summary>
        /// Determines whether this dimension can be destroyed (e.g., when it is empty).
        /// </summary>
        /// <returns><c>true</c> if the dimension can be destroyed; otherwise, <c>false</c>.</returns>
        public bool CanDestroy();
    }
}
