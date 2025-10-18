namespace Hagalaz.Authorization.Messages
{
    /// <summary>
    /// Represents a request to retrieve user information using an access token.
    /// </summary>
    /// <param name="AccessToken">The access token used to authorize the request.</param>
    public record GetUserInfoRequestMessage(string AccessToken);
}