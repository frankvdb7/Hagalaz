using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Raido.Common.Protocol;

namespace Raido.Server
{
    /// <summary>
    /// Manages the lifetime of Raido connections.
    /// </summary>
    public interface IRaidoLifetimeManager
    {
        /// <summary>
        /// Called when a new connection is established.
        /// </summary>
        /// <param name="connection">The connection that was established.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous connect operation.</returns>
        Task OnConnectedAsync(RaidoConnectionContext connection);

        /// <summary>
        /// Called when a connection is terminated.
        /// </summary>
        /// <param name="connection">The connection that was terminated.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous disconnect operation.</returns>
        Task OnDisconnectedAsync(RaidoConnectionContext connection);

        /// <summary>
        /// Sends a message to all connected clients.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the send operation.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous send operation.</returns>
        Task SendAllAsync(RaidoMessage message, CancellationToken cancellationToken);

        /// <summary>
        /// Sends a message to all connected clients except for the specified connections.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="excludedConnectionIds">A list of connection IDs to exclude.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the send operation.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous send operation.</returns>
        Task SendAllExceptAsync(RaidoMessage message, IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken);

        /// <summary>
        /// Sends a message to the specified connection.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="connectionId">The ID of the connection to send the message to.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the send operation.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous send operation.</returns>
        Task SendConnectionAsync(RaidoMessage message, string connectionId, CancellationToken cancellationToken);

        /// <summary>
        /// Sends a message to the specified connections.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="connectionIds">A list of connection IDs to send the message to.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the send operation.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous send operation.</returns>
        Task SendConnectionsAsync(RaidoMessage message, IReadOnlyList<string> connectionIds, CancellationToken cancellationToken);
    }
}