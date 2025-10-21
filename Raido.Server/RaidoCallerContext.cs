using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Threading;
using Microsoft.AspNetCore.Http.Features;
using Raido.Common.Protocol;

namespace Raido.Server
{
    /// <summary>
    /// A context for a Raido caller.
    /// </summary>
    public abstract class RaidoCallerContext
    {
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
        public abstract CancellationToken ConnectionAbortedToken { get; }
        
        /// <summary>
        /// Gets the local IP endpoint of the connection.
        /// </summary>
        public abstract IPEndPoint? LocalIPEndPoint { get; }
        
        /// <summary>
        /// Gets the remote IP endpoint of the connection.
        /// </summary>
        public abstract IPEndPoint? RemoteIPEndPoint { get; }

        /// <summary>
        /// Gets or sets the protocol used by the connection.
        /// </summary>
        public abstract IRaidoProtocol Protocol { get; set; }

        /// <summary>
        /// Aborts the connection.
        /// </summary>
        public abstract void Abort();
    }
}