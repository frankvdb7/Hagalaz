using System.Collections.Generic;
using Hagalaz.Services.Contacts.Store.Model;

namespace Hagalaz.Services.Contacts.Store
{
    public sealed class ContactSessionStore
    {
        private readonly Dictionary<uint, ContactSessionContext> _sessions = new();
        private readonly object _sessionGate = new();

        public bool TrySetNewerSession(ContactSessionContext session)
        {
            lock (_sessionGate)
            {
                _sessions.TryGetValue(session.MasterId, out var existing);
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

                return _sessions.Remove(masterId);
            }
        }

        public bool TryRemoveExact(ContactSessionContext expectedSession) =>
            TryRemoveExact(expectedSession.MasterId, expectedSession.SessionGeneration, expectedSession.ConnectionId);

        public bool TryGetValue(uint masterId, out ContactSessionContext session)
        {
            lock (_sessionGate)
            {
                return _sessions.TryGetValue(masterId, out session!);
            }
        }

        public ContactSessionContext? GetOrDefault(uint masterId)
        {
            lock (_sessionGate)
            {
                return _sessions.GetValueOrDefault(masterId);
            }
        }

        public IReadOnlyList<ContactSessionContext> RemoveSessionsForWorld(int worldId)
        {
            lock (_sessionGate)
            {
                var removed = new List<ContactSessionContext>();
                foreach (var session in _sessions.Values)
                {
                    if (session.WorldId == worldId)
                    {
                        removed.Add(session);
                    }
                }

                foreach (var session in removed)
                {
                    _sessions.Remove(session.MasterId);
                }

                return removed;
            }
        }
    }
}
