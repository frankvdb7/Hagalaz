using System.Diagnostics.CodeAnalysis;

namespace Hagalaz.Authorization.Messages;

/// <summary>
/// Contains the existing password-validation result and, when successful, its subject.
/// </summary>
public record ValidateUserCredentialsResponseMessage
{
    [MemberNotNullWhen(true, nameof(Subject))]
    public bool Succeeded { get; init; }
    public bool IsLockedOut { get; init; }
    public bool IsDisabled { get; init; }
    public bool AreCredentialsInvalid { get; init; }
    public bool IsNotAllowed { get; init; }
    public string? Subject { get; init; }
}
