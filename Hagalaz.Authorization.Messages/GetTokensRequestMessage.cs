namespace Hagalaz.Authorization.Messages
{
    /// <summary>
    /// Represents a request to retrieve all authorization tokens for a specific user and client.
    /// </summary>
    /// <param name="ClientId">The unique identifier of the client application.</param>
    /// <param name="Subject">The unique subject identifier of the user.</param>
    public record GetTokensRequestMessage(string ClientId, string Subject)
    {
        /// <summary>
        /// Gets or sets the optional status to filter the tokens by (e.g., "valid", "expired").
        /// </summary>
        public string? Status { get; init; }
    }
}
