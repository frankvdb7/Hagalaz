using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that manages game objects in the world.
    /// </summary>
    public interface IGameObjectService
    {
        /// <summary>
        /// Finds all game objects at a specific location.
        /// </summary>
        /// <param name="location">The location to search for game objects.</param>
        /// <returns>An enumerable collection of game objects at the specified location.</returns>
        public IEnumerable<IGameObject> FindByLocation(ILocation location);

        /// <summary>
        /// Finds a game object definition by its ID.
        /// </summary>
        /// <param name="objectId">The ID of the game object definition to find.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="IGameObjectDefinition"/>.</returns>
        Task<IGameObjectDefinition> FindGameObjectDefinitionById(int objectId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the total number of game objects currently in the world.
        /// </summary>
        /// <returns>The total count of game objects.</returns>
        int GetObjectsCount();

        /// <summary>
        /// Sends an update for a game object to relevant clients.
        /// </summary>
        /// <param name="gameObjectUpdate">The game object update to send.</param>
        void UpdateGameObject(GameObjectUpdate gameObjectUpdate);

        /// <summary>
        /// Plays an animation on a specific game object.
        /// </summary>
        /// <param name="gameObject">The game object to animate.</param>
        /// <param name="animation">The animation to play.</param>
        void AnimateGameObject(IGameObject gameObject, IAnimation animation);
    }
}