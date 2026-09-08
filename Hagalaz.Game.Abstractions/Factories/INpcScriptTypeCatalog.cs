using System;
using System.Collections.Generic;

namespace Hagalaz.Game.Abstractions.Factories
{
    /// <summary>
    /// Provides the NPC script types owned by one explicitly registered script source.
    /// </summary>
    public interface INpcScriptTypeCatalog
    {
        IReadOnlyCollection<Type> ScriptTypes { get; }
    }
}
