using System;
using System.Security.Claims;
using Hagalaz.Authorization.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;

namespace Hagalaz.Services.Cache.Security;

public static class CacheAuthorization
{
    public const string PolicyName = "CacheAdminPolicy";

    public static IServiceCollection AddCacheAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(Roles.SystemAdministrator);
                policy.RequireAssertion(context => HasCacheScope(context.User));
            });
        });

        return services;
    }

    internal static bool HasCacheScope(ClaimsPrincipal principal)
    {
        var scopeClaim = principal.FindFirst(OpenIddictConstants.Claims.Scope)?.Value;
        if (string.IsNullOrWhiteSpace(scopeClaim))
        {
            return false;
        }

        return scopeClaim.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(Scopes.CacheApi, StringComparer.Ordinal);
    }
}

