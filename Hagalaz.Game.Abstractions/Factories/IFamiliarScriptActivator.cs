using System;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Factories
{
    /// <summary>
    /// Creates a familiar script selected by runtime type.
    /// </summary>
    public interface IFamiliarScriptActivator
    {
        IFamiliarScript Create(Type scriptType);
    }
}
