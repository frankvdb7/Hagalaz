using System.Security.Claims;
using Microsoft.AspNetCore.Connections.Features;

namespace Hagalaz.Services.GameWorld.Features
{
    public class AuthenticationFeature : IAuthenticationFeature, IConnectionUserFeature
    {
        public required AuthenticationProperties AuthenticationProperties { get; init; }
        public ClaimsPrincipal? User { get; set; }

    }
}
