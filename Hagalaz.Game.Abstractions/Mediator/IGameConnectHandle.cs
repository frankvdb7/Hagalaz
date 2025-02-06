using System;

namespace Hagalaz.Game.Abstractions.Mediator
{
    public interface IGameConnectHandle : IDisposable
    {
        public void Disconnect();
    }
}
