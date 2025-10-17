using System;

namespace Hagalaz.Game.Abstractions.Providers
{
    /// <summary>
    /// Defines a contract for a provider that supplies a default script for character-NPC interactions.
    /// This script is used when a specific interaction script for a character and NPC combination is not found.
    /// </summary>
    public interface IDefaultCharacterNpcScriptProvider
    {
        /// <summary>
        /// Gets the <see cref="Type"/> of the default character-NPC interaction script.
        /// </summary>
        /// <returns>The <see cref="Type"/> of the default script implementation.</returns>
        Type GetScriptType();
    }
}