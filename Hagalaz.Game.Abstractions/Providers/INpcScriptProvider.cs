using System;

namespace Hagalaz.Game.Abstractions.Providers
{
    /// <summary>
    /// Defines a contract for a provider that maps an NPC ID to its corresponding script type.
    /// These scripts control the general behavior of an NPC, such as its combat style, movement patterns, and basic interactions.
    /// </summary>
    public interface INpcScriptProvider
    {
        /// <summary>
        /// Gets the <see cref="Type"/> of the script for a specific NPC.
        /// </summary>
        /// <param name="npcId">The unique identifier of the NPC.</param>
        /// <returns>The script <see cref="Type"/> for the specified NPC, or a default/null type if not found.</returns>
        Type GetNpcScriptTypeById(int npcId);
    }
}