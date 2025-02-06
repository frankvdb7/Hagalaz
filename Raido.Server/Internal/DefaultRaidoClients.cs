using System.Collections.Generic;
using Raido.Server.Internal.Proxies;

namespace Raido.Server.Internal
{
    internal class DefaultRaidoClients : IRaidoClients
    {
        private readonly IRaidoLifetimeManager _lifetimeManager;

        public DefaultRaidoClients(IRaidoLifetimeManager lifetimeManager)
        {
            _lifetimeManager = lifetimeManager;
            All = new AllClientProxy(lifetimeManager);
        }

        public IRaidoClientProxy All { get; }
        public IRaidoClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => new AllClientsExceptProxy(_lifetimeManager, excludedConnectionIds);

        public IRaidoClientProxy Client(string connectionId) => new SingleClientProxy(_lifetimeManager, connectionId);

        public IRaidoClientProxy Clients(IReadOnlyList<string> connectionIds) => new MultipleClientsProxy(_lifetimeManager, connectionIds);
    }
}