using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Raido.Common.Messages;
using Raido.Common.Protocol;

namespace Raido.Server.Internal
{
    /// <summary>
    /// A default in-memory lifetime manager abstraction.
    /// </summary>
    internal class DefaultRaidoLifetimeManager : IRaidoLifetimeManager
    {
        private readonly RaidoConnectionStore _connections;

        public DefaultRaidoLifetimeManager(RaidoConnectionStore connections) => _connections = connections;

        public Task OnConnectedAsync(RaidoConnectionContext connection)
        {
            _connections.Add(connection);
            return Task.CompletedTask;
        }

        public Task OnDisconnectedAsync(RaidoConnectionContext connection)
        {
            _connections.Remove(connection);
            return Task.CompletedTask;
        }

        public Task SendAllAsync(RaidoMessage message, CancellationToken cancellationToken) =>
            SendToAllConnectionsAsync(message, include: null, state: null, cancellationToken);

        public Task SendAllExceptAsync(RaidoMessage message, IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken) =>
            SendToAllConnectionsAsync(message,
                (connection, state) => !((IReadOnlyList<string>)state!).Contains(connection.ConnectionId), excludedConnectionIds, cancellationToken);

        public Task SendConnectionAsync(RaidoMessage message, string connectionId, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(connectionId);

            var connection = _connections[connectionId];

            if (connection == null)
            {
                return Task.CompletedTask;
            }

            // We're sending to a single connection
            // Write message directly to connection without caching it in memory
            return connection.WriteAsync(message, cancellationToken).AsTask();
        }

        public Task SendConnectionsAsync(RaidoMessage message, IReadOnlyList<string> connectionIds, CancellationToken cancellationToken) =>
            SendToAllConnectionsAsync(message,
                (connection, state) => ((IReadOnlyList<string>)state!).Contains(connection.ConnectionId), connectionIds, cancellationToken);

        private Task SendToAllConnectionsAsync(
            RaidoMessage message, Func<RaidoConnectionContext, object?, bool>? include, object? state = null, CancellationToken cancellationToken = default)
        {
            List<Task>? tasks = null;
            foreach (var connection in _connections.Where(connection => include == null || include(connection, state)))
            {
                var task = connection.WriteAsync(message, cancellationToken);

                if (!task.IsCompletedSuccessfully)
                {
                    tasks ??= [];

                    tasks.Add(task.AsTask());
                }
                else
                {
                    // If it's a IValueTaskSource backed ValueTask,
                    // inform it its result has been read so it can reset
                    task.GetAwaiter().GetResult();
                }
            }
            
            if (tasks == null)
            {
                return Task.CompletedTask;
            }

            // Some connections are slow
            return Task.WhenAll(tasks);
        }
    }
}