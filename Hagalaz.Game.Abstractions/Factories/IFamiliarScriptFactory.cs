using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Factories
{
    /// <summary>
    /// Defines a contract for a factory that discovers and provides familiar scripts.
    /// These scripts define the behavior of summoned familiars.
    /// </summary>
    public interface IFamiliarScriptFactory
    {
        /// <summary>
        /// Retrieves an asynchronous enumerable of all discovered familiar scripts, pairing each familiar's NPC ID with its corresponding script type.
        /// </summary>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> of tuples, where each tuple contains an NPC ID and the <see cref="Type"/> of the <see cref="IFamiliarScript"/> that handles it.</returns>
        IAsyncEnumerable<(int npcId, Type scriptType)> GetScripts();
    }
}