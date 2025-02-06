using System;
using System.Collections.Generic;

namespace Hagalaz.Game.Abstractions.Factories
{
    public interface IAreaScriptFactory
    {
        IAsyncEnumerable<(int areaId, Type scriptType)> GetScripts();
    }
}