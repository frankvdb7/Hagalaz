using System;
using System.Diagnostics.CodeAnalysis;

namespace Hagalaz.Authorization.Messages
{
    /// <summary>
    /// Represents the response to a <see cref="SignInUserRequestMessage"/>, containing either the authentication tokens or an error message.
    /// </summary>
    public record SignInUserResponseMessage
    {
        /// <summary>
        /// Gets a value indicating whether the sign-in was successful.
        /// If <c>true</c>, the token properties will be populated. If <c>false</c>, the <see cref="Error"/> property will be populated.
        /// </summary>
        [MemberNotNullWhen(false, nameof(Error))]
        [MemberNotNullWhen(true, nameof(IdToken), nameof(AccessToken), nameof(Scope), nameof(ExpireDate), nameof(TokenType))]
        public bool Succeeded { get; init; }

        /// <summary>
        /// Gets a value indicating whether the user's account is disabled.
        /// </summary>
        public bool IsDisabled { get; init; }

        /// <summary>
        /// Gets a value indicating whether the provided credentials were invalid.
        /// </summary>
        public bool AreCredentialsInvalid { get; init; }

        /// <summary>
        /// Gets a value indicating whether the user was successfully authenticated.
        /// </summary>
        public bool IsAuthenticated { get; init; }

        /// <summary>
        /// Gets a value indicating whether the user's account is locked out.
        /// </summary>
        public bool IsLockedOut { get; init; }

        /// <summary>
        /// Gets the ID token if the sign-in was successful.
        /// </summary>
        public string? IdToken { get; init; }

        /// <summary>
        /// Gets the access token if the sign-in was successful.
        /// </summary>
        public string? AccessToken { get; init; }

        /// <summary>
        /// Gets the granted scopes if the sign-in was successful.
        /// </summary>
        public string? Scope { get; init; }

        /// <summary>
        /// Gets the expiration date and time of the tokens if the sign-in was successful.
        /// </summary>
        public DateTimeOffset? ExpireDate { get; init; }

        /// <summary>
        /// Gets the type of the token (e.g., "Bearer").
        /// </summary>
        public string? TokenType { get; init; }

        /// <summary>
        /// Gets the error message if the sign-in failed.
        /// </summary>
        public string? Error { get; init; }
    }
}