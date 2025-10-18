namespace Hagalaz.Authorization.Messages
{
    /// <summary>
    /// Represents a request to revoke all authorization tokens for a specific user and client.
    /// </summary>
    /// <param name="ClientId">The unique identifier of the client application.</param>
    /// <param name="Subject">The unique subject identifier of the user whose tokens should be revoked.</param>
    public record RevokeTokenRequestMessage(string ClientId, string Subject);
}