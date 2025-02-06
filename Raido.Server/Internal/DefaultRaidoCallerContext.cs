using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Threading;
using Microsoft.AspNetCore.Http.Features;
using Raido.Common.Protocol;

namespace Raido.Server.Internal
{
    internal sealed class DefaultRaidoCallerContext : RaidoCallerContext
    {
        private readonly RaidoConnectionContext _connection;

        public DefaultRaidoCallerContext(RaidoConnectionContext connection) => _connection = connection;

        public override string ConnectionId => _connection.ConnectionId;
        public override ClaimsPrincipal? User => _connection.User;
        public override IDictionary<object, object?> Items => _connection.Items;
        public override IFeatureCollection Features => _connection.Features;
        public override CancellationToken ConnectionAbortedToken => _connection.ConnectionAbortedToken;
        public override IPEndPoint? LocalIPEndPoint => _connection.LocalEndPoint;
        public override IPEndPoint? RemoteIPEndPoint => _connection.RemoteEndPoint;

        public override IRaidoProtocol Protocol
        {
            get => _connection.Protocol;
            set => _connection.Protocol = value;
        }

        public override void Abort() => _connection.Abort();
    }
}