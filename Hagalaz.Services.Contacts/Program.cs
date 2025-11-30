using AutoMapper;
using MassTransit;
using Hagalaz.Data;
using Hagalaz.Data.Extensions;
using Hagalaz.Services.Contacts.Consumers;
using Hagalaz.Services.Contacts.Data;
using Hagalaz.Services.Contacts.Services;
using Hagalaz.Services.Contacts.Store;
using Hagalaz.ServiceDefaults;

namespace Hagalaz.Services.Contacts
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // aspire
            builder.AddServiceDefaults();
            builder.AddHagalazDbContextPool("hagalaz-db");

            builder.Services.AddMassTransit(x =>
            {
                x.UsingRabbitMq((context, cfg) => 
                {
                    var host = builder.Configuration.GetConnectionString("messaging");
                    if (string.IsNullOrEmpty(host))
                    {
                        throw new ArgumentNullException(nameof(host));
                    }
                    cfg.Host(host);

                    cfg.ReceiveEndpoint(typeof(WorldStatusConsumer).FullName!, e =>
                        {
                            e.ConfigureConsumer<WorldStatusConsumer>(context);
                        });
                    
                    cfg.ConfigureEndpoints(context);
                });

                x.AddConsumer<WorldUserSignInOutConsumer>();
                x.AddConsumer<LobbyUserSignInOutConsumer>();
                x.AddConsumer<GetContactsConsumer>();
                x.AddConsumer<AddRemoveContactConsumer>();
                x.AddConsumer<SetContactSettingsConsumer>();
                x.AddConsumer<WorldStatusConsumer>();
                x.AddConsumer<AddContactMessageConsumer>();
            });
            builder.Services.AddHostedService<WorldStatusService>();
            builder.Services.AddSingleton<ContactSessionStore>();
            builder.Services.AddSingleton<WorldSessionStore>();
            builder.Services.AddScoped<ICharacterUnitOfWork, CharacterUnitOfWork>();
            builder.Services.AddScoped<IContactService, ContactService>();
            builder.Services.AddScoped<ICharacterService, CharacterService>();
            builder.Services.AddScoped<IContactSessionService, ContactSessionService>();

            builder.Services.AddAutoMapper(_ => { }, typeof(Program));

            var app = builder.Build();

            // aspire
            app.UseServiceDefaults();

            if (app.Environment.IsDevelopment())
            {
                var mapper = app.Services.GetRequiredService<IMapper>();
                mapper.ConfigurationProvider.AssertConfigurationIsValid();
            }

            await app.MigrateDatabase<HagalazDbContext>();

            await app.RunAsync();
        }
    }
}