using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps.Updates;

namespace Hagalaz.Game.Abstractions.Model.Maps
{
    /// <summary>
    /// Defines the contract for an 8x8 section of a map region, used for dynamic map updates.
    /// </summary>
    public interface IMapRegionPart
    {
        /// <summary>
        /// Gets or sets the X-coordinate of the region part that this part's data is drawn from.
        /// </summary>
        int DrawRegionPartX { get; set; }

        /// <summary>
        /// Gets or sets the Y-coordinate of the region part that this part's data is drawn from.
        /// </summary>
        int DrawRegionPartY { get; set; }

        /// <summary>
        /// Gets or sets the plane (height level) of the region part that this part's data is drawn from.
        /// </summary>
        int DrawRegionZ { get; set; }

        /// <summary>
        /// Gets or sets the rotation of this region part.
        /// </summary>
        int Rotation { get; set; }

        /// <summary>
        /// Finds a game object at the specified local coordinates and layer within this region part.
        /// </summary>
        /// <param name="layer">The rendering layer of the object.</param>
        /// <param name="localX">The local X-coordinate within the part (0-7).</param>
        /// <param name="localY">The local Y-coordinate within the part (0-7).</param>
        /// <param name="z">The plane (height level).</param>
        /// <returns>The game object if found; otherwise, <c>null</c>.</returns>
        IGameObject? FindGameObject(LayerType layer, int localX, int localY, int z);

        /// <summary>
        /// Finds all game objects in this region part.
        /// </summary>
        /// <returns>An enumerable collection of game objects.</returns>
        IEnumerable<IGameObject> FindAllGameObjects();

        /// <summary>
        /// Finds all ground items in this region part.
        /// </summary>
        /// <returns>An enumerable collection of ground items.</returns>
        IEnumerable<IGroundItem> FindAllGroundItems();

        /// <summary>
        /// Adds a game object to this region part.
        /// </summary>
        /// <param name="gameObject">The game object to add.</param>
        void Add(IGameObject gameObject);

        /// <summary>
        /// Adds a ground item to this region part.
        /// </summary>
        /// <param name="item">The ground item to add.</param>
        void Add(IGroundItem item);

        /// <summary>
        /// Removes a game object from this region part.
        /// </summary>
        /// <param name="gameObject">The game object to remove.</param>
        void Remove(IGameObject gameObject);

        /// <summary>
        /// Removes a ground item from this region part.
        /// </summary>
        /// <param name="item">The ground item to remove.</param>
        void Remove(IGroundItem item);

        /// <summary>
        /// Processes an expired ground item, making it public or despawning it.
        /// </summary>
        /// <param name="item">The expired ground item.</param>
        void ProcessExpiredItem(IGroundItem item);

        /// <summary>
        /// Sends all pending updates for this region part to a specific character.
        /// </summary>
        /// <param name="character">The character to send the updates to.</param>
        void SendFullUpdate(ICharacter character);

        /// <summary>
        /// Sends the updates for the current tick to a specific character.
        /// </summary>
        /// <param name="character">The character to send the updates to.</param>
        void SendUpdates(ICharacter character);

        /// <summary>
        /// Queues an update for this region part to be sent in the next client update.
        /// </summary>
        /// <param name="update">The region part update to queue.</param>
        void QueueUpdate(IRegionPartUpdate update);

        /// <summary>
        /// Clears all queued updates for this region part.
        /// </summary>
        void ClearUpdates();

        /// <summary>
        /// Erases all dynamic data from this region part, resetting it to its base state.
        /// </summary>
        void Erase();
    }
}