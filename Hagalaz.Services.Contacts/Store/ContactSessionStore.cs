using Hagalaz.Collections;
using Hagalaz.Services.Contacts.Store.Model;

namespace Hagalaz.Services.Contacts.Store
{
    public class ContactSessionStore : ConcurrentStore<uint, ContactSessionContext>
    {
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

                if (existing == null)
                {
                    return TryAdd(session.MasterId, session);
                }

                this[session.MasterId] = session;
                return true;
            }
        }

        public bool TryRemoveSession(uint masterId, long sessionGeneration, string connectionId)
        {
            lock (_sessionGate)
            {
                if (!TryGetValue(masterId, out var session) ||
                    session.SessionGeneration != sessionGeneration ||
                    session.ConnectionId != connectionId)
                {
                    return false;
                }

                return TryRemove(masterId, session);
            }
        }

        public bool TryRemoveSession(ContactSessionContext expectedSession)
        {
            lock (_sessionGate)
            {
                return TryRemove(expectedSession.MasterId, expectedSession);
            }
        }
    }
}
