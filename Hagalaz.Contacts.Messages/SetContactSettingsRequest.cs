using Hagalaz.Contacts.Messages.Model;

namespace Hagalaz.Contacts.Messages
{
    public record SetContactSettingsRequest
    {
        public required uint MasterId { get; init; }
        public required ContactSettingsDto Settings { get; init; }
    }
}
