using System.Security.Claims;
using Hagalaz.Authorization.Constants;
using Hagalaz.Services.Cache.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenIddict.Abstractions;

namespace Hagalaz.Services.Cache.Tests.Security;

[TestClass]
public class CacheAuthorizationTests
{
    [TestMethod]
    public void AddCacheAuthorization_RegistersExpectedRequirements()
    {
        var services = new ServiceCollection();

        services.AddCacheAuthorization();

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AuthorizationOptions>>();
        var policy = options.Value.GetPolicy(CacheAuthorization.PolicyName);

        Assert.IsNotNull(policy);
        Assert.IsTrue(policy.Requirements.OfType<DenyAnonymousAuthorizationRequirement>().Any());

        var roleRequirement = policy.Requirements.OfType<RolesAuthorizationRequirement>().Single();
        CollectionAssert.AreEquivalent(new[] { Roles.SystemAdministrator }, roleRequirement.AllowedRoles.ToArray());
        Assert.IsTrue(policy.Requirements.OfType<AssertionRequirement>().Any());
    }

    [TestMethod]
    public async Task CacheAdminPolicy_RequiresAdministratorRoleAndCacheScope()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCacheAuthorization();

        using var provider = services.BuildServiceProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();
        var administrator = CreatePrincipal(new[]
        {
            new Claim(OpenIddictConstants.Claims.Role, Roles.SystemAdministrator),
            new Claim(OpenIddictConstants.Claims.Scope, Scopes.CacheApi)
        });
        var nonAdministrator = CreatePrincipal(new[]
        {
            new Claim(OpenIddictConstants.Claims.Role, "Player"),
            new Claim(OpenIddictConstants.Claims.Scope, Scopes.CacheApi)
        });
        var administratorWithoutCacheScope = CreatePrincipal(new[]
        {
            new Claim(OpenIddictConstants.Claims.Role, Roles.SystemAdministrator),
            new Claim(OpenIddictConstants.Claims.Scope, Scopes.CharactersApi)
        });

        Assert.IsTrue((await authorization.AuthorizeAsync(administrator, CacheAuthorization.PolicyName)).Succeeded);
        Assert.IsFalse((await authorization.AuthorizeAsync(nonAdministrator, CacheAuthorization.PolicyName)).Succeeded);
        Assert.IsFalse((await authorization.AuthorizeAsync(administratorWithoutCacheScope, CacheAuthorization.PolicyName)).Succeeded);
    }

    [TestMethod]
    public void HasCacheScope_ReturnsTrue_WhenScopeClaimContainsCacheApi()
    {
        var principal = CreatePrincipal(new[]
        {
            new Claim(OpenIddictConstants.Claims.Scope, Scopes.CacheApi)
        });

        Assert.IsTrue(CacheAuthorization.HasCacheScope(principal));
    }

    [TestMethod]
    public void HasCacheScope_ReturnsFalse_WhenScopeClaimMissingCacheApi()
    {
        var principal = CreatePrincipal(new[]
        {
            new Claim(OpenIddictConstants.Claims.Scope, Scopes.CharactersApi)
        });

        Assert.IsFalse(CacheAuthorization.HasCacheScope(principal));
    }

    private static ClaimsPrincipal CreatePrincipal(IEnumerable<Claim> claims)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(
            claims,
            "Bearer",
            OpenIddictConstants.Claims.Name,
            OpenIddictConstants.Claims.Role));
    }
}
