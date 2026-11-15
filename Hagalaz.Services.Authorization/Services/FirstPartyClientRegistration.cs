using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OpenIddict.Abstractions;
using OpenIddictConstants = OpenIddict.Abstractions.OpenIddictConstants;
using HagalazScopes = Hagalaz.Authorization.Constants.Scopes;

namespace Hagalaz.Services.Authorization.Services
{
    public sealed class FirstPartyClientRegistration
    {
        private const string NormalClientId = "storm-app";
        private const string AdminClientId = "storm-admin";
        private const string CacheServiceClientId = "cache-service-1";
        private const string IntrospectionClientSecretConfigurationKey = "Security:Introspection:ClientSecret";

        private readonly IOpenIddictApplicationManager _applicationManager;
        private readonly IConfiguration _configuration;

        public FirstPartyClientRegistration(
            IOpenIddictApplicationManager applicationManager,
            IConfiguration configuration)
        {
            _applicationManager = applicationManager;
            _configuration = configuration;
        }

        public async Task RegisterIfMissingAsync(CancellationToken cancellationToken = default)
        {
            var cacheServiceClientSecret = _configuration[IntrospectionClientSecretConfigurationKey];
            if (string.IsNullOrWhiteSpace(cacheServiceClientSecret))
            {
                throw new InvalidOperationException(
                    $"The {IntrospectionClientSecretConfigurationKey} setting must be configured to register the Cache API introspection client.");
            }

            var cacheServiceClient = await _applicationManager.FindByClientIdAsync(CacheServiceClientId, cancellationToken);

            var normalClient = await _applicationManager.FindByClientIdAsync(NormalClientId, cancellationToken);
            if (normalClient is null)
            {
                await _applicationManager.CreateAsync(
                    CreateDescriptor(NormalClientId, "Hagalaz Web App", HagalazScopes.CharactersApi),
                    cancellationToken);
            }
            else
            {
                await RemoveAdminScopeFromNormalClientAsync(normalClient, cancellationToken);
            }

            if (await _applicationManager.FindByClientIdAsync(AdminClientId, cancellationToken) is null)
            {
                await _applicationManager.CreateAsync(
                    CreateDescriptor(
                        AdminClientId,
                        "Hagalaz Admin App",
                        HagalazScopes.CharactersApi,
                        HagalazScopes.CacheApi),
                    cancellationToken);
            }

            if (cacheServiceClient is null)
            {
                await _applicationManager.CreateAsync(
                    CreateCacheServiceDescriptor(cacheServiceClientSecret),
                    cancellationToken);
            }
            else if (!await _applicationManager.ValidateClientSecretAsync(
                         cacheServiceClient,
                         cacheServiceClientSecret,
                         cancellationToken))
            {
                var descriptor = new OpenIddictApplicationDescriptor();
                await _applicationManager.PopulateAsync(descriptor, cacheServiceClient, cancellationToken);
                descriptor.ClientSecret = cacheServiceClientSecret;
                await _applicationManager.UpdateAsync(cacheServiceClient, descriptor, cancellationToken);
            }
        }

        private static OpenIddictApplicationDescriptor CreateCacheServiceDescriptor(string clientSecret)
        {
            var descriptor = new OpenIddictApplicationDescriptor
            {
                ApplicationType = OpenIddictConstants.ApplicationTypes.Web,
                ClientId = CacheServiceClientId,
                ClientSecret = clientSecret,
                ClientType = OpenIddictConstants.ClientTypes.Confidential,
                DisplayName = "Hagalaz Cache Service"
            };

            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Introspection);
            return descriptor;
        }

        private async Task RemoveAdminScopeFromNormalClientAsync(object application, CancellationToken cancellationToken)
        {
            var descriptor = new OpenIddictApplicationDescriptor();
            await _applicationManager.PopulateAsync(descriptor, application, cancellationToken);

            var cacheScopePermission = OpenIddictConstants.Permissions.Prefixes.Scope + HagalazScopes.CacheApi;
            if (descriptor.Permissions.Remove(cacheScopePermission))
            {
                await _applicationManager.UpdateAsync(application, descriptor, cancellationToken);
            }
        }

        private static OpenIddictApplicationDescriptor CreateDescriptor(
            string clientId,
            string displayName,
            params string[] apiScopes)
        {
            var descriptor = new OpenIddictApplicationDescriptor
            {
                ApplicationType = OpenIddictConstants.ApplicationTypes.Web,
                ClientId = clientId,
                ClientType = OpenIddictConstants.ClientTypes.Public,
                DisplayName = displayName
            };

            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Token);
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.EndSession);
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.Password);
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.RefreshToken);
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.Email);
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.Profile);
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.Roles);

            foreach (var scope in apiScopes)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + scope);
            }

            return descriptor;
        }
    }
}
