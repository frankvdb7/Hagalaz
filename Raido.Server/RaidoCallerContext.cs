using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Threading;
using Microsoft.AspNetCore.Http.Features;
using Raido.Common.Protocol;

namespace Raido.Server
{
    public abstract class RaidoCallerContext
    {
        public abstract string ConnectionId { get; }

        public abstract ClaimsPrincipal? User { get; }
        
        public abstract IDictionary<object, object?> Items { get; }

        public abstract IFeatureCollection Features { get; }

        public abstract CancellationToken ConnectionAbortedToken { get; }
        
        public abstract IPEndPoint? LocalIPEndPoint { get; }
        
        public abstract IPEndPoint? RemoteIPEndPoint { get; }

        public abstract IRaidoProtocol Protocol { get; set; }

        public abstract void Abort();
    }
}