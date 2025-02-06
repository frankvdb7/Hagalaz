using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps.Updates;

namespace Hagalaz.Game.Abstractions.Model.Maps
{
    /// <summary>
    /// 
    /// </summary>
    public interface IMapRegion
    {
        /// <summary>
        /// Gets the region's unique id.
        /// </summary>
        /// <value>The id.</value>
        int Id { get; }
        /// <summary>
        /// Gets the xtea keys for this map region.
        /// </summary>
        /// <returns>Returns an array of integers.</returns>
        int[] XteaKeys { get; }
        /// <summary>
        /// The base location of the region.
        /// </summary>
        ILocation BaseLocation { get; }
        /// <summary>
        /// The size of the region.
        /// </summary>
        IVector3 Size { get; }
        /// <summary>
        /// Contains boolean if this region is dynamic.
        /// </summary>
        /// <value><c>true</c> if dynamic; otherwise, <c>false</c>.</value>
        bool IsDynamic { get; }
        /// <summary>
        /// Contains boolean if this region is loaded
        /// </summary>
        bool IsLoaded { get; }
        /// <summary>
        /// Contains boolean if this region is destroyed
        /// </summary>
        bool IsDestroyed { get; }
        /// <summary>
        /// Get's if this region can be destroyed.
        /// </summary>
        /// <returns><c>true</c> if this instance can destroy; otherwise, <c>false</c>.</returns>
        bool CanDestroy();
        /// <summary>
        /// Get's if this region can be idled.
        /// </summary>
        /// <returns><c>true</c> if this instance can suspend; otherwise, <c>false</c>.</returns>
        bool CanSuspend();
        /// <summary>
        /// Loads this region
        /// </summary>
        /// <returns></returns>
        void Load();
        /// <summary>
        /// Resumes this region
        /// </summary>
        /// <returns></returns>
        void Resume();
        /// <summary>
        /// Suspends this region
        /// </summary>
        /// <returns></returns>
        void Suspend();
        /// <summary>
        ///  Destroys this region
        /// </summary>
        Task DestroyAsync();
        /// <summary>
        /// Get's all the characters from this region.
        /// </summary>
        /// <value>The characters.</value>
        IEnumerable<ICharacter> FindAllCharacters();
        /// <summary>
        /// Get's all the npcs from this region.
        /// </summary>
        /// <value>The npcs.</value>
        IEnumerable<INpc> FindAllNpcs();
        /// <summary>
        /// Get's all the ground items from this region.
        /// </summary>
        /// <returns></returns>
        IEnumerable<IGroundItem> FindAllGroundItems();
        /// <summary>
        /// Get's all the ground items from this region.
        /// </summary>
        /// <returns></returns>
        IEnumerable<IGameObject> FindAllGameObjects();
        /// <summary>
        /// Make's region standart, and removes any changes
        /// made to it.
        /// </summary>
        void MakeStandard();
        /// <summary>
        /// Make's this region dynamic.
        /// </summary>
        void MakeDynamic();
        /// <summary>
        /// Adds an npc who has entered this region.
        /// </summary>
        /// <param name="npc">The NPC.</param>
        void Add(INpc npc);
        /// <summary>
        /// Adds a player who has entered this region.
        /// </summary>
        /// <param name="character">The character.</param>
        void Add(ICharacter character);
        /// <summary>
        /// Spawn's specific ground item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>Item which was spawned, if this item was not stacked with other ground items , then returns is ('item').
        /// Return can be null if given item is already spawned.</returns>
        void Add(IGroundItem item);
        /// <summary>
        /// Spawn's given gameObject.
        /// Despawns/Disables overloaded objects.
        /// </summary>
        /// <param name="gameObj">Object which should be spawned.</param>
        void Add(IGameObject gameObj);
        /// <summary>
        /// Revmoves a player who has left this region.
        /// </summary>
        /// <param name="character">The character to remove.</param>
        void Remove(ICharacter character);
        /// <summary>
        /// Removes an npc who has left this region.
        /// </summary>
        /// <param name="npc">The NPC to remove.</param>
        void Remove(INpc npc);
        /// <summary>
        /// Delete's or Disable's given gameObject.
        /// </summary>
        /// <param name="gameObj">Object which should be despawned.</param>
        /// <returns>If deletion was sucessfull.</returns>
        void Remove(IGameObject gameObj);
        /// <summary>
        /// Delete's specific ground item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>If item was sucessfully deleted.</returns>
        void Remove(IGroundItem item);
        /// <summary>
        /// Flag's clip with specific flag at specific location.
        /// Throws exception if location is invalid.
        /// </summary>
        /// <param name="localX">The x.</param>
        /// <param name="localY">The y.</param>
        /// <param name="z">The z.</param>
        /// <param name="flag">The flag.</param>
        void FlagCollision(int localX, int localY, int z, CollisionFlag flag);

        /// <summary>
        /// UnFlag's clip with specific flag at specific location.
        /// Throws exception if location is invalid.
        /// </summary>
        /// <param name="localX">The x.</param>
        /// <param name="localY">The y.</param>
        /// <param name="z">The z.</param>
        /// <param name="flag">The flag.</param>
        void UnFlagCollision(int localX, int localY, int z, CollisionFlag flag);
        /// <summary>
        /// Remove's specific object from clipping.
        /// </summary>
        /// <param name="gameObject">The game object.</param>
        void UnFlagCollision(IGameObject gameObject);
        /// <summary>
        /// Add's specific object to clipping.
        /// </summary>
        /// <param name="gameObject">The game object.</param>
        void FlagCollision(IGameObject gameObject);

        /// <summary>
        /// Tick 1.
        /// </summary>
        Task MajorUpdateTick();

        /// <summary>
        /// Tick 2.
        /// </summary>
        Task MajorClientPrepareUpdateTick();

        /// <summary>
        /// Tick 3.
        /// </summary>
        Task MajorClientUpdateTick();

        /// <summary>
        /// Tick 4.
        /// </summary>
        Task MajorClientUpdateResetTick();

        /// <summary>
        /// Sends all zone updates of this region to the character.
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        void SendFullPartUpdates(ICharacter character);

        /// <summary>
        /// Get's region part data of given part.
        /// </summary>
        /// <param name="partX">PartX of region to write. PartX = AbsX / 8.</param>
        /// <param name="partY">PartY of region to write. PartY = AbsY / 8.</param>
        /// <param name="z">The z.</param>
        /// <returns>RegionPartData.</returns>
        IMapRegionPart GetRegionPartData(int partX, int partY, int z);

        /// <summary>
        /// Writes region data to given block.
        /// </summary>
        /// <param name="partX">PartX of region to write. PartX = AbsX / 8.</param>
        /// <param name="partY">PartY of region to write. PartY = AbsY / 8.</param>
        /// <param name="z">The z.</param>
        /// <param name="drawPartX">The draw part X.</param>
        /// <param name="drawPartY">The draw part Y.</param>
        /// <param name="drawPartZ">The draw part Z.</param>
        void WriteBlock(int partX, int partY, int z, int drawPartX, int drawPartY, int drawPartZ);
        /// <summary>
        /// Queues an update for this map region.
        /// </summary>
        /// <param name="update"></param>
        void QueueUpdate(IRegionPartUpdate update);
        /// <summary>
        /// Get's standart object at specific location.
        /// Throws exception if location is invalid.
        /// </summary>
        /// <param name="localX">The x.</param>
        /// <param name="localY">The y.</param>
        /// <param name="z">The z.</param>
        /// <returns>GameObject or null if nothing is spawned at that location.</returns>
        IGameObject? FindStandardGameObject(int localX, int localY, int z);
        /// <summary>
        /// Get's game objects at specific location.
        /// </summary>
        /// <param name="localX">The x.</param>
        /// <param name="localY">The y.</param>
        /// <param name="z">The z.</param>
        /// <returns>List{GameObject}.</returns>
        IEnumerable<IGameObject> FindGameObjects(int localX, int localY, int z);
        /// <summary>
        /// Get's clip at specific location.
        /// Throws exception if location is invalid.
        /// </summary>
        /// <param name="localX">The local x in the region.</param>
        /// <param name="localY">The local y in the region.</param>
        /// <param name="z">The z.</param>
        /// <returns>System.Int32.</returns>
        CollisionFlag GetCollision(int localX, int localY, int z);
    }
}
