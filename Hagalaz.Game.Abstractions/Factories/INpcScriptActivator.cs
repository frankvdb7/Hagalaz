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

        /// <summary>
        /// Creates a script that has a typed NPC parent relationship.
        /// </summary>
        /// <typeparam name="TScript">The concrete NPC script type.</typeparam>
        /// <param name="owner">The NPC that will own the script.</param>
        /// <param name="parent">The parent NPC supplied to the script.</param>
        /// <returns>The created NPC script.</returns>
        TScript CreateWithParent<TScript>(INpc owner, INpc parent) where TScript : INpcScript;
    }
}
