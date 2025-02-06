using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace Hagalaz.Services.Authorization.Model
{
    public record RefreshTokenGrantResult
    {
        public bool Succeeded { get; }
        public AuthenticationProperties? AuthenticationProperties { get;  }
        public ClaimsPrincipal? User { get; }

        public RefreshTokenGrantResult(AuthenticationProperties authenticationProperties) => AuthenticationProperties = authenticationProperties;

        public RefreshTokenGrantResult(ClaimsPrincipal user)
        {
            User = user;
            Succeeded = true;
        }
    }
}