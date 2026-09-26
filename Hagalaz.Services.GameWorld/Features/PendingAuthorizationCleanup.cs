namespace Hagalaz.Services.GameWorld.Features;

public sealed record PendingAuthorizationCleanup(
    string ClientId,
    string Subject,
    string AuthorizationId);
