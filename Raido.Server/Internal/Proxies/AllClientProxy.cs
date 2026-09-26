using System.Threading;
using System.Threading.Tasks;
using Raido.Common.Protocol;

namespace Raido.Server.Internal.Proxies
{
    internal class AllClientProxy : IRaidoClientProxy
    {
        private readonly IRaidoHubLifetimeManager _lifetimeManager;

        public AllClientProxy(IRaidoHubLifetimeManager lifetimeManager) => _lifetimeManager = lifetimeManager;

        public Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : RaidoMessage =>
            _lifetimeManager.SendAllAsync(message, cancellationToken);
    }
}