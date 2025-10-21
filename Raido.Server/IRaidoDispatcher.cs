using System;
using System.Threading.Tasks;
using Raido.Common.Protocol;

namespace Raido.Server
{
    /// <summary>
    /// A dispatcher for Raido connections.
    /// </summary>
    public interface IRaidoDispatcher
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
        /// <param name="exception">An <see cref="Exception"/> representing the error that occurred, or <c>null</c> if the connection was closed gracefully.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous disconnect operation.</returns>
        Task OnDisconnectedAsync(RaidoConnectionContext connection, Exception? exception);

        /// <summary>
        /// Dispatches a message to the appropriate handler.
        /// </summary>
        /// <param name="connection">The connection that received the message.</param>
        /// <param name="message">The message to dispatch.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous dispatch operation.</returns>
        Task DispatchMessageAsync(RaidoConnectionContext connection, RaidoMessage message);
    }
}