using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Raido.Common.Protocol;

namespace Raido.Server
{
    /// <summary>
    /// A context for a Raido caller.
    /// </summary>
    public abstract class RaidoCallerContext
    {
        internal virtual RaidoHubConnectionContext? Connection => null;

        /// <summary>
        /// Gets the ID of the connection.
        /// </summary>
        public abstract string ConnectionId { get; }

        /// <summary>
        /// Gets the user associated with the connection.
        /// </summary>
        public abstract ClaimsPrincipal? User { get; }
        
        /// <summary>
        /// Gets a key/value collection that can be used to share data within the scope of this connection.
        /// </summary>
        public abstract IDictionary<object, object?> Items { get; }

        /// <summary>
        /// Gets the collection of features available on the connection.
        /// </summary>
        public abstract IFeatureCollection Features { get; }

        /// <summary>
        /// Gets a <see cref="CancellationToken"/> that is triggered when the connection is aborted.
        /// </summary>
        public abstract CancellationToken ConnectionAborted { get; }
        
        /// <summary>
        /// Gets the local IP endpoint of the connection.
        /// </summary>
        public abstract IPEndPoint? LocalIPEndPoint { get; }
        
        /// <summary>
        /// Gets the remote IP endpoint of the connection.
        /// </summary>
        public abstract IPEndPoint? RemoteIPEndPoint { get; }

        /// <summary>
        /// Gets the protocol used by the connection.
        /// </summary>
        public abstract IRaidoProtocol Protocol { get; }

        /// <summary>
        /// Changes the protocol used by the connection after writes using the current protocol have completed.
        /// </summary>
        /// <param name="protocol">The protocol to use for subsequent reads and writes.</param>
        /// <param name="cancellationToken">The token that cancels waiting for the write boundary.</param>
        /// <returns>A <see cref="ValueTask"/> that represents the transition.</returns>
        public abstract ValueTask SetProtocolAsync(IRaidoProtocol protocol, CancellationToken cancellationToken);

        /// <summary>
        /// Changes the protocol used by the connection and transfers ownership of its lifetime to the connection.
        /// </summary>
        /// <param name="protocol">The protocol to use for subsequent reads and writes.</param>
        /// <param name="protocolLifetime">The lifetime for the protocol and its connection-owned dependencies.</param>
        /// <param name="cancellationToken">The token that cancels waiting for the write boundary.</param>
        /// <returns>A <see cref="ValueTask"/> that represents the transition.</returns>
        public abstract ValueTask SetProtocolAsync(
            IRaidoProtocol protocol,
            IAsyncDisposable protocolLifetime,
            CancellationToken cancellationToken);

        /// <summary>
        /// Aborts the connection.
        /// </summary>
        public abstract void Abort();
    }
}
