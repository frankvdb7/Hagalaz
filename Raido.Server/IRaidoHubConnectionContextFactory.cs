using System;
using Microsoft.AspNetCore.Connections;
using Raido.Common.Protocol;

namespace Raido.Server
{
    /// <summary>
    /// Creates logical Hub connection contexts from physical connections.
    /// </summary>
    public interface IRaidoHubConnectionContextFactory
    {
        /// <summary>
        /// Creates a logical Hub connection context and attaches its initial physical connection.
        /// </summary>
        /// <param name="connection">The initial physical connection.</param>
        /// <param name="protocol">The protocol used by the logical connection.</param>
        /// <param name="keepAliveInterval">An optional per-connection keep-alive override.</param>
        /// <param name="clientTimeoutInterval">An optional per-connection client-timeout override.</param>
        /// <param name="statefulReconnect">Whether stateful reconnect is enabled for the connection.</param>
        /// <returns>The created logical Hub connection context.</returns>
        RaidoHubConnectionContext Create(
            ConnectionContext connection,
            IRaidoProtocol protocol,
            TimeSpan? keepAliveInterval = null,
            TimeSpan? clientTimeoutInterval = null,
            bool statefulReconnect = false);
    }
}
