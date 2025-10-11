using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.GameObjects;

namespace Hagalaz.Game.Abstractions.Factories
{
    /// <summary>
    /// Defines a contract for a factory that discovers and provides game object scripts.
    /// These scripts define the behavior of interactive objects in the game world.
    /// </summary>
    public interface IGameObjectScriptFactory
    {
        /// <summary>
        /// Retrieves an asynchronous enumerable of all discovered game object scripts, pairing each object ID with its corresponding script type.
        /// </summary>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> of tuples, where each tuple contains a game object ID and the <see cref="Type"/> of the <see cref="IGameObjectScript"/> that handles it.</returns>
        IAsyncEnumerable<(int objectId, Type scriptType)> GetScripts();
    }
}
