using System;
using System.Collections.Generic;

namespace Hagalaz.Game.Abstractions.Factories
{
    public interface IStateFactory
    {
        IAsyncEnumerable<(string stateId, Type scriptType)> GetStates();
    }
}