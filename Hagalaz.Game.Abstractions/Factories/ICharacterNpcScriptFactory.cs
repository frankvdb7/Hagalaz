using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Factories
{
    /// <summary>
    /// Defines a contract for a factory that discovers and provides character-NPC interaction scripts.
    /// These scripts handle events that occur when a player character interacts with an NPC.
    /// </summary>
    public interface ICharacterNpcScriptFactory
    {
        /// <summary>
        /// Retrieves an asynchronous enumerable of all discovered character-NPC interaction scripts, pairing each NPC ID with its corresponding script type.
        /// </summary>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> of tuples, where each tuple contains an NPC ID and the <see cref="Type"/> of the <see cref="ICharacterNpcScript"/> that handles it.</returns>
        IAsyncEnumerable<(int npcId, Type scriptType)> GetScripts();
    }
}