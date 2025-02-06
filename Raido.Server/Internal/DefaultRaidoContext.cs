namespace Raido.Server.Internal
{
    internal class DefaultRaidoContext : IRaidoContext
    {
        public DefaultRaidoContext(IRaidoLifetimeManager lifetimeManager) => Clients = new DefaultRaidoClients(lifetimeManager);

        public IRaidoClients Clients { get; }
    }
}