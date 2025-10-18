using System.Diagnostics.CodeAnalysis;

namespace Hagalaz.Authorization.Messages
{
    /// <summary>
    /// Represents the response to a <see cref="RevokeTokenRequestMessage"/>, indicating whether the operation was successful.
    /// </summary>
    public record RevokeTokenResponseMessage
    {
        /// <summary>
        /// Gets a value indicating whether the token revocation was successful.
        /// If <c>false</c>, the <see cref="Error"/> property will be populated.
        /// </summary>
        [MemberNotNullWhen(false, nameof(Error))]
        public bool Succeeded { get; init; }

        /// <summary>
        /// Gets the error message if the revocation failed.
        /// </summary>
        public string? Error { get; init; }
    }
}
