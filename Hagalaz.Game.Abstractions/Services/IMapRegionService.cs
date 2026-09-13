using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Model.Maps.Updates;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that manages map regions and dimensions.
    /// </summary>
    public interface IMapRegionService
    {
        /// <summary>
        /// Finds an existing map region without creating or resuming it.
        /// </summary>
        /// <param name="id">The ID of the region.</param>
        /// <param name="dimension">The dimension ID (0 for the global world).</param>
        /// <returns>The existing <see cref="IMapRegion"/>; otherwise, <c>null</c>.</returns>
        IMapRegion? FindMapRegion(int id, int dimension);

        /// <summary>
        /// Gets an existing map region or creates a new one if it doesn't exist.
        /// </summary>
        /// <param name="id">The ID of the region.</param>
        /// <param name="dimension">The dimension ID (0 for the global world).</param>
        /// <returns>The existing or newly created <see cref="IMapRegion"/>.</returns>
        IMapRegion GetOrCreateMapRegion(int id, int dimension);

        IMapRegion AttachCharacter(ICharacter character);

        void DetachCharacter(ICharacter character, IMapRegion expectedRegion);

        IMapRegion AttachNpc(INpc npc);

        void DetachNpc(INpc npc, IMapRegion expectedRegion);

        void AddGroundItem(IGroundItem item);

        bool RemoveGroundItem(IGroundItem item);

        void AddGameObject(IGameObject gameObject);

        void RemoveGameObject(IGameObject gameObject);

        void FlagCollision(IGameObject gameObject);

        void UnFlagCollision(IGameObject gameObject);

        void QueueUpdate(IRegionPartUpdate update);

        /// <summary>
        /// Removes a region only when the active region is the expected instance.
        /// </summary>
        /// <param name="id">The ID of the region.</param>
        /// <param name="dimension">The dimension containing the region.</param>
        /// <param name="expectedRegion">The region instance that may be removed.</param>
        /// <returns><c>true</c> when the expected instance was removed; otherwise, <c>false</c>.</returns>
        bool TryRemoveMapRegion(int id, int dimension, IMapRegion expectedRegion);

        /// <summary>
        /// Moves the expected active region to idle ownership.
        /// </summary>
        bool TrySuspendMapRegion(IMapRegion expectedRegion);

        /// <summary>
        /// Removes an exact idle region before destruction.
        /// </summary>
        bool TryRemoveIdleMapRegion(int id, int dimension, IMapRegion expectedRegion);

        /// <summary>
        /// Checks whether the expected region instance is the current active region for its location.
        /// </summary>
        /// <param name="id">The region ID.</param>
        /// <param name="dimension">The dimension containing the region.</param>
        /// <param name="expectedRegion">The region instance to compare by reference.</param>
        /// <returns><c>true</c> when the expected instance is current; otherwise, <c>false</c>.</returns>
        bool IsCurrentMapRegion(int id, int dimension, IMapRegion expectedRegion);

        /// <summary>
        /// Gets all map regions within a certain range of a location, typically for a character's viewport.
        /// </summary>
        /// <param name="location">The central location.</param>
        /// <param name="mapSize">The size of the area to get regions for.</param>
        /// <returns>An enumerable collection of map regions within the specified range.</returns>
        IEnumerable<IMapRegion> GetMapRegionsWithinRange(ILocation location, IMapSize mapSize);

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
        IReadOnlyList<IMapRegion> FindRegionsByDimension(int dimensionId);

        /// <summary>
        /// Finds a snapshot of all idle regions within a specific dimension.
        /// </summary>
        /// <param name="dimensionId">The ID of the dimension.</param>
        /// <returns>A snapshot of idle regions in the specified dimension.</returns>
        IReadOnlyList<IMapRegion> FindIdleRegionsByDimension(int dimensionId);

        /// <summary>
        /// Finds all active regions across all dimensions.
        /// </summary>
        /// <returns>An enumerable collection of all active map regions.</returns>
        IReadOnlyList<IMapRegion> FindAllRegions();

        /// <summary>
        /// Finds one snapshot of all active regions that are ready for the major game tick.
        /// </summary>
        IReadOnlyList<IMapRegion> FindReadyRegions();

        /// <summary>
        /// Finds all active dimensions.
        /// </summary>
        /// <returns>An enumerable collection of all active dimensions.</returns>
        IReadOnlyList<IDimension> FindAllDimensions();

        /// <summary>
        /// Removes the exact dimension only when it is current and empty.
        /// </summary>
        /// <param name="expectedDimension">The exact dimension instance to remove.</param>
        bool TryRemoveEmptyDimension(IDimension expectedDimension);
    }
}
