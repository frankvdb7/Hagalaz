using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Raido.Common.Protocol;

namespace Raido.Server
{
    public class RaidoConnectionStore : IEnumerable<RaidoConnectionContext>, IDisposable
    {
        private readonly ConcurrentDictionary<string, RaidoConnectionContext> _connections = new(StringComparer.Ordinal);
        
        public RaidoConnectionContext? this[string connectionId]
        {
            get
            {
                _connections.TryGetValue(connectionId, out var connection);
                return connection;
            }
        }
        
        public int Count => _connections.Count;
        
        public void Add(RaidoConnectionContext connection)
        {
            _connections.TryAdd(connection.ConnectionId, connection);
        }

        public void Remove(RaidoConnectionContext connection)
        {
            _connections.TryRemove(connection.ConnectionId, out _);
        }

        /// <summary>
        /// Prepares a replacement physical transport for a retained logical connection.
        /// The returned reservation is committed by the Raido connection handler after it has
        /// quiesced the replacement input and captured all unread bytes.
        /// </summary>
        public ValueTask<RaidoRebindReservation?> TryPrepareRebindAsync(string connectionId, RaidoConnectionContext replacement)
        {
            ArgumentNullException.ThrowIfNull(connectionId);
            ArgumentNullException.ThrowIfNull(replacement);

            return _connections.TryGetValue(connectionId, out var connection)
                ? connection.TryPrepareRebindAsync(replacement, replacementProtocol: null)
                : ValueTask.FromResult<RaidoRebindReservation?>(null);
        }

        /// <summary>
        /// Prepares a replacement physical transport and protocol for a retained logical connection.
        /// </summary>
        public ValueTask<RaidoRebindReservation?> TryPrepareRebindAsync(
            string connectionId,
            RaidoConnectionContext replacement,
            IRaidoProtocol replacementProtocol)
        {
            ArgumentNullException.ThrowIfNull(connectionId);
            ArgumentNullException.ThrowIfNull(replacement);
            ArgumentNullException.ThrowIfNull(replacementProtocol);

            return _connections.TryGetValue(connectionId, out var connection)
                ? connection.TryPrepareRebindAsync(replacement, replacementProtocol)
                : ValueTask.FromResult<RaidoRebindReservation?>(null);
        }

        /// <summary>
        /// Prepares the caller's physical transport and protocol for a retained logical connection.
        /// </summary>
        public ValueTask<RaidoRebindReservation?> TryPrepareRebindAsync(
            string connectionId,
            RaidoCallerContext replacement,
            IRaidoProtocol replacementProtocol)
        {
            ArgumentNullException.ThrowIfNull(replacement);

            return _connections.TryGetValue(replacement.ConnectionId, out var replacementConnection)
                ? TryPrepareRebindAsync(connectionId, replacementConnection, replacementProtocol)
                : ValueTask.FromResult<RaidoRebindReservation?>(null);
        }

        /// <summary>
        /// Closes every retained logical connection.
        /// </summary>
        public void Dispose()
        {
            foreach (var connection in _connections.Values)
            {
                connection.Abort();
            }

            _connections.Clear();
        }
        
        public Enumerator GetEnumerator() => new(this);
        IEnumerator<RaidoConnectionContext> IEnumerable<RaidoConnectionContext>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public readonly struct Enumerator : IEnumerator<RaidoConnectionContext>
        {
            private readonly IEnumerator<KeyValuePair<string, RaidoConnectionContext>> _enumerator;

            public Enumerator(RaidoConnectionStore hubConnectionList) => _enumerator = hubConnectionList._connections.GetEnumerator();

            public RaidoConnectionContext Current => _enumerator.Current.Value;

            object IEnumerator.Current => Current;

            public void Dispose() => _enumerator.Dispose();

            public bool MoveNext() => _enumerator.MoveNext();

            public void Reset() => _enumerator.Reset();
        }
    }
}
