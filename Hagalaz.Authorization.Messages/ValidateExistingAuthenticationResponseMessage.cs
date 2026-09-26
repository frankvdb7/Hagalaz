namespace Hagalaz.Authorization.Messages;

/// <summary>
/// Reports the result of validating credentials against an existing authentication.
/// </summary>
public record ValidateExistingAuthenticationResponseMessage
{
    public bool Succeeded { get; init; }
    public string? Subject { get; init; }
    public bool AreCredentialsInvalid { get; init; }
    public bool IsDisabled { get; init; }
    public bool IsLockedOut { get; init; }
}
