using Hagalaz.Contacts.Messages.Model;

namespace Hagalaz.Contacts.Messages
{
    public record GetContactsResponse
    {
        public required uint MasterId { get; init; }
        public required long SessionGeneration { get; init; }
        public required string ConnectionId { get; init; }
        public required IReadOnlyList<ContactDto> Friends { get; init; }
        public required IReadOnlyList<ContactDto> Ignores { get; init; }
        public long ObservationBoundary { get; init; }
    }
}
