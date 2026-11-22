using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps.Updates;

namespace Hagalaz.Game.Abstractions.Model.Maps
{
    /// <summary>
    /// Defines the contract for a map region, which is a 64x64 section of the game world.
    /// </summary>
    public interface IMapRegion
    {
        /// <summary>
        /// Gets the region's unique identifier.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// Gets the XTEA keys used to decrypt this map region's data.
        /// </summary>
        int[] XteaKeys { get; }

        /// <summary>
        /// Gets the base location (bottom-left corner) of the region.
        /// </summary>
        ILocation BaseLocation { get; }

        /// <summary>
        /// Gets the size of the region.
        /// </summary>
        IVector3 Size { get; }

        /// <summary>
        /// Gets a value indicating whether this region is dynamic (i.e., has been modified from its base state).
        /// </summary>
        bool IsDynamic { get; }

        /// <summary>
        /// Gets the initial-load lifecycle state of this region instance.
        /// </summary>
        MapRegionState State { get; }

        /// <summary>
        /// Gets a value indicating whether an NPC attached through the region
        /// ownership boundary cannot be suspended.
        /// </summary>
        bool HasNonSuspendableNpcs { get; }

        /// <summary>
        /// Checks if this region can be destroyed (e.g., when it is empty).
        /// </summary>
        /// <returns><c>true</c> if the region can be destroyed; otherwise, <c>false</c>.</returns>
        bool CanDestroy();

        /// <summary>
        /// Checks if this region can be suspended (made idle) to save resources.
        /// </summary>
        /// <returns><c>true</c> if the region can be suspended; otherwise, <c>false</c>.</returns>
        bool CanSuspend();

        /// <summary>
        /// Publishes this region as ready after its initial data and population are complete.
        /// </summary>
        void MarkReady();

        /// <summary>
        /// Permanently discards this region after an initial-load failure or cancellation.
        /// </summary>
        void MarkDiscarded();

        /// <summary>
        /// Resumes processing for a suspended (idle) region.
        /// </summary>
        void Resume();

        /// <summary>
        /// Suspends processing for this region, making it idle.
        /// </summary>
        void Suspend();

        /// <summary>
        /// Destroys the region-local resources after the region has been removed from the game world.
        /// </summary>
        void Destroy();

        /// <summary>
        /// Finds all characters currently in this region.
        /// </summary>
        /// <returns>An enumerable collection of characters.</returns>
        IEnumerable<ICharacter> FindAllCharacters();

        /// <summary>
        /// Finds all NPCs currently in this region.
        /// </summary>
        /// <returns>An enumerable collection of NPCs.</returns>
        IEnumerable<INpc> FindAllNpcs();

        /// <summary>
        /// Finds all ground items currently in this region.
        /// </summary>
        /// <returns>An enumerable collection of ground items.</returns>
        IEnumerable<IGroundItem> FindAllGroundItems();

        /// <summary>
        /// Finds all game objects currently in this region.
        /// </summary>
        /// <returns>An enumerable collection of game objects.</returns>
        IEnumerable<IGameObject> FindAllGameObjects();

        /// <summary>
        /// Maps every part to itself and rebuilds objects and floor collision from the region's own template.
        /// </summary>
        /// <param name="standardTemplate">The template of this region.</param>
        void MakeStandard(MapRegionTemplate standardTemplate);

        /// <summary>
        /// Marks this region as dynamic, indicating it has been modified.
        /// </summary>
        void MakeDynamic();

        /// <summary>
        /// Adds an NPC to the region.
        /// </summary>
        /// <param name="npc">The NPC to add.</param>
        void Add(INpc npc);

        /// <summary>
        /// Adds an NPC when its suspension eligibility has already been
        /// evaluated outside the map-region residency gate.
        /// </summary>
        /// <param name="npc">The NPC to add.</param>
        /// <param name="canSuspend">The eligibility observed by the owner.</param>
        void Add(INpc npc, bool canSuspend);

        /// <summary>
        /// Adds a character to the region.
        /// </summary>
        /// <param name="character">The character to add.</param>
        void Add(ICharacter character);

        /// <summary>
        /// Adds a ground item to the region.
        /// </summary>
        /// <param name="item">The ground item to add.</param>
        void Add(IGroundItem item);

        /// <summary>
        /// Adds a game object to the region.
        /// </summary>
        /// <param name="gameObj">The game object to add.</param>
        void Add(IGameObject gameObj);

        /// <summary>
        /// Removes a character from the region.
        /// </summary>
        /// <param name="character">The character to remove.</param>
        void Remove(ICharacter character);

        /// <summary>
        /// Removes an NPC from the region.
        /// </summary>
        /// <param name="npc">The NPC to remove.</param>
        void Remove(INpc npc);

        /// <summary>
        /// Removes a game object from the region.
        /// </summary>
        /// <param name="gameObj">The game object to remove.</param>
        void Remove(IGameObject gameObj);

        /// <summary>
        /// Removes a ground item from the region.
        /// </summary>
        /// <param name="item">The ground item to remove.</param>
        /// <returns><c>true</c> when the exact ground-item instance was removed; otherwise, <c>false</c>.</returns>
        bool Remove(IGroundItem item);

        /// <summary>
        /// Applies a collision flag to a specific tile in the region.
        /// </summary>
        /// <param name="localX">The local X-coordinate within the region (0-63).</param>
        /// <param name="localY">The local Y-coordinate within the region (0-63).</param>
        /// <param name="z">The plane (height level).</param>
        /// <param name="flag">The collision flag to add.</param>
        void FlagCollision(int localX, int localY, int z, CollisionFlag flag);

        /// <summary>
        /// Removes a collision flag from a specific tile in the region.
        /// </summary>
        /// <param name="localX">The local X-coordinate within the region (0-63).</param>
        /// <param name="localY">The local Y-coordinate within the region (0-63).</param>
        /// <param name="z">The plane (height level).</param>
        /// <param name="flag">The collision flag to remove.</param>
        void UnFlagCollision(int localX, int localY, int z, CollisionFlag flag);

        /// <summary>
        /// Removes the collision flags associated with a specific game object.
        /// </summary>
        /// <param name="gameObject">The game object.</param>
        void UnFlagCollision(IGameObject gameObject);

        /// <summary>
        /// Adds the collision flags associated with a specific game object.
        /// </summary>
        /// <param name="gameObject">The game object.</param>
        void FlagCollision(IGameObject gameObject);

        /// <summary>
        /// Performs the main game logic update tick for the region.
        /// </summary>
        void MajorUpdateTick();

        /// <summary>
        /// Prepares the data for the client update tick.
        /// </summary>
        void MajorClientPrepareUpdateTick();

        /// <summary>
        /// Performs the main client update tick for the region.
        /// </summary>
        /// <param name="characters">The character view captured for this client update.</param>
        void MajorClientUpdateTick(IReadOnlyDictionary<int, ICharacter> characters);

        /// <summary>
        /// Resets the update flags for the region after a client update.
        /// </summary>
        void MajorClientUpdateResetTick();

        /// <summary>
        /// Sends all pending zone updates for this region to a specific character.
        /// </summary>
        /// <param name="character">The character to send the updates to.</param>
        void SendFullPartUpdates(ICharacter character);

        /// <summary>
        /// Gets the 8x8 part at absolute part coordinates (absolute tile / 8).
        /// </summary>
        /// <param name="partX">The absolute part X-coordinate.</param>
        /// <param name="partY">The absolute part Y-coordinate.</param>
        /// <param name="z">The plane (height level).</param>
        /// <returns>The map region part data.</returns>
        IMapRegionPart GetRegionPartByAbsolutePart(int partX, int partY, int z);

        /// <summary>
        /// Gets the 8x8 part at a local part index of this region.
        /// </summary>
        /// <param name="localPartX">The local part X index (0-7).</param>
        /// <param name="localPartY">The local part Y index (0-7).</param>
        /// <param name="z">The plane (height level).</param>
        /// <returns>The map region part data.</returns>
        IMapRegionPart GetRegionPartByLocalPart(int localPartX, int localPartY, int z);

        /// <summary>
        /// Maps a local part of this dynamic region to a source part and rebuilds its
        /// objects and floor collision from the source region's template.
        /// </summary>
        /// <param name="localPartX">The local part X index (0-7).</param>
        /// <param name="localPartY">The local part Y index (0-7).</param>
        /// <param name="z">The destination plane.</param>
        /// <param name="sourcePartX">The absolute source part X-coordinate.</param>
        /// <param name="sourcePartY">The absolute source part Y-coordinate.</param>
        /// <param name="sourceZ">The source plane.</param>
        /// <param name="sourceTemplate">The template of the source region.</param>
        void WriteBlockByIndex(int localPartX, int localPartY, int z, int sourcePartX, int sourcePartY, int sourceZ, MapRegionTemplate sourceTemplate);

        /// <summary>
        /// Rotates a local part of this dynamic region and rebuilds it from its source region's template.
        /// </summary>
        /// <param name="localPartX">The local part X index (0-7).</param>
        /// <param name="localPartY">The local part Y index (0-7).</param>
        /// <param name="z">The destination plane.</param>
        /// <param name="rotation">The part rotation (0-3).</param>
        /// <param name="sourceTemplate">The template of the part's source region.</param>
        void RotateBlockByIndex(int localPartX, int localPartY, int z, int rotation, MapRegionTemplate sourceTemplate);

        /// <summary>
        /// Queues an update for a part of this map region.
        /// </summary>
        /// <param name="update">The region part update to queue.</param>
        void QueueUpdate(IRegionPartUpdate update);

        /// <summary>
        /// Finds a standard game object at a specific location in the region.
        /// </summary>
        /// <param name="localX">The local X-coordinate within the region (0-63).</param>
        /// <param name="localY">The local Y-coordinate within the region (0-63).</param>
        /// <param name="z">The plane (height level).</param>
        /// <returns>The game object if found; otherwise, <c>null</c>.</returns>
        IGameObject? FindStandardGameObject(int localX, int localY, int z);

        /// <summary>
        /// Finds all game objects at a specific location in the region.
        /// </summary>
        /// <param name="localX">The local X-coordinate within the region (0-63).</param>
        /// <param name="localY">The local Y-coordinate within the region (0-63).</param>
        /// <param name="z">The plane (height level).</param>
        /// <returns>An enumerable collection of game objects at the location.</returns>
        IEnumerable<IGameObject> FindGameObjects(int localX, int localY, int z);

        /// <summary>
        /// Gets the collision flags for a specific tile in the region.
        /// </summary>
        /// <param name="localX">The local X-coordinate within the region (0-63).</param>
        /// <param name="localY">The local Y-coordinate within the region (0-63).</param>
        /// <param name="z">The plane (height level).</param>
        /// <returns>The collision flags for the tile.</returns>
        CollisionFlag GetCollision(int localX, int localY, int z);
    }
}
