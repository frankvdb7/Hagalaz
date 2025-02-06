namespace Raido.Server
{
    public interface IRaidoContext
    {
        IRaidoClients Clients { get; }
    }
}