using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Hagalaz.Authorization.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Hagalaz.Services.GameWorld.Factories
{
    public class ClaimsPrincipalFactory : IClaimsPrincipalFactory
    {
        private readonly IOptions<IdentityOptions> _identiyOptions;

        public ClaimsPrincipalFactory(IOptions<IdentityOptions> identiyOptions) => _identiyOptions = identiyOptions;

        public ClaimsPrincipal Create(IDictionary<string, object> Claims)
        {
            var claimIdentityOptions = _identiyOptions.Value.ClaimsIdentity;
            return new ClaimsPrincipal(new ClaimsIdentity(Claims.SelectMany(SelectManyClaims).ToList(),
                    AuthenticationType.DefaultAuthenticationType,
                    claimIdentityOptions.UserNameClaimType,
                    claimIdentityOptions.RoleClaimType));
        }

        private static IEnumerable<Claim> SelectManyClaims(KeyValuePair<string, object> claimPair)
        {
            var key = claimPair.Key;
            var value = claimPair.Value;
            if (value is IEnumerable<object> enumerable)
            {
                foreach (var claim in enumerable.Select(v => new Claim(key, v?.ToString() ?? string.Empty)))
                {
                    yield return claim;
                }
            }
            else
            {
                yield return new Claim(key, value?.ToString() ?? string.Empty);
            }
        }
    }
}
