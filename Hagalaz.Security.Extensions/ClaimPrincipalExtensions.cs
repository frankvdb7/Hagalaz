using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Hagalaz.Security.Extensions
{
    public static class ClaimPrincipalExtensions
    {
        public static IEnumerable<Claim> FindAllRoles(this ClaimsPrincipal principal) =>
            principal.Identities.SelectMany(identity => identity.FindAll(identity.RoleClaimType));
    }
}