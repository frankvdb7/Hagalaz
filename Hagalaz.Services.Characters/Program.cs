using System;
using System.Threading.Tasks;
using AutoMapper;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Validation.AspNetCore;
using Hagalaz.Data;
using Hagalaz.Data.Extensions;
using Hagalaz.Services.Characters.Consumers;
using Hagalaz.Services.Characters.Data;
using Hagalaz.Services.Characters.Mediator.Consumers;
using Hagalaz.Services.Characters.Services;
using Hagalaz.ServiceDefaults;

namespace Hagalaz.Services.Characters
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // aspire
            builder.AddServiceDefaults();
            builder.AddHagalazDbContextPool("hagalaz-db");

            builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);

            builder.Services.AddOpenIddict()
                .AddValidation(options =>
                {                    
                    var authServiceUri = builder.Configuration.GetServiceConfigurationValue("hagalaz-services-authorization", "https", "http");
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
                    var clientSecret = builder.Configuration.GetValue<string>("Security:Introspection:ClientSecret") ?? string.Empty;
                    options.UseIntrospection().SetClientId("characters-service-1").SetClientSecret(clientSecret);

                    // Register the System.Net.Http integration.
                    options.UseSystemNetHttp();

                    // Register the ASP.NET Core host.
                    options.UseAspNetCore();
                });

            // unit of work
            builder.Services.AddScoped<ICharacterUnitOfWork, CharacterUnitOfWork>();
            builder.Services.AddScoped<ICharacterService, CharacterService>();

            builder.Services.AddMassTransit(x =>
            {
                x.UsingRabbitMq((context, cfg) =>
                {
                    var host = builder.Configuration.GetConnectionString("messaging");
                    if (host == null)
                    {
                        throw new ArgumentNullException(nameof(host));
                    }
                    cfg.Host(host.ToString());
                    cfg.ConfigureEndpoints(context);
                });

                x.AddConsumer<GetCharacterRequestConsumer>();
                x.AddConsumer<UpdateCharacterRequestConsumer>();
            });

            builder.Services.AddMediator(c =>
            {
                c.AddConsumer<GetCharacterStatisticsQueryConsumer>();
                c.AddConsumer<GetAllCharacterStatisticsQueryConsumer>();
            });

            builder.Services.AddAutoMapper(typeof(Program));

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                var mapper = app.Services.GetRequiredService<IMapper>();
                mapper.ConfigurationProvider.AssertConfigurationIsValid();
            }

            app.UseServiceDefaults();

            await app.MigrateDatabase<HagalazDbContext>();

            await app.RunAsync();
        }
    }
}
