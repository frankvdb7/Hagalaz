using System;
using System.Collections.Generic;

namespace Hagalaz.Game.Abstractions.Factories
{
    public interface ICharacterNpcScriptFactory
    {
        IAsyncEnumerable<(int npcId, Type scriptType)> GetScripts();
    }
}