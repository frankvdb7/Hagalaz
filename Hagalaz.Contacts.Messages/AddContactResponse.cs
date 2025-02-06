using Hagalaz.Contacts.Messages.Model;

namespace Hagalaz.Contacts.Messages
{
    public class AddContactResponse
    {
        public required uint MasterId { get; init; }
        public required ContactDto Contact { get; init; }
    }
}
