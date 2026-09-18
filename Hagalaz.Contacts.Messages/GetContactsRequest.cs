namespace Hagalaz.Contacts.Messages
{
    public record GetContactsRequest(uint MasterId, long ObservationBoundary = 0);
}
