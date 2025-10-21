namespace Raido.Server
{
    /// <summary>
    /// A proxy that provides access to the calling client and other clients.
    /// </summary>
    public interface IRaidoCallerClients : IRaidoCallerClients<IRaidoClientProxy>
    {
    }

    /// <summary>
    /// A proxy that provides access to the calling client and other clients.
    /// </summary>
    /// <typeparam name="T">The type of the client proxy.</typeparam>
    public interface IRaidoCallerClients<out T> : IRaidoClients<T>
    {
        /// <summary>
        /// Gets a caller to the connection which triggered the current invocation.
        /// </summary>
        T Caller { get; }

        /// <summary>
        /// Gets a caller to all connections except the one which triggered the current invocation.
        /// </summary>
        T Others { get; }
    }
}