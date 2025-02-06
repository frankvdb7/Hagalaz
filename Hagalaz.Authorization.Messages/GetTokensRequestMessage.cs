namespace Hagalaz.Authorization.Messages
{
    public record GetTokensRequestMessage(string ClientId, string Subject)
    {
        public string? Status { get; init; }
    }
}
