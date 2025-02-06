namespace Hagalaz.Contacts.Messages
{
    public record AddContactMessageRequest
    {
        public required uint MasterId { get; init; }
        public required string ContactDisplayName { get; init; }
        public required int MessageLength { get; init; }
        public required byte[] MessagePayload { get; init; }
    }
}