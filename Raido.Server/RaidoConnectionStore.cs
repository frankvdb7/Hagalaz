using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Raido.Server
{
    public class RaidoConnectionStore : IEnumerable<RaidoHubConnectionContext>
    {
        private readonly ConcurrentDictionary<string, RaidoHubConnectionContext> _connections = new(StringComparer.Ordinal);
        
        public RaidoHubConnectionContext? this[string connectionId]
        {
            get
            {
                _connections.TryGetValue(connectionId, out var connection);
                return connection;
            }
        }
        
        public int Count => _connections.Count;
        
        public void Add(RaidoHubConnectionContext connection) => _connections.TryAdd(connection.ConnectionId, connection);

        public void Remove(RaidoHubConnectionContext connection) => _connections.TryRemove(connection.ConnectionId, out _);
        
        public Enumerator GetEnumerator() => new(this);
        IEnumerator<RaidoHubConnectionContext> IEnumerable<RaidoHubConnectionContext>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public readonly struct Enumerator : IEnumerator<RaidoHubConnectionContext>
        {
            private readonly IEnumerator<KeyValuePair<string, RaidoHubConnectionContext>> _enumerator;

            public Enumerator(RaidoConnectionStore hubConnectionList) => _enumerator = hubConnectionList._connections.GetEnumerator();

            public RaidoHubConnectionContext Current => _enumerator.Current.Value;

            object IEnumerator.Current => Current;

            public void Dispose() => _enumerator.Dispose();

            public bool MoveNext() => _enumerator.MoveNext();

            public void Reset() => _enumerator.Reset();
        }
    }
}
