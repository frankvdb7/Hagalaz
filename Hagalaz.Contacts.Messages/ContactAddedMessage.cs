using Hagalaz.Contacts.Messages.Model;

namespace Hagalaz.Contacts.Messages
{
    public record ContactAddedMessage(ContactDto Master, ContactDto Contact);
}