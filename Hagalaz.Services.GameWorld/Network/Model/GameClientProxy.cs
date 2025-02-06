using System.Threading;
using System.Threading.Tasks;
using Raido.Common.Protocol;
using Raido.Server;

namespace Hagalaz.Services.GameWorld.Network.Model
{
    public class GameClientProxy : IRaidoClientProxy
    {
        private readonly IRaidoLifetimeManager _lifetimeManager;
        private readonly string _connectionId;

        public GameClientProxy(IRaidoLifetimeManager lifetimeManager, string connectionId)
        {
            _lifetimeManager = lifetimeManager;
            _connectionId = connectionId;
        }

        public Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : RaidoMessage 
            => _lifetimeManager.SendConnectionAsync(message, _connectionId, cancellationToken);
    }
}
