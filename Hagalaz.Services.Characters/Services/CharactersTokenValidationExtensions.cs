using System;
using Hagalaz.ServiceDefaults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.Characters.Services
{
    public static class CharactersTokenValidationExtensions
    {
        /// <summary>
        ///     Validates bearer tokens by introspecting them at the Authorization service as the
        ///     <c>characters-service-1</c> client. Fails fast when the client secret is not configured.
        /// </summary>
        public static IServiceCollection AddCharactersTokenValidation(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOpenIddict()
                .AddValidation(options =>
                {
                    var authServiceUri = configuration.GetServiceConfigurationValue("hagalaz-services-authorization", "https", "http");
                    if (string.IsNullOrEmpty(authServiceUri))
                    {
                        Console.Error.WriteLine("The authorization service URI is not configured.");
                        return;
                    }

                    // Note: the validation handler uses OpenID Connect discovery
                    // to retrieve the address of the introspection endpoint.
                    options.SetIssuer(authServiceUri);
                    options.AddAudiences("characters-service-1", "hagalaz-web-app");

                    // Configure the validation handler to use introspection and register the client
                    // credentials used when communicating with the remote introspection endpoint.
                    var clientSecret = configuration.GetValue<string>("Security:Introspection:ClientSecret");
                    if (string.IsNullOrWhiteSpace(clientSecret))
                    {
                        throw new InvalidOperationException("The Security:Introspection:ClientSecret setting must be configured for the Characters API introspection client.");
                    }

                    options.UseIntrospection().SetClientId("characters-service-1").SetClientSecret(clientSecret);

                    // Register the System.Net.Http integration.
                    options.UseSystemNetHttp();

                    // Register the ASP.NET Core host.
                    options.UseAspNetCore();
                });

            return services;
        }
    }
}
