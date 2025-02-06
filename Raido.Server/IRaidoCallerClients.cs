namespace Raido.Server
{
    public interface IRaidoCallerClients : IRaidoCallerClients<IRaidoClientProxy>
    {
    }

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