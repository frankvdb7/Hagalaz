using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Hagalaz.Data;
using Hagalaz.Data.Extensions;
using Hagalaz.Services.Authorization.Services;
using Hagalaz.Services.Characters.Services;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
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

        [TestMethod]
        public async Task CharactersValidation_WithSameSecret_AcceptsValidTokenAndRejectsOthers()
        {
            await using var host = await TestAuthorizationHost.StartAsync(CacheSecret, CharactersSecret);
            var validToken = await host.IssueTokenAsync("characters-service-1");
            var otherAudienceToken = await host.IssueTokenAsync("some-other-service");

            await using var characters = await CharactersValidationHost.StartAsync(host.BaseUrl, CharactersSecret);

            Assert.AreEqual(HttpStatusCode.OK, await characters.GetProtectedAsync(validToken));
            Assert.AreEqual(HttpStatusCode.Unauthorized, await characters.GetProtectedAsync("not-a-token"));
            Assert.AreEqual(HttpStatusCode.Unauthorized, await characters.GetProtectedAsync(otherAudienceToken));
            Assert.AreEqual(HttpStatusCode.Unauthorized, await characters.GetProtectedAsync(null));
        }

        [TestMethod]
        public async Task CharactersValidation_WithWrongSecret_RejectsOtherwiseValidToken()
        {
            await using var host = await TestAuthorizationHost.StartAsync(CacheSecret, CharactersSecret);
            var validToken = await host.IssueTokenAsync("characters-service-1");

            await using var characters = await CharactersValidationHost.StartAsync(host.BaseUrl, "wrong-secret");

            // The validation handler fails (server error) rather than accepting the token when Authorization rejects its client.
            Assert.AreNotEqual(HttpStatusCode.OK, await characters.GetProtectedAsync(validToken));
        }

        [TestMethod]
        public void CharactersValidation_WithoutSecret_FailsFast()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["services:hagalaz-services-authorization:https:0"] = "https://localhost:1"
                })
                .Build();
            var exception = Assert.Throws<System.InvalidOperationException>(() =>
            {
                using var provider = new ServiceCollection()
                    .AddCharactersTokenValidation(configuration)
                    .BuildServiceProvider();
                _ = provider.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<OpenIddict.Validation.OpenIddictValidationOptions>>().CurrentValue;
            });
            StringAssert.Contains(exception.Message, "Security:Introspection:ClientSecret");
        }

        private sealed class CharactersValidationHost : System.IAsyncDisposable
        {
            private readonly WebApplication _app;
            private readonly HttpClient _client;

            private CharactersValidationHost(WebApplication app)
            {
                _app = app;
                _client = new HttpClient { BaseAddress = new System.Uri(app.Urls.First()) };
            }

            public static async Task<CharactersValidationHost> StartAsync(string authorizationUrl, string clientSecret)
            {
                var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
                builder.WebHost.UseUrls("http://127.0.0.1:0");
                builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["services:hagalaz-services-authorization:http:0"] = authorizationUrl,
                    ["Security:Introspection:ClientSecret"] = clientSecret
                });
                builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
                builder.Services.AddAuthorization();
                builder.Services.AddCharactersTokenValidation(builder.Configuration);

                var app = builder.Build();
                app.UseAuthentication();
                app.UseAuthorization();
                app.MapGet("/protected", () => Results.Ok()).RequireAuthorization();
                await app.StartAsync();
                return new CharactersValidationHost(app);
            }

            public async Task<HttpStatusCode> GetProtectedAsync(string? token)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "protected");
                if (token is not null)
                {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }

                using var response = await _client.SendAsync(request);
                return response.StatusCode;
            }

            public async ValueTask DisposeAsync()
            {
                _client.Dispose();
                await _app.StopAsync();
                await _app.DisposeAsync();
            }
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

            public string BaseUrl => _app.Urls.First();

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
                using var content = new FormUrlEncodedContent(form);
                using var response = await _client.PostAsync(path, content);
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
