using Hagalaz.Contacts.Messages.Model;

namespace Hagalaz.Contacts.Messages
{
    public record ContactSignInMessage(ContactDto Contact, long SessionGeneration, string ConnectionId);
}
