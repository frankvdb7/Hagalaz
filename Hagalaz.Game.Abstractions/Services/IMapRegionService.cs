using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Maps;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// 
    /// </summary>
    public interface IMapRegionService
    {
        /// <summary>
        /// Get's a map region by its Id.
        /// </summary>
        /// <param name="id">Region Id.</param>
        /// <param name="dimension">Dimension Id, 0 for global world.</param>
        /// <param name="create">Create region if not within the active regions.</param>
        /// <param name="resume">Resume region if it's suspended.</param>
        /// <returns>Returns the map region.</returns>
        /// <exception cref="System.Exception"></exception>
        IMapRegion? GetMapRegion(int id, int dimension, bool create, bool resume);

        /// <summary>
        /// Gets or creates a map region by its Id.
        /// </summary>
        /// <param name="id">Region Id.</param>
        /// <param name="dimension">Dimension Id, 0 for global world.</param>
        /// <param name="create">Create region if not within the active regions.</param>
        /// <param name="resume">Resume region if it's suspended.</param>
        /// <returns>Returns the map region.</returns>
        /// <exception cref="System.Exception"></exception>
        public IMapRegion GetOrCreateMapRegion(int id, int dimension, bool resume);

        /// <summary>
        /// Gets the regions within a certain range.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="create"></param>
        /// <param name="resume"></param>
        /// <param name="mapSize"></param>
        /// <returns></returns>
        IEnumerable<IMapRegion> GetMapRegionsWithinRange(ILocation location, bool create, bool resume, IMapSize mapSize);

        /// <summary>
        /// Loads the specified region async.
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        public Task LoadRegionAsync(IMapRegion region);

        /// <summary>
        /// Get's xtea of given region.
        /// </summary>
        /// <param name="regionID">The region's Id.</param>
        /// <returns>Returns the xtea keys.</returns>
        int[] GetXtea(int regionID);

        /// <summary>
        /// Checks if the specified location is accessible.
        /// </summary>
        /// <param name="location">The location to check.</param>
        /// <returns>Returns true if the location is accessible; otherwise, false.</returns>
        bool IsAccessible(ILocation location);

        /// <summary>
        /// Gets the clipping flag.
        /// </summary>
        /// <param name="absX">The x.</param>
        /// <param name="absY">The y.</param>
        /// <param name="z">The z.</param>
        /// <returns></returns>
        CollisionFlag GetClippingFlag(int absX, int absY, int z);

        /// <summary>
        /// Flag's specific clip to specific location,
        /// creates regions if needed.
        /// </summary>
        /// <param name="location">Location where clip should be flaged.</param>
        /// <param name="flag">The flag.</param>
        void FlagCollision(ILocation location, CollisionFlag flag);

        /// <summary>
        /// UnFlag's specific clip to specific location,
        /// creates regions if needed.
        /// </summary>
        /// <param name="location">Location where clip should be unflaged.</param>
        /// <param name="flag">The flag.</param>
        void UnFlagCollision(ILocation location, CollisionFlag flag);

        /// <summary>
        /// Allocate's new dimesion.
        /// Return's false if dimension couldn't be allocated.
        /// </summary>
        bool TryCreateDimension([NotNullWhen(true)] out IDimension? dimension);

        /// <summary>
        /// Creates the dynamic region.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="destination">The destination.</param>
        /// <returns></returns>
        void CreateDynamicRegion(ILocation source, ILocation destination);

        /// <summary>
        /// Gets the active regions by dimension.
        /// </summary>
        /// <param name="dimensionId">The dimension identifier.</param>
        /// <returns></returns>
        IEnumerable<IMapRegion> FindRegionsByDimension(int dimensionId);

        /// <summary>
        /// Gets all the active regions.
        /// </summary>
        /// <returns></returns>
        IEnumerable<IMapRegion> FindAllRegions();

        /// <summary>
        /// Gets al the dimensions.
        /// </summary>
        /// <returns></returns>
        IEnumerable<IDimension> FindAllDimensions();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dimension"></param>
        void RemoveDimension(IDimension dimension);
    }
}