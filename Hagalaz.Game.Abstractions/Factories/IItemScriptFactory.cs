using System;
using System.Collections.Generic;

namespace Hagalaz.Game.Abstractions.Factories
{
    public interface IItemScriptFactory
    {
        IAsyncEnumerable<(int itemId, Type scriptType)> GetScripts();
    }
}