using Hagalaz.Contacts.Messages.Model;

namespace Hagalaz.Contacts.Messages
{
    public record ContactRemovedMessage(ContactDto Master, ContactDto Contact);
}