using System;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Quartz;
using Refit;
using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Data.Extensions;
using Hagalaz.ServiceDefaults;
using static OpenIddict.Abstractions.OpenIddictConstants;
using OpenIddict.Abstractions;
using System.Threading;
using Hagalaz.Services.Authorization.Api;
using Hagalaz.Services.Authorization.Config;
using Hagalaz.Services.Authorization.Consumers;
using Hagalaz.Services.Authorization.Data;
using Hagalaz.Services.Authorization.Identity;
using Hagalaz.Services.Authorization.Mediator.Consumers;
using Hagalaz.Services.Authorization.Services;

namespace Hagalaz.Services.Authorization
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // aspire
            builder.AddServiceDefaults();
            builder.AddHagalazDbContextPool("hagalaz-db");

            // captcha
            builder.Services.Configure<CaptchaOptions>(builder.Configuration.GetSection(CaptchaOptions.Captcha));
            builder.Services.AddRefitClient<IHCaptchaApi>()
                .ConfigureHttpClient((provider, c) =>
                {
                    var options = provider.GetRequiredService<IOptions<CaptchaOptions>>().Value;
                    c.BaseAddress = new Uri(options.ApiBaseUrl);
                });
            builder.Services.AddTransient<ICaptchaService, HCaptchaService>();
            builder.Services.AddTransient<ICharacterUnitOfWork, CharacterUnitOfWork>();
            builder.Services.AddTransient<IOffenceService, OffenceService>();
            builder.Services.AddTransient<IOpenIddictService, OpenIddictService>();

            builder.Services.AddTransient<ICharacterStore, CharacterStore>();
            builder.Services.AddTransient<IUserStore<Character>>(provider => provider.GetRequiredService<ICharacterStore>());
            builder.Services.AddScoped<CharacterSignInManager>();
            builder.Services.AddScoped<SignInManager<Character>>(provider => provider.GetRequiredService<CharacterSignInManager>());
            builder.Services.AddScoped<CharacterManager>();
            builder.Services.AddScoped<UserManager<Character>>(provider => provider.GetRequiredService<CharacterManager>());

            // Configure Identity to use the same JWT claims as OpenIddict instead
            // of the legacy WS-Federation claims it uses by default (ClaimTypes),
            // which saves you from doing the mapping in your authorization controller.
            builder.Services.Configure<IdentityOptions>(options =>
            {
                options.ClaimsIdentity.UserNameClaimType = Claims.Name;
                options.ClaimsIdentity.UserIdClaimType = Claims.Subject;
                options.ClaimsIdentity.RoleClaimType = Claims.Role;
                options.ClaimsIdentity.EmailClaimType = Claims.Email;
            });

            // OpenIddict offers native integration with Quartz.NET to perform scheduled tasks
            // (like pruning orphaned authorizations/tokens from the database) at regular intervals.
            builder.Services.AddQuartz(options =>
            {
                options.UseSimpleTypeLoader();
                options.UseInMemoryStore();
            });

            // Register the Quartz.NET service and configure it to block shutdown until jobs are complete.
            builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

            builder.Services.AddOpenIddict()
                // Register the OpenIddict core components.
                .AddHagalazOpenIddictCore(options =>
                {
                    // Enable Quartz.NET integration.
                    options.UseQuartz();
                })
                .AddServer(options =>
                {
                    // Enable all the endpoints.
                    options.SetTokenEndpointUris("connect/token")
                        .SetEndSessionEndpointUris("connect/logout")
                        .SetIntrospectionEndpointUris("connect/introspect")
                        .SetUserInfoEndpointUris("connect/userinfo")
                        .SetRevocationEndpointUris("connect/revoke");

                    options.RegisterScopes(Scopes.Email, Scopes.Profile, Scopes.Roles, Hagalaz.Authorization.Constants.Scopes.CharactersApi);
                    options.RegisterClaims(Claims.Username, Claims.PreferredUsername, Claims.Email, Claims.EmailVerified, Claims.Role);

                    // Enable the password and refresh token  flows.
                    options.AllowPasswordFlow().AllowRefreshTokenFlow().RequireProofKeyForCodeExchange();

                    // Register the signing credentials.
                    options.AddDevelopmentSigningCertificate().AddDevelopmentEncryptionCertificate();

                    // Register the ASP.NET Core host and configure the ASP.NET Core-specific options.
                    options.UseAspNetCore()
                        .EnableEndSessionEndpointPassthrough()
                        .EnableTokenEndpointPassthrough()
                        .EnableUserInfoEndpointPassthrough()
                        .DisableTransportSecurityRequirement();

                    builder.Services.AddSingleton<OpenIddictServerPocoEvents.RequirePocoRequest>();
                    foreach (var descriptor in OpenIddictServerPocoHandlers.DefaultHandlers)
                    {
                        options.AddEventHandler(descriptor);
                    }
                })

                // Register the OpenIddict validation components.
                .AddValidation(options =>
                {
                    // Import the configuration from the local OpenIddict server instance.
                    options.UseLocalServer();

                    // Register the ASP.NET Core host.
                    options.UseAspNetCore();

                    // Check every token for validity
                    options.EnableTokenEntryValidation();
                });

            builder.Services.AddMassTransit(x =>
            {
                x.UsingRabbitMq((context, cfg) =>
                {
                    var host = builder.Configuration.GetConnectionString("messaging");
                    if (host == null)
                    {
                        throw new ArgumentNullException(nameof(host));
                    }

                    cfg.Host(host);
                    cfg.ConfigureEndpoints(context);
                });
                x.AddConsumer<GetUserInfoRequestConsumer>();
                x.AddConsumer<SignInUserRequestConsumer>();
                x.AddConsumer<RevokeTokenRequestConsumer>();
                x.AddConsumer<GetTokensRequestConsumer>();
            });
            builder.Services.AddMediator(x =>
            {
                x.AddConsumer<GetUserInfoCommandConsumer>();
                x.AddConsumer<PasswordGrantCommandConsumer>();
                x.AddConsumer<RefreshTokenGrantCommandConsumer>();
                x.AddConsumer<GetTokensRequestConsumer>();
            });


            var app = builder.Build();

            app.UseServiceDefaults();

            await app.MigrateDatabase<HagalazDbContext>();

            await app.RunAsync();
        }
    }
}