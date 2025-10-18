using System.Collections.Immutable;

namespace Hagalaz.Authorization.Messages;

/// <summary>
/// Represents a request to sign in a user and obtain authorization tokens.
/// </summary>
/// <param name="Login">The user's login credential (e.g., username or email).</param>
/// <param name="Password">The user's password.</param>
/// <param name="RemoteIpAddress">The IP address from which the sign-in request originated.</param>
/// <param name="ClientId">The unique identifier of the client application making the request.</param>
/// <param name="Scopes">The scopes requested by the user for authorization.</param>
/// <param name="ClientScopes">The scopes requested by the client application itself.</param>
public record SignInUserRequestMessage(string Login, string Password, string RemoteIpAddress, string ClientId, ImmutableArray<string> Scopes, ImmutableArray<string> ClientScopes);