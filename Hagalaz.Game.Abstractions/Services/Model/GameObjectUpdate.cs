using Hagalaz.Game.Abstractions.Model.GameObjects;

namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// A data transfer object containing information for updating a game object's appearance or state.
    /// </summary>
    public record GameObjectUpdate
    {
        /// <summary>
        /// Gets the game object instance to be updated.
        /// </summary>
        public required IGameObject Instance { get; init; }

        /// <summary>
        /// Gets the new object ID to transform the game object into.
        /// </summary>
        public int Id { get; init; }

        /// <summary>
        /// Gets the new rotation for the game object.
        /// </summary>
        public int Rotation { get; init; }
    }
}