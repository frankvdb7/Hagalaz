using Hagalaz.Data;
using Hagalaz.Data.Extensions;
using Hagalaz.Database.Migrations;
using Hagalaz.Database.Migrations.Seeding;
using Hagalaz.ServiceDefaults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);
builder.AddHagalazDbContextPool("hagalaz-db");
builder.Services.AddGameWorldSeeding();

using var host = builder.Build();
var logger = host.Services.GetRequiredService<ILogger<MigrationApplication>>();
var migrationApplication = new MigrationApplication(
    cancellationToken => host.MigrateDatabase<HagalazDbContext>(
        // Runs under the migration lock, after the schema is current. Seeders only add what is absent.
        token => host.Services.SeedGameWorldAsync(token),
        cancellationToken),
    logger);

return await migrationApplication.RunAsync();
