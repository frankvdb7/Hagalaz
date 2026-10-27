using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using OpenIddict.Abstractions;

namespace Hagalaz.Services.Authorization.Services;

internal sealed class AccessTokenClaimDestinationService
{
    public void IncludeRoleClaimsWhenGranted(ClaimsPrincipal principal, IEnumerable<string> grantedScopes)
    {
        if (!grantedScopes.Contains(OpenIddictConstants.Scopes.Roles, StringComparer.Ordinal))
        {
            return;
        }

        foreach (var claim in principal.FindAll(OpenIddictConstants.Claims.Role))
        {
            claim.SetDestinations(OpenIddictConstants.Destinations.AccessToken);
        }
    }
}
