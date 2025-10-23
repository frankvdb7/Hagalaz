using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Hagalaz.Security.Extensions
{
    /// <summary>
    /// Provides extension methods for the <see cref="ClaimsPrincipal"/> class, simplifying access to claims.
    /// </summary>
    public static class ClaimPrincipalExtensions
    {
        /// <summary>
        /// Finds all role claims from all identities within a <see cref="ClaimsPrincipal"/>.
        /// </summary>
        /// <param name="principal">The claims principal to search for role claims.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="Claim"/> that represent the roles of the principal.</returns>
        public static IEnumerable<Claim> FindAllRoles(this ClaimsPrincipal principal) =>
            principal.Identities.SelectMany(identity => identity.FindAll(identity.RoleClaimType));
    }
}