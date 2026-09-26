using System.Collections.Generic;
using Hagalaz.Services.Contacts.Store.Model;

namespace Hagalaz.Services.Contacts.Store
{
    public sealed class ContactSessionStore
    {
        private readonly Dictionary<uint, ContactSessionContext> _sessions = new();
        private readonly object _sessionGate = new();
        public bool TrySetNewerSession(ContactSessionContext session) =>
            TrySetNewerSession(session, out _);

        public bool TrySetNewerSession(ContactSessionContext session, out ContactSessionContext acceptedSession)
        {
            lock (_sessionGate)
            {
                _sessions.TryGetValue(session.MasterId, out var existing);
                if (existing != null && session.SessionGeneration <= existing.SessionGeneration)
                {
                    acceptedSession = null!;
                    return false;
                }

                acceptedSession = session;
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

        public bool TryRemoveExact(ContactSessionContext expectedSession)
        {
            lock (_sessionGate)
            {
                if (!_sessions.TryGetValue(expectedSession.MasterId, out var session) ||
                    session != expectedSession)
                {
                    return false;
                }

                return _sessions.Remove(expectedSession.MasterId);
            }
        }

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

        public IReadOnlyList<ContactSessionContext> RemoveSessionsForWorld(int worldId, string worldInstanceId, long worldGeneration)
        {
            lock (_sessionGate)
            {
                var removed = new List<ContactSessionContext>();
                foreach (var session in _sessions.Values)
                {
                    if (session.WorldId == worldId &&
                        session.WorldInstanceId == worldInstanceId &&
                        session.WorldGeneration == worldGeneration)
                    {
                        removed.Add(session);
                    }
                }

                foreach (var session in removed)
                {
                    ((ICollection<KeyValuePair<uint, ContactSessionContext>>)_sessions)
                        .Remove(new KeyValuePair<uint, ContactSessionContext>(session.MasterId, session));
                }

                return removed;
            }
        }
    }
}
