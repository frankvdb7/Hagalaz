using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Raido.Common.Protocol;

namespace Raido.Server.Internal
{
    internal sealed class DefaultRaidoCallerContext : RaidoCallerContext
    {
        private readonly RaidoHubConnectionContext _connection;

        public DefaultRaidoCallerContext(RaidoHubConnectionContext connection) => _connection = connection;

        public override string ConnectionId => _connection.ConnectionId;
        public override ClaimsPrincipal? User => _connection.User;
        public override IDictionary<object, object?> Items => _connection.Items;
        public override IFeatureCollection Features => _connection.Features;
        public override CancellationToken ConnectionAborted => _connection.ConnectionAborted;
        public override IPEndPoint? LocalIPEndPoint => _connection.LocalEndPoint;
        public override IPEndPoint? RemoteIPEndPoint => _connection.RemoteEndPoint;

        public override IRaidoProtocol Protocol => _connection.Protocol;

        public override ValueTask SetProtocolAsync(IRaidoProtocol protocol, CancellationToken cancellationToken) =>
            _connection.SetProtocolAsync(protocol, cancellationToken);

        public override ValueTask SetProtocolAsync(
            IRaidoProtocol protocol,
            IAsyncDisposable protocolLifetime,
            CancellationToken cancellationToken) =>
            _connection.SetProtocolAsync(protocol, protocolLifetime, cancellationToken);

        internal override RaidoHubConnectionContext Connection => _connection;

        public override bool TryEnableStatefulReconnect() => _connection.TryEnableStatefulReconnect();

        public override void Abort() => _connection.Abort();
    }
}
