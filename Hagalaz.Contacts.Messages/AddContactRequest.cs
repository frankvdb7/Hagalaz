namespace Hagalaz.Contacts.Messages
{
    public record AddContactRequest
    {
        public required uint MasterId { get; init; }
        public required string ContactDisplayName { get; init; }
        public required bool Ignore { get; init; }
    }
}
