using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Hagalaz.Authorization.Messages
{
    public record GetUserInfoResponseMessage 
    {
        [MemberNotNullWhen(false, nameof(Error))]
        [MemberNotNullWhen(true, nameof(Claims))]
        public bool Succeeded { get; init; }
        public IDictionary<string, object>? Claims { get; init; }
        public string? Error { get; init; }
    }
}