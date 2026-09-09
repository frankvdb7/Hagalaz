using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Hagalaz.Services.Contacts.Store.Model;

namespace Hagalaz.Services.Contacts.Store
{
    public sealed class ContactSessionStore : IEnumerable<ContactSessionContext>
    {
        private readonly ConcurrentDictionary<uint, ContactSessionContext> _sessions = new();
        private readonly object _sessionGate = new();

        public bool TrySetNewerSession(ContactSessionContext session)
        {
            lock (_sessionGate)
            {
                var existing = GetOrDefault(session.MasterId);
                if (existing != null && session.SessionGeneration <= existing.SessionGeneration)
                {
                    return false;
                }

                _sessions[session.MasterId] = session;
                return true;
            }
        }

        public bool TryRemoveExact(uint masterId, long sessionGeneration, string connectionId)
        {
            lock (_sessionGate)
            {
                if (!_sessions.TryGetValue(masterId, out var session) ||
                    session.SessionGeneration != sessionGeneration ||
                    session.ConnectionId != connectionId)
                {
                    return false;
                }

                return _sessions.TryRemove(masterId, out _);
            }
        }

        public bool TryRemoveExact(ContactSessionContext expectedSession) =>
            TryRemoveExact(expectedSession.MasterId, expectedSession.SessionGeneration, expectedSession.ConnectionId);

        public bool TryGetValue(uint masterId, out ContactSessionContext session) => _sessions.TryGetValue(masterId, out session!);

        public ContactSessionContext? GetOrDefault(uint masterId) => _sessions.GetValueOrDefault(masterId);

        public IEnumerator<ContactSessionContext> GetEnumerator() => _sessions.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
