using System.Threading;
using System.Threading.Tasks;
using Raido.Common.Protocol;

namespace Raido.Server.Internal.Proxies
{
    internal class SingleClientProxy : IRaidoClientProxy
    {
        private readonly IRaidoHubLifetimeManager _lifetimeManager;
        private readonly string _connectionId;

        public SingleClientProxy(IRaidoHubLifetimeManager lifetimeManager, string connectionId)
        {
            _lifetimeManager = lifetimeManager;
            _connectionId = connectionId;
        }
        
        public Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : RaidoMessage =>
            _lifetimeManager.SendConnectionAsync(message, _connectionId, cancellationToken);
    }
}