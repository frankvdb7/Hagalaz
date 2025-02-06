using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

namespace Hagalaz.Services.GameLogon.Services.Model
{
    public record CharacterSignInResult
    {
        public static CharacterSignInResult Success(ClaimsPrincipal claimsPrincipal) => new()
        {
            Succeeded = true,
            ClaimsPrincipal = claimsPrincipal
        };

        public static CharacterSignInResult Fail { get; } = new();

        public static CharacterSignInResult AlreadyLoggedOn { get; } = new()
        {
            IsAlreadyLoggedOn = true
        };

        public static CharacterSignInResult Disabled { get; } = new()
        {
            IsDisabled = true
        };

        public static CharacterSignInResult LockedOut { get; } = new()
        {
            IsLockedOut = true
        };

        [MemberNotNullWhen(true, nameof(ClaimsPrincipal))]
        public bool Succeeded { get; private init; }
        public bool IsAlreadyLoggedOn { get; private init; }
        public bool IsDisabled { get; private init; }
        public bool IsLockedOut { get; private init; }
        public ClaimsPrincipal? ClaimsPrincipal { get; private init; }

        private CharacterSignInResult()
        {
        }
    }
}