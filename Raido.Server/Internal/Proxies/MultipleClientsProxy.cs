using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Raido.Common.Protocol;

namespace Raido.Server.Internal.Proxies
{
    internal class MultipleClientsProxy : IRaidoClientProxy
    {
        private readonly IRaidoHubLifetimeManager _lifetimeManager;
        private readonly IReadOnlyList<string> _connectionIds;

        public MultipleClientsProxy(IRaidoHubLifetimeManager lifetimeManager, IReadOnlyList<string> connectionIds)
        {
            _lifetimeManager = lifetimeManager;
            _connectionIds = connectionIds;
        }

        public Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : RaidoMessage =>
            _lifetimeManager.SendConnectionsAsync(message, _connectionIds, cancellationToken);
    }
}