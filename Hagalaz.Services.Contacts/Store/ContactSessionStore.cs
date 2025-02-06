using Hagalaz.Collections;
using Hagalaz.Services.Contacts.Store.Model;

namespace Hagalaz.Services.Contacts.Store
{
    public class ContactSessionStore : ConcurrentStore<uint, ContactSessionContext>
    {
    }
}
