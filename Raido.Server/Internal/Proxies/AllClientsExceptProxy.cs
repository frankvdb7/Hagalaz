using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Raido.Common.Protocol;

namespace Raido.Server.Internal.Proxies
{
    internal class AllClientsExceptProxy : IRaidoClientProxy
    {
        private readonly IRaidoLifetimeManager _lifetimeManager;
        private readonly IReadOnlyList<string> _excludedConnectionIds;

        public AllClientsExceptProxy(IRaidoLifetimeManager lifetimeManager, IReadOnlyList<string> excludedConnectionIds)
        {
            _lifetimeManager = lifetimeManager;
            _excludedConnectionIds = excludedConnectionIds;
        }

        public Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : RaidoMessage =>
            _lifetimeManager.SendAllExceptAsync(message, _excludedConnectionIds, cancellationToken);
    }
}