using System.Collections.Immutable;

namespace Hagalaz.Authorization.Messages;

/// <summary>
/// Requests validation of credentials against an already-issued authentication.
/// </summary>
/// <param name="Login">The user's login credential.</param>
/// <param name="Password">The user's password.</param>
/// <param name="RemoteIpAddress">The IP address from which the request originated, when available.</param>
/// <param name="Scopes">The scopes used while validating the credentials.</param>
/// <param name="ClientScopes">The client scopes whose valid authentication must already exist.</param>
public record ValidateExistingAuthenticationRequestMessage(
    string Login,
    string Password,
    string? RemoteIpAddress,
    ImmutableArray<string> Scopes,
    ImmutableArray<string> ClientScopes);
