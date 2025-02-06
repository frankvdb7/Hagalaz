namespace Hagalaz.Authorization.Messages
{
    public record RevokeTokenRequestMessage(string ClientId, string Subject);
}