using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    public interface IGameObjectService
    {
        /// <summary>
        /// Finds the game object by location.
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public IEnumerable<IGameObject> FindByLocation(ILocation location);

        /// <summary>
        /// Finds all game objects by criteria.
        /// </summary>
        /// <param name="gameObjectFindAll"></param>
        /// <returns></returns>
        public IEnumerable<IGameObject> FindAll(GameObjectFindAll gameObjectFindAll);
        /// <summary>
        /// Gets the object definition.
        /// </summary>
        /// <param name="objectID">The object identifier.</param>
        /// <returns></returns>
        IGameObjectDefinition FindGameObjectDefinitionById(int objectID);

        /// <summary>
        /// Gets the objects count.
        /// </summary>
        /// <returns></returns>
        int GetObjectsCount();

        /// <summary>
        /// Updates the game object.
        /// </summary>
        /// <param name="gameObjectUpdate"></param>
        void UpdateGameObject(GameObjectUpdate gameObjectUpdate);

        /// <summary>
        /// Animates the game object.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="animation"></param>
        void AnimateGameObject(IGameObject gameObject, IAnimation animation);
    }
}
