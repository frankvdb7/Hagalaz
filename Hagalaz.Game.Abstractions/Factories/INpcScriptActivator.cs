using System;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

namespace Hagalaz.Game.Abstractions.Factories
{
    /// <summary>
    /// Creates an NPC script for a specific NPC owner.
    /// </summary>
    public interface INpcScriptActivator
    {
        /// <summary>
        /// Creates the selected script with the NPC owner supplied as a runtime constructor argument.
        /// </summary>
        /// <param name="scriptType">The concrete NPC script type.</param>
        /// <param name="owner">The NPC that will own the script.</param>
        /// <returns>The created NPC script.</returns>
        INpcScript Create(Type scriptType, INpc owner);
    }
}
