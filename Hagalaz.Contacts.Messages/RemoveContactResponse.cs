using Hagalaz.Contacts.Messages.Model;

namespace Hagalaz.Contacts.Messages
{
    public record RemoveContactResponse
    {
        public required uint MasterId { get; init; }
        public required ContactDto Contact { get; init; }
    }
}
