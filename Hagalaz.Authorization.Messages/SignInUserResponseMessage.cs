using System;
using System.Diagnostics.CodeAnalysis;

namespace Hagalaz.Authorization.Messages
{
    public record SignInUserResponseMessage
    {
        [MemberNotNullWhen(false, nameof(Error))]
        [MemberNotNullWhen(true, nameof(IdToken), nameof(AccessToken), nameof(Scope), nameof(ExpireDate), nameof(TokenType))]
        public bool Succeeded { get; init; }
        public bool IsDisabled { get; init; }
        public bool AreCredentialsInvalid { get; init; }
        public bool IsAuthenticated { get; init; }
        public bool IsLockedOut { get; init; }
        public string? IdToken { get; init; }
        public string? AccessToken { get; init; }
        public string? Scope { get; init; }
        public DateTimeOffset? ExpireDate { get; init; }
        public string? TokenType { get; init; }

        public string? Error { get; init; }
    }
}