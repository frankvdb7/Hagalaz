using System;
using System.Collections.Generic;
using System.Threading;

namespace Hagalaz.Game.Abstractions.Factories
{
    public interface INpcScriptFactory
    {
        IAsyncEnumerable<(int npcId, Type scriptType)> GetScripts(CancellationToken cancellationToken = default);
    }
}
