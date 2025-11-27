using System;

namespace Hagalaz.Game.Abstractions.Providers
{
    public interface IStateProvider
    {
        public Type GetStateById(string id);
    }
}