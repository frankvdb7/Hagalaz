using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Hagalaz.Authorization.Messages
{
    /// <summary>
    /// Represents the response to a <see cref="GetUserInfoRequestMessage"/>, containing the user's claims or an error.
    /// </summary>
    public record GetUserInfoResponseMessage 
    {
        /// <summary>
        /// Gets a value indicating whether the request was successful.
        /// If <c>true</c>, the <see cref="Claims"/> property will be populated.
        /// If <c>false</c>, the <see cref="Error"/> property will be populated.
        /// </summary>
        [MemberNotNullWhen(false, nameof(Error))]
        [MemberNotNullWhen(true, nameof(Claims))]
        public bool Succeeded { get; init; }

        /// <summary>
        /// Gets the dictionary of user claims if the request was successful.
        /// </summary>
        public IDictionary<string, object>? Claims { get; init; }

        /// <summary>
        /// Gets the error message if the request failed.
        /// </summary>
        public string? Error { get; init; }
    }
}