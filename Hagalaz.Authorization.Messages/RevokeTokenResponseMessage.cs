using System.Diagnostics.CodeAnalysis;

namespace Hagalaz.Authorization.Messages
{
    public record RevokeTokenResponseMessage
    {
        [MemberNotNullWhen(false, nameof(Error))]
        public bool Succeeded { get; init; }
        public string? Error { get; init; }
    }
}
