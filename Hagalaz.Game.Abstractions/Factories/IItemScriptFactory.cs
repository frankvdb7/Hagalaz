using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Factories
{
    /// <summary>
    /// Defines a contract for a factory that discovers and provides item scripts.
    /// These scripts define custom behaviors for items (e.g., when they are used or operated).
    /// </summary>
    public interface IItemScriptFactory
    {
        /// <summary>
        /// Retrieves an asynchronous enumerable of all discovered item scripts, pairing each item ID with its corresponding script type.
        /// </summary>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> of tuples, where each tuple contains an item ID and the <see cref="Type"/> of the <see cref="IItemScript"/> that handles it.</returns>
        IAsyncEnumerable<(int itemId, Type scriptType)> GetScripts();
    }
}