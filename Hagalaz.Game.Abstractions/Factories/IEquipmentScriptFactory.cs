using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Hagalaz.Game.Abstractions.Factories
{
    public interface IEquipmentScriptFactory
    {
        IAsyncEnumerable<(int itemId, Type scriptType)> GetScripts(CancellationToken cancellationToken = default);
    }
}