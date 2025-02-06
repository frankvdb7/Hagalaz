using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Raido.Server
{
    public class RaidoConnectionStore : IEnumerable<RaidoConnectionContext>
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
        
        public void Add(RaidoConnectionContext connection) => _connections.TryAdd(connection.ConnectionId, connection);

        public void Remove(RaidoConnectionContext connection) => _connections.TryRemove(connection.ConnectionId, out _);
        
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