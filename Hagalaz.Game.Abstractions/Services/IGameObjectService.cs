using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
        /// Gets the object definition.
        /// </summary>
        /// <param name="objectId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IGameObjectDefinition> FindGameObjectDefinitionById(int objectId, CancellationToken cancellationToken = default);

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
