using System;
using System.Collections.Generic;

namespace Hagalaz.Authorization.Messages
{
    public record GetTokensResponseMessage
    {
        public record TokenDto(string Id, string Subject, string Type, string Status, DateTime? CreationDateUTC, DateTime? ExpirationDateUTC);

        public required IReadOnlyList<TokenDto> Tokens { get; init; }
    }
}
