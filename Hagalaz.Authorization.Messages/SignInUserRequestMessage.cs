using System.Collections.Immutable;

namespace Hagalaz.Authorization.Messages;

public record SignInUserRequestMessage(string Login, string Password, string RemoteIpAddress, string ClientId, ImmutableArray<string> Scopes, ImmutableArray<string> ClientScopes);