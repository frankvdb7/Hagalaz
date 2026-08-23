using System;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Factories
{
    /// <summary>
    /// Creates a character-NPC script selected by runtime type.
    /// </summary>
    public interface ICharacterNpcScriptActivator
    {
        /// <summary>
        /// Creates a character-NPC script in the active character scope.
        /// </summary>
        /// <param name="scriptType">The selected script type.</param>
        /// <returns>The created character-NPC script.</returns>
        ICharacterNpcScript Create(Type scriptType);
    }
}
