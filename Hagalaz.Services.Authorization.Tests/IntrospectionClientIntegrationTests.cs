using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Hagalaz.Data;
using Hagalaz.Data.Extensions;
using Hagalaz.Services.Authorization.Services;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;
using HagalazScopes = Hagalaz.Authorization.Constants.Scopes;

namespace Hagalaz.Services.Authorization.Tests
{
    /// <summary>
    ///     Exercises the real <see cref="FirstPartyClientRegistration" /> and the real OpenIddict server introspection endpoint
    ///     (in-memory store, loopback Kestrel) the same way Characters API and Cache API call it.
    /// </summary>
    [TestClass]
    public sealed class IntrospectionClientIntegrationTests
    {
        private const string CacheSecret = "cache-integration-secret";
        private const string CharactersSecret = "characters-integration-secret";

        [TestMethod]
        public async Task CharactersClient_WithConfiguredSecret_IntrospectsValidTokenAndRejectsOthers()
        {
            await using var host = await TestAuthorizationHost.StartAsync(CacheSecret, CharactersSecret);

            var validToken = await host.IssueTokenAsync("characters-service-1");
            var otherAudienceToken = await host.IssueTokenAsync("some-other-service");

            using var valid = await host.IntrospectAsync("characters-service-1", CharactersSecret, validToken);
            Assert.AreEqual(HttpStatusCode.OK, valid.StatusCode);
            Assert.IsTrue(valid.Json.GetProperty("active").GetBoolean());

            using var wrongSecret = await host.IntrospectAsync("characters-service-1", "wrong-secret", validToken);
            Assert.AreEqual(HttpStatusCode.Unauthorized, wrongSecret.StatusCode);
            Assert.AreEqual("invalid_client", wrongSecret.Json.GetProperty("error").GetString());

            using var cacheSecretForCharacters = await host.IntrospectAsync("characters-service-1", CacheSecret, validToken);
            Assert.AreEqual(HttpStatusCode.Unauthorized, cacheSecretForCharacters.StatusCode);

            using var garbage = await host.IntrospectAsync("characters-service-1", CharactersSecret, "not-a-token");
            Assert.IsFalse(garbage.Json.GetProperty("active").GetBoolean());

            using var wrongAudience = await host.IntrospectAsync("characters-service-1", CharactersSecret, otherAudienceToken);
            Assert.IsFalse(wrongAudience.Json.GetProperty("active").GetBoolean());
        }

        [TestMethod]
        public async Task CacheClient_StillIntrospectsWithItsOwnSecret()
        {
            await using var host = await TestAuthorizationHost.StartAsync(CacheSecret, CharactersSecret);
            var token = await host.IssueTokenAsync("cache-service-1");

            using var valid = await host.IntrospectAsync("cache-service-1", CacheSecret, token);
            Assert.IsTrue(valid.Json.GetProperty("active").GetBoolean());

            using var charactersSecret = await host.IntrospectAsync("cache-service-1", CharactersSecret, token);
            Assert.AreEqual(HttpStatusCode.Unauthorized, charactersSecret.StatusCode);
        }

        [TestMethod]
        public async Task CharactersClient_CannotRequestTokens()
        {
            await using var host = await TestAuthorizationHost.StartAsync(CacheSecret, CharactersSecret);

            using var response = await host.PostFormAsync("connect/token", new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = "characters-service-1",
                ["client_secret"] = CharactersSecret,
                ["scope"] = HagalazScopes.CharactersApi
            });

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.AreEqual("unauthorized_client", response.Json.GetProperty("error").GetString());
        }

        [TestMethod]
        public async Task CharactersClient_SecretRotation_ReplacesStoredSecret()
        {
            await using var host = await TestAuthorizationHost.StartAsync(CacheSecret, CharactersSecret);
            var token = await host.IssueTokenAsync("characters-service-1");

            await host.RegisterAsync(CacheSecret, "rotated-characters-secret");

            using var oldSecret = await host.IntrospectAsync("characters-service-1", CharactersSecret, token);
            Assert.AreEqual(HttpStatusCode.Unauthorized, oldSecret.StatusCode);

            using var newSecret = await host.IntrospectAsync("characters-service-1", "rotated-characters-secret", token);
            Assert.IsTrue(newSecret.Json.GetProperty("active").GetBoolean());
        }

        private sealed class JsonResponse : System.IDisposable
        {
            private readonly JsonDocument _document;

            public JsonResponse(HttpStatusCode statusCode, JsonDocument document)
            {
                StatusCode = statusCode;
                _document = document;
            }

            public HttpStatusCode StatusCode { get; }
            public JsonElement Json => _document.RootElement;
            public void Dispose() => _document.Dispose();
        }

        private sealed class TestAuthorizationHost : System.IAsyncDisposable
        {
            private const string IssuerClientId = "test-token-issuer";
            private const string IssuerClientSecret = "test-token-issuer-secret";

            private readonly WebApplication _app;
            private readonly HttpClient _client;

            private TestAuthorizationHost(WebApplication app)
            {
                _app = app;
                _client = new HttpClient { BaseAddress = new System.Uri(app.Urls.First()) };
            }

            public static async Task<TestAuthorizationHost> StartAsync(string cacheSecret, string charactersSecret)
            {
                var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
                builder.WebHost.UseUrls("http://127.0.0.1:0");
                var databaseName = System.Guid.NewGuid().ToString();
                builder.Services.AddDbContext<HagalazDbContext>(options =>
                {
                    options.UseInMemoryDatabase(databaseName);
                    options.UseOpenIddict();
                });
                builder.Services.AddAuthentication();
                builder.Services.AddOpenIddict()
                    .AddHagalazOpenIddictCore(_ => { })
                    .AddServer(options =>
                    {
                        options.SetTokenEndpointUris("connect/token").SetIntrospectionEndpointUris("connect/introspect");
                        options.AllowClientCredentialsFlow();
                        options.RegisterScopes(HagalazScopes.CharactersApi, HagalazScopes.CacheApi);
                        options.AddEphemeralEncryptionKey().AddEphemeralSigningKey();
                        options.UseAspNetCore()
                            .EnableTokenEndpointPassthrough()
                            .DisableTransportSecurityRequirement();
                    });

                var app = builder.Build();
                app.UseRouting();
                app.UseAuthentication();
                app.MapPost("/connect/token", (HttpContext context) =>
                {
                    var request = context.GetOpenIddictServerRequest()!;
                    var identity = new ClaimsIdentity("test", Claims.Name, Claims.Role);
                    identity.AddClaim(new Claim(Claims.Subject, request.ClientId!));
                    var principal = new ClaimsPrincipal(identity);
                    principal.SetScopes(request.GetScopes());
                    principal.SetResources(request.GetParameter("token_audience")?.ToString() ?? "unspecified");
                    principal.SetDestinations(_ => new[] { Destinations.AccessToken });
                    return Results.SignIn(principal, authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                });

                await app.StartAsync();
                var host = new TestAuthorizationHost(app);
                await host.RegisterAsync(cacheSecret, charactersSecret);
                return host;
            }

            public async Task RegisterAsync(string cacheSecret, string charactersSecret)
            {
                await using var scope = _app.Services.CreateAsyncScope();
                var configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Security:Introspection:ClientSecret"] = cacheSecret,
                        ["Security:Introspection:CharactersClientSecret"] = charactersSecret
                    })
                    .Build();
                var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
                await new FirstPartyClientRegistration(manager, configuration).RegisterIfMissingAsync();

                // A token issuer exists only in this test host so tokens with chosen audiences can be introspected.
                if (await manager.FindByClientIdAsync(IssuerClientId) is null)
                {
                    await manager.CreateAsync(new OpenIddictApplicationDescriptor
                    {
                        ClientId = IssuerClientId,
                        ClientSecret = IssuerClientSecret,
                        ClientType = ClientTypes.Confidential,
                        Permissions =
                        {
                            Permissions.Endpoints.Token,
                            Permissions.GrantTypes.ClientCredentials,
                            Permissions.Prefixes.Scope + HagalazScopes.CharactersApi
                        }
                    });
                }
            }

            public async Task<string> IssueTokenAsync(string audience)
            {
                using var response = await PostFormAsync("connect/token", new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = IssuerClientId,
                    ["client_secret"] = IssuerClientSecret,
                    ["scope"] = HagalazScopes.CharactersApi,
                    ["token_audience"] = audience
                });
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, response.Json.ToString());
                return response.Json.GetProperty("access_token").GetString()!;
            }

            public Task<JsonResponse> IntrospectAsync(string clientId, string clientSecret, string token) =>
                PostFormAsync("connect/introspect", new Dictionary<string, string>
                {
                    ["client_id"] = clientId,
                    ["client_secret"] = clientSecret,
                    ["token"] = token
                });

            public async Task<JsonResponse> PostFormAsync(string path, Dictionary<string, string> form)
            {
                using var response = await _client.PostAsync(path, new FormUrlEncodedContent(form));
                var json = await response.Content.ReadAsStringAsync();
                return new JsonResponse(response.StatusCode, JsonDocument.Parse(json));
            }

            public async ValueTask DisposeAsync()
            {
                _client.Dispose();
                await _app.StopAsync();
                await _app.DisposeAsync();
            }
        }
    }
}
