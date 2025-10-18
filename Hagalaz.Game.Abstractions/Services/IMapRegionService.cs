using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Maps;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that manages map regions and dimensions.
    /// </summary>
    public interface IMapRegionService
    {
        /// <summary>
        /// Gets a map region by its ID and dimension.
        /// </summary>
        /// <param name="id">The ID of the region.</param>
        /// <param name="dimension">The dimension ID (0 for the global world).</param>
        /// <param name="create">If set to <c>true</c>, creates the region if it does not exist.</param>
        /// <param name="resume">If set to <c>true</c>, resumes the region if it is suspended.</param>
        /// <returns>The <see cref="IMapRegion"/> if found or created; otherwise, <c>null</c>.</returns>
        IMapRegion? GetMapRegion(int id, int dimension, bool create, bool resume);

        /// <summary>
        /// Gets an existing map region or creates a new one if it doesn't exist.
        /// </summary>
        /// <param name="id">The ID of the region.</param>
        /// <param name="dimension">The dimension ID (0 for the global world).</param>
        /// <param name="resume">If set to <c>true</c>, resumes the region if it is suspended.</param>
        /// <returns>The existing or newly created <see cref="IMapRegion"/>.</returns>
        public IMapRegion GetOrCreateMapRegion(int id, int dimension, bool resume);

        /// <summary>
        /// Gets all map regions within a certain range of a location, typically for a character's viewport.
        /// </summary>
        /// <param name="location">The central location.</param>
        /// <param name="create">If set to <c>true</c>, creates any regions within range that do not exist.</param>
        /// <param name="resume">If set to <c>true</c>, resumes any regions within range that are suspended.</param>
        /// <param name="mapSize">The size of the area to get regions for.</param>
        /// <returns>An enumerable collection of map regions within the specified range.</returns>
        IEnumerable<IMapRegion> GetMapRegionsWithinRange(ILocation location, bool create, bool resume, IMapSize mapSize);

        /// <summary>
        /// Asynchronously loads the data for a specific region.
        /// </summary>
        /// <param name="region">The region to load.</param>
        /// <returns>A task that represents the asynchronous loading operation.</returns>
        public Task LoadRegionAsync(IMapRegion region);

        /// <summary>
        /// Gets the XTEA keys for a given region, used for decrypting map data.
        /// </summary>
        /// <param name="regionID">The ID of the region.</param>
        /// <returns>An array of four integer keys.</returns>
        int[] GetXtea(int regionID);

        /// <summary>
        /// Checks if a specific location is accessible (i.e., not blocked by impassable terrain or objects).
        /// </summary>
        /// <param name="location">The location to check.</param>
        /// <returns><c>true</c> if the location is accessible; otherwise, <c>false</c>.</returns>
        bool IsAccessible(ILocation location);

        /// <summary>
        /// Gets the collision flag for a specific tile in the world.
        /// </summary>
        /// <param name="absX">The absolute X-coordinate of the tile.</param>
        /// <param name="absY">The absolute Y-coordinate of the tile.</param>
        /// <param name="z">The plane (height level) of the tile.</param>
        /// <returns>The <see cref="CollisionFlag"/> for the specified tile.</returns>
        CollisionFlag GetClippingFlag(int absX, int absY, int z);

        /// <summary>
        /// Adds a collision flag to a specific tile, creating the region if necessary.
        /// </summary>
        /// <param name="location">The location of the tile.</param>
        /// <param name="flag">The collision flag to add.</param>
        void FlagCollision(ILocation location, CollisionFlag flag);

        /// <summary>
        /// Removes a collision flag from a specific tile, creating the region if necessary.
        /// </summary>
        /// <param name="location">The location of the tile.</param>
        /// <param name="flag">The collision flag to remove.</param>
        void UnFlagCollision(ILocation location, CollisionFlag flag);

        /// <summary>
        /// Attempts to allocate a new, unused dimension.
        /// </summary>
        /// <param name="dimension">When this method returns, contains the newly created dimension if successful; otherwise, null.</param>
        /// <returns><c>true</c> if a new dimension was successfully created; otherwise, <c>false</c>.</returns>
        bool TryCreateDimension([NotNullWhen(true)] out IDimension? dimension);

        /// <summary>
        /// Creates a dynamic region by copying map data from a source location to a destination location.
        /// </summary>
        /// <param name="source">The source location to copy from.</param>
        /// <param name="destination">The destination location to copy to.</param>
        void CreateDynamicRegion(ILocation source, ILocation destination);

        /// <summary>
        /// Finds all active regions within a specific dimension.
        /// </summary>
        /// <param name="dimensionId">The ID of the dimension.</param>
        /// <returns>An enumerable collection of map regions in the specified dimension.</returns>
        IEnumerable<IMapRegion> FindRegionsByDimension(int dimensionId);

        /// <summary>
        /// Finds all active regions across all dimensions.
        /// </summary>
        /// <returns>An enumerable collection of all active map regions.</returns>
        IEnumerable<IMapRegion> FindAllRegions();

        /// <summary>
        /// Finds all active dimensions.
        /// </summary>
        /// <returns>An enumerable collection of all active dimensions.</returns>
        IEnumerable<IDimension> FindAllDimensions();

        /// <summary>
        /// Removes a dimension and all its associated regions from the world.
        /// </summary>
        /// <param name="dimension">The dimension to remove.</param>
        void RemoveDimension(IDimension dimension);
    }
}