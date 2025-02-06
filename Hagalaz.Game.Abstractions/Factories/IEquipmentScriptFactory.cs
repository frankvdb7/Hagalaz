using System;
using System.Collections.Generic;

namespace Hagalaz.Game.Abstractions.Factories
{
    public interface IEquipmentScriptFactory
    {
        IAsyncEnumerable<(int itemId, Type scriptType)> GetScripts();
    }
}