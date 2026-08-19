using System;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Factories
{
    /// <summary>
    /// Creates a familiar script selected by runtime type.
    /// </summary>
    public interface IFamiliarScriptActivator
    {
        /// <summary>
        /// Creates a familiar script in the active character scope.
        /// </summary>
        /// <param name="scriptType">The selected script type.</param>
        /// <returns>The created familiar script.</returns>
        IFamiliarScript Create(Type scriptType);
    }
}
