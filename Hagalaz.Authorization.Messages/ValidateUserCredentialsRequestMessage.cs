namespace Hagalaz.Authorization.Messages;

/// <summary>
/// Requests credential validation without issuing or inspecting an authorization token.
/// </summary>
public record ValidateUserCredentialsRequestMessage(string Login, string Password);
