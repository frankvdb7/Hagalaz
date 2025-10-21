namespace Raido.Server
{
    /// <summary>
    /// Provides access to the clients connected to a Raido hub.
    /// </summary>
    public interface IRaidoContext
    {
        /// <summary>
        /// Gets a <see cref="IRaidoClients"/> that can be used to invoke methods on the clients connected to the hub.
        /// </summary>
        IRaidoClients Clients { get; }
    }
}