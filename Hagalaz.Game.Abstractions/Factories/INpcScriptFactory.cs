using System;
using System.Collections.Generic;
using System.Threading;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

namespace Hagalaz.Game.Abstractions.Factories
{
    /// <summary>
    /// Defines a contract for a factory that discovers and provides NPC scripts.
    /// These scripts define the behavior of Non-Player Characters in the game.
    /// </summary>
    public interface INpcScriptFactory
    {
        /// <summary>
        /// Retrieves an asynchronous enumerable of all discovered NPC scripts, pairing each NPC ID with its corresponding script type.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> of tuples, where each tuple contains an NPC ID and the <see cref="Type"/> of the <see cref="INpcScript"/> that handles it.</returns>
        IAsyncEnumerable<(int npcId, Type scriptType)> GetScripts(CancellationToken cancellationToken = default);
    }
}
