using System;

namespace Hagalaz.Game.Abstractions.Providers
{
    /// <summary>
    /// Defines a contract for a provider that maps an NPC ID to a script type that handles character-specific interactions with that NPC.
    /// This is used when an NPC's behavior changes based on the player interacting with it (e.g., for quests or contextual dialogues).
    /// </summary>
    public interface ICharacterNpcScriptProvider
    {
        /// <summary>
        /// Gets the <see cref="Type"/> of the script that handles character interactions for a specific NPC.
        /// </summary>
        /// <param name="npcId">The unique identifier of the NPC.</param>
        /// <returns>The script <see cref="Type"/> for the specified NPC, or a default/null type if not found.</returns>
        Type GetCharacterNpcScriptTypeById(int npcId);
    }
}