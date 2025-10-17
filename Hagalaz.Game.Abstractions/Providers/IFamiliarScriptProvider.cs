using System;

namespace Hagalaz.Game.Abstractions.Providers
{
    /// <summary>
    /// Defines a contract for a provider that maps a familiar's NPC ID to its corresponding script type.
    /// These scripts control the behavior and special abilities of summoned familiars.
    /// </summary>
    public interface IFamiliarScriptProvider
    {
        /// <summary>
        /// Finds the <see cref="Type"/> of the script for a specific familiar.
        /// </summary>
        /// <param name="npcId">The NPC identifier of the familiar.</param>
        /// <returns>The script <see cref="Type"/> for the specified familiar, or a default/null type if not found.</returns>
        Type FindFamiliarScriptTypeById(int npcId);
    }
}