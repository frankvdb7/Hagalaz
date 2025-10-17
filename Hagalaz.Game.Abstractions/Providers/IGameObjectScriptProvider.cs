using System;

namespace Hagalaz.Game.Abstractions.Providers
{
    /// <summary>
    /// Defines a contract for a provider that maps a game object ID to its corresponding script type.
    /// These scripts control the behavior of interactive objects in the game world, such as doors, levers, or chests.
    /// </summary>
    public interface IGameObjectScriptProvider
    {
        /// <summary>
        /// Gets the <see cref="Type"/> of the script for a specific game object.
        /// </summary>
        /// <param name="objectId">The unique identifier of the game object.</param>
        /// <returns>The script <see cref="Type"/> for the specified game object, or a default/null type if not found.</returns>
        Type GetGameObjectScriptTypeById(int objectId);
    }
}