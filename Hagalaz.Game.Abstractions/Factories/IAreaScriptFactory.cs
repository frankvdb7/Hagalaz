using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Maps;

namespace Hagalaz.Game.Abstractions.Factories
{
    /// <summary>
    /// Defines a contract for a factory that discovers and provides area scripts.
    /// Area scripts define custom behaviors for specific map areas.
    /// </summary>
    public interface IAreaScriptFactory
    {
        /// <summary>
        /// Retrieves an asynchronous enumerable of all discovered area scripts, pairing each area ID with its corresponding script type.
        /// </summary>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> of tuples, where each tuple contains an area ID and the <see cref="Type"/> of the <see cref="IAreaScript"/> that handles it.</returns>
        IAsyncEnumerable<(int areaId, Type scriptType)> GetScripts();
    }
}