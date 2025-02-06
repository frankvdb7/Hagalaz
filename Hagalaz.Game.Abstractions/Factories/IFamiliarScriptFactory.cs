using System;
using System.Collections.Generic;

namespace Hagalaz.Game.Abstractions.Factories
{
    public interface IFamiliarScriptFactory
    {
        IAsyncEnumerable<(int npcId, Type scriptType)> GetScripts();
    }
}