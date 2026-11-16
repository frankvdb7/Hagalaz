namespace Hagalaz.Contacts.Messages
{
    /// <summary>
    /// Alternative response to <see cref="AddContactRequest"/> and <see cref="RemoveContactRequest"/>
    /// when the contact service rejected the mutation (self-targeting or a failed commit).
    /// </summary>
    public record ContactMutationFailedResponse
    {
        public required uint MasterId { get; init; }
    }
}
