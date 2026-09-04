namespace Raido.Server.Internal
{
    internal class DefaultRaidoContext : IRaidoContext
    {
        public DefaultRaidoContext(IRaidoHubLifetimeManager lifetimeManager) => Clients = new DefaultRaidoClients(lifetimeManager);

        public IRaidoClients Clients { get; }
    }
}