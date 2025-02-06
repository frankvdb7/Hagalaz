namespace Hagalaz.Contacts.Messages
{
    public record ContactMessageNotification
    {
        public record ClaimDto
        {
            public required string Name { get; init; }
        }

        public record SenderDto
        {
            public required uint MasterId { get; init; }
            public required string DisplayName { get; init; }
            public string? PreviousDisplayName { get; init; }
            public IReadOnlyCollection<ClaimDto>? Claims { get; init; }
        }
        public required uint MasterId { get; init; }
        public required int MessageLength { get; init; }
        public required byte[] MessagePayload { get; init; }
        public required SenderDto Sender { get; init; }
    }
}