using System;
using System.Collections.Generic;

namespace Hagalaz.Game.Abstractions.Factories
{
    public interface IGameObjectScriptFactory
    {
        IAsyncEnumerable<(int objectId, Type scriptType)> GetScripts();
    }
}
