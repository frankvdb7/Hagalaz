using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

namespace Hagalaz.Services.Authorization.Model
{
    public record PasswordGrantResult
    {
        public static PasswordGrantResult Fail { get; } = new();

        public static PasswordGrantResult LockedOut { get; } = new()
        {
            IsLockedOut = true
        };
        
        public static PasswordGrantResult Disabled { get; } = new()
        {
            IsDisabled = true
        };
        public static PasswordGrantResult CredentialsInvalid { get; } = new()
        {
            AreCredentialsInvalid = true
        };

        public static PasswordGrantResult NotAllowed { get; } = new()
        {
            IsNotAllowed = true
        };

        [MemberNotNullWhen(true, nameof(User))]
        public bool Succeeded { get; }
        public bool IsLockedOut { get; private init; }
        public bool IsDisabled { get; private init; }
        public bool AreCredentialsInvalid { get; private init; }
        public bool IsNotAllowed { get; private init; }
        public ClaimsPrincipal? User { get; }

        private PasswordGrantResult(){}
        
        public PasswordGrantResult(ClaimsPrincipal user)
        {
            User = user;
            Succeeded = true;
        }
    }
}