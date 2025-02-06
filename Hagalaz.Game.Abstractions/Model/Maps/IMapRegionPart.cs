using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps.Updates;

namespace Hagalaz.Game.Abstractions.Model.Maps
{
    public interface IMapRegionPart
    {
        /// <summary>
        /// Contains real (drawed) part X
        /// If this part data is not modified this is same
        /// as the PartX of this data.
        /// </summary>
        int DrawRegionPartX { get; set; }
        /// <summary>
        /// Contains real (drawed) part Y
        /// If this part data is not modified this is same
        /// as the PartY of this data.
        /// </summary>
        int DrawRegionPartY { get; set; }
        /// <summary>
        /// Contains real (drawed) Z
        /// If this part data is not modified this is same
        /// as the Z of this data.
        /// </summary>
        int DrawRegionZ { get; set; }
        /// <summary>
        /// Contains rotation of this sector,
        /// It is 0 by default if not modified.
        /// </summary>
        int Rotation { get; set; }

        /// <summary>
        /// Finds the game object at the specified local coordinates and layer.
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="localX"></param>
        /// <param name="localY"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        IGameObject? FindGameObject(LayerType layer, int localX, int localY, int z);
        /// <summary>
        /// Finds all the game objects in this part.
        /// </summary>
        /// <returns></returns>
        IEnumerable<IGameObject> FindAllGameObjects();
        /// <summary>
        /// Finds all the ground items in this part.
        /// </summary>
        /// <returns></returns>
        IEnumerable<IGroundItem> FindAllGroundItems();
        
        /// <summary>
        /// Adds the game object to this part.
        /// </summary>
        /// <param name="gameObject"></param>
        void Add(IGameObject gameObject);
        /// <summary>
        /// Adds the ground item to this part.
        /// </summary>
        /// <param name="item"></param>
        void Add(IGroundItem item);

        /// <summary>
        /// Removes the game object from this part.
        /// </summary>
        /// <param name="gameObject"></param>
        void Remove(IGameObject gameObject);
        /// <summary>
        /// Removes the ground item from this part.
        /// </summary>
        /// <param name="item"></param>
        void Remove(IGroundItem item);

        /// <summary>
        /// Sends all updates that happened inside this part to the character.
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        void SendFullUpdate(ICharacter character);

        /// <summary>
        /// Sends all the updates that happened in this tick to the character.
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        void SendUpdates(ICharacter character);
        /// <summary>
        /// Queues a part update for this tick.
        /// </summary>
        /// <param name="update"></param>
        void QueueUpdate(IRegionPartUpdate update);
        /// <summary>
        /// Clears all the updates that happened in this tick.
        /// </summary>
        void ClearUpdates();

        /// <summary>
        /// Erase's this region part.
        /// </summary>
        void Erase();
    }
}