using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Features.States;

namespace Hagalaz.Game.Abstractions.Factories
{
    public interface IStateScriptFactory
    {
        IAsyncEnumerable<(StateType stateType, Type scriptType)> GetScripts();
    }
}