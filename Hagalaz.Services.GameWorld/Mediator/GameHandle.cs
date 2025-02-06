using Hagalaz.Game.Abstractions.Mediator;
using MassTransit;

namespace Hagalaz.Services.GameWorld.Mediator
{
    public sealed class GameHandle : ConnectHandle, IGameConnectHandle
    {
        private readonly ConnectHandle _handle;
        public GameHandle(ConnectHandle handle) => _handle = handle;
        public void Disconnect() => _handle.Disconnect();
        public void Dispose() => _handle.Dispose();
    }
}
