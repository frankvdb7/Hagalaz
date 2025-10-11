using System;
using System.Collections.Generic;
using System.Threading;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Factories
{
    /// <summary>
    /// Defines a contract for a factory that discovers and provides equipment scripts.
    /// These scripts define custom behaviors for items when they are equipped.
    /// </summary>
    public interface IEquipmentScriptFactory
    {
        /// <summary>
        /// Retrieves an asynchronous enumerable of all discovered equipment scripts, pairing each item ID with its corresponding script type.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> of tuples, where each tuple contains an item ID and the <see cref="Type"/> of the <see cref="IEquipmentScript"/> that handles it.</returns>
        IAsyncEnumerable<(int itemId, Type scriptType)> GetScripts(CancellationToken cancellationToken = default);
    }
}