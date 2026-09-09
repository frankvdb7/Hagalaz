using Hagalaz.Collections;
using Hagalaz.Services.Contacts.Store.Model;

namespace Hagalaz.Services.Contacts.Store
{
    public class ContactSessionStore : ConcurrentStore<uint, ContactSessionContext>
    {
        private readonly object _sessionGate = new();

        public bool TryAddSession(ContactSessionContext session)
        {
            lock (_sessionGate)
            {
                return TryAdd(session.MasterId, session);
            }
        }

        public bool TryReplaceSession(ContactSessionContext session)
        {
            lock (_sessionGate)
            {
                var existing = GetOrDefault(session.MasterId);
                if (existing?.ConnectionId == session.ConnectionId)
                {
                    return false;
                }

                this[session.MasterId] = session;
                return true;
            }
        }

        public bool TryRemoveSession(uint masterId, string connectionId)
        {
            lock (_sessionGate)
            {
                if (!TryGetValue(masterId, out var session) || session.ConnectionId != connectionId)
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
