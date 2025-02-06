using System.Collections.Generic;

namespace Raido.Server
{
    public interface IRaidoClients : IRaidoClients<IRaidoClientProxy>
    {
    }

    public interface IRaidoClients<out T>
    {
        /// <summary>
        /// Gets a <typeparamref name="T" /> that can be used to send a message to all clients connected to the hub.
        /// </summary>
        /// <returns>A client caller.</returns>
        T All { get; }

        /// <summary>
        /// Gets a <typeparamref name="T" /> that can be used to send a message to all clients connected to the hub excluding the specified client connections.
        /// </summary>
        /// <param name="excludedConnectionIds">A collection of connection IDs to exclude.</param>
        /// <returns>A client caller.</returns>
        T AllExcept(IReadOnlyList<string> excludedConnectionIds);

        /// <summary>
        /// Gets a <typeparamref name="T" /> that can be used to send a message to the specified client connection.
        /// </summary>
        /// <param name="connectionId">The connection ID.</param>
        /// <returns>A client caller.</returns>
        T Client(string connectionId);

        /// <summary>
        /// Gets a <typeparamref name="T" /> that can be used to send a message to the specified client connections.
        /// </summary>
        /// <param name="connectionIds">The connection IDs.</param>
        /// <returns>A client caller.</returns>
        T Clients(IReadOnlyList<string> connectionIds);
    }
}