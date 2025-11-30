using System;
using System.Text.Json.Serialization;
using AutoMapper;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Hagalaz.Data;
using Hagalaz.Data.Extensions;
using Hagalaz.Services.GameLogon.Config;
using Hagalaz.Services.GameLogon.Consumers;
using Hagalaz.Services.GameLogon.Data;
using Hagalaz.Services.GameLogon.Services;
using Hagalaz.Services.GameLogon.Store;

namespace Hagalaz.Services.GameLogon
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHealthChecks();
            
            // configure
            services.Configure<FriendsChatOptions>(Configuration.GetSection(FriendsChatOptions.FriendsChat));
            services.Configure<KestrelServerOptions>(Configuration.GetSection("Kestrel"));

            // unit of work
            services.AddScoped<ICharacterUnitOfWork, CharacterUnitOfWork>();
            
            // stores
            services.AddSingleton<FriendsChatStore>();

            // services
            services.AddScoped<ICharacterService, CharacterService>();
            services.AddScoped<IClanService, ClanService>();
            services.AddScoped<IClanChatService, ClanChatService>();
            services.AddScoped<IFriendsChatService, FriendsChatService>();
            services.AddScoped<IContactService, ContactService>();

            var connectionString = "";
            
            services.AddAutoMapper(_ => { },typeof(Startup));

            // rabbitmq
            services.AddAuthorization();
            services.AddMassTransit(x =>
            {
                x.AddConsumer<FriendsChatConsumer>();
                x.UsingRabbitMq((context, cfg) =>
                {
                    var host = Configuration.GetConnectionString("rabbitmq");
                    if (host == null)
                    {
                        throw new ArgumentNullException(nameof(host));
                    }
                    cfg.Host(host.ToString());
                });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IMapper mapper)
        {
            app.UseAuthentication();
            app.UseAuthorization();
            if (env.IsDevelopment())
            {
                mapper.ConfigurationProvider.AssertConfigurationIsValid();
            }
        }
    }
}