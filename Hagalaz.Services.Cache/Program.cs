using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Validation.AspNetCore;
using Hagalaz.Cache;
using Hagalaz.Cache.Extensions;
using Hagalaz.ServiceDefaults;
using Hagalaz.Services.Cache.Features;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.Configure<CacheOptions>(builder.Configuration.GetSection(CacheOptions.Key));
builder.Services.AddGameCache();

builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
builder.Services.AddOpenIddict()
    .AddValidation(options =>
    {
        var authServiceUri = builder.Configuration.GetServiceConfigurationValue("hagalaz-services-authorization", "https", "http");
        if (string.IsNullOrWhiteSpace(authServiceUri))
        {
            Console.Error.WriteLine("The authorization service URI is not configured.");
            return;
        }

        options.SetIssuer(authServiceUri);
        options.AddAudiences("cache-service-1", "hagalaz-web-app");

        var clientSecret = builder.Configuration.GetValue<string>("Security:Introspection:ClientSecret") ?? string.Empty;
        options.UseIntrospection().SetClientId("cache-service-1").SetClientSecret(clientSecret);
        options.UseSystemNetHttp();
        options.UseAspNetCore();
    });

var app = builder.Build();

app.UseServiceDefaults();
app.MapCacheEndpoints();

await app.RunAsync();
