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
        private const string CharactersServiceClientId = "characters-service-1";
        private const string CacheClientSecretConfigurationKey = "Security:Introspection:ClientSecret";
        private const string CharactersClientSecretConfigurationKey = "Security:Introspection:CharactersClientSecret";

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
            var cacheServiceClientSecret = GetRequiredSecret(CacheClientSecretConfigurationKey, "Cache API");
            var charactersServiceClientSecret = GetRequiredSecret(CharactersClientSecretConfigurationKey, "Characters API");

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

            await RegisterIntrospectionClientAsync(CacheServiceClientId, "Hagalaz Cache Service", cacheServiceClientSecret, cancellationToken);
            await RegisterIntrospectionClientAsync(CharactersServiceClientId, "Hagalaz Characters Service", charactersServiceClientSecret, cancellationToken);
        }

        private string GetRequiredSecret(string configurationKey, string serviceName)
        {
            var secret = _configuration[configurationKey];
            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new InvalidOperationException(
                    $"The {configurationKey} setting must be configured to register the {serviceName} introspection client.");
            }

            return secret;
        }

        private async Task RegisterIntrospectionClientAsync(
            string clientId,
            string displayName,
            string clientSecret,
            CancellationToken cancellationToken)
        {
            var client = await _applicationManager.FindByClientIdAsync(clientId, cancellationToken);
            if (client is null)
            {
                await _applicationManager.CreateAsync(
                    CreateIntrospectionDescriptor(clientId, displayName, clientSecret),
                    cancellationToken);
            }
            else if (!await _applicationManager.ValidateClientSecretAsync(client, clientSecret, cancellationToken))
            {
                var descriptor = new OpenIddictApplicationDescriptor();
                await _applicationManager.PopulateAsync(descriptor, client, cancellationToken);
                descriptor.ClientSecret = clientSecret;
                await _applicationManager.UpdateAsync(client, descriptor, cancellationToken);
            }
        }

        private static OpenIddictApplicationDescriptor CreateIntrospectionDescriptor(string clientId, string displayName, string clientSecret)
        {
            var descriptor = new OpenIddictApplicationDescriptor
            {
                ApplicationType = OpenIddictConstants.ApplicationTypes.Web,
                ClientId = clientId,
                ClientSecret = clientSecret,
                ClientType = OpenIddictConstants.ClientTypes.Confidential,
                DisplayName = displayName
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
