using System;
using System.Collections.Generic;

namespace Hagalaz.Authorization.Messages
{
    /// <summary>
    /// Represents the response to a <see cref="GetTokensRequestMessage"/>, containing a list of authorization tokens.
    /// </summary>
    public record GetTokensResponseMessage
    {
        /// <summary>
        /// Represents a single authorization token with its details.
        /// </summary>
        /// <param name="Id">The unique identifier of the token.</param>
        /// <param name="Subject">The subject (user) to whom the token was issued.</param>
        /// <param name="Type">The type of the token (e.g., "access_token", "refresh_token").</param>
        /// <param name="Status">The current status of the token (e.g., "valid", "revoked").</param>
        /// <param name="CreationDateUTC">The UTC date and time when the token was created.</param>
        /// <param name="ExpirationDateUTC">The UTC date and time when the token expires.</param>
        public record TokenDto(string Id, string Subject, string Type, string Status, DateTime? CreationDateUTC, DateTime? ExpirationDateUTC);

        /// <summary>
        /// Gets the list of tokens returned by the request.
        /// </summary>
        public required IReadOnlyList<TokenDto> Tokens { get; init; }
    }
}
