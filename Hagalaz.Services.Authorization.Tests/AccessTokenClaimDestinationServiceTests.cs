using System.Security.Claims;
using Hagalaz.Services.Authorization.Services;
using OpenIddict.Abstractions;

namespace Hagalaz.Services.Authorization.Tests;

[TestClass]
public sealed class AccessTokenClaimDestinationServiceTests
{
    [TestMethod]
    public void IncludeRoleClaimsWhenGranted_AddsAccessTokenDestination_WhenRolesScopeGranted()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(OpenIddictConstants.Claims.Role, "SystemAdministrator"),
            new Claim(OpenIddictConstants.Claims.Email, "admin@example.test")
        ]));
        var service = new AccessTokenClaimDestinationService();

        service.IncludeRoleClaimsWhenGranted(principal, [OpenIddictConstants.Scopes.Roles]);

        var roleClaim = principal.FindFirst(OpenIddictConstants.Claims.Role)!;
        var emailClaim = principal.FindFirst(OpenIddictConstants.Claims.Email)!;
        CollectionAssert.AreEquivalent(
            new[] { OpenIddictConstants.Destinations.AccessToken },
            roleClaim.GetDestinations().ToArray());
        Assert.IsFalse(emailClaim.GetDestinations().Contains(OpenIddictConstants.Destinations.AccessToken));
    }

    [TestMethod]
    public void IncludeRoleClaimsWhenGranted_LeavesRoleClaimOutOfAccessToken_WhenRolesScopeOmitted()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(OpenIddictConstants.Claims.Role, "SystemAdministrator")
        ]));
        var service = new AccessTokenClaimDestinationService();

        service.IncludeRoleClaimsWhenGranted(principal, [OpenIddictConstants.Scopes.Profile]);

        var roleClaim = principal.FindFirst(OpenIddictConstants.Claims.Role)!;
        Assert.IsFalse(roleClaim.GetDestinations().Contains(OpenIddictConstants.Destinations.AccessToken));
    }
}
