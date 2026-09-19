namespace Hagalaz.Contacts.Messages
{
    public record GetContactsRequest(
        uint MasterId,
        long SessionGeneration,
        string ConnectionId,
        long ObservationBoundary);
}
