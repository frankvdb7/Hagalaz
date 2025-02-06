using System;
using System.Collections.Generic;

namespace Hagalaz.Game.Abstractions.Factories
{
    public interface INpcScriptFactory
    {
        IAsyncEnumerable<(int npcId, Type scriptType)> GetScripts();
    }
}
