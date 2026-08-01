using Hagalaz.Data;
using Hagalaz.Data.Extensions;
using Hagalaz.Database.Migrations;
using Hagalaz.ServiceDefaults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);
builder.AddHagalazDbContextPool("hagalaz-db");

using var host = builder.Build();
var logger = host.Services.GetRequiredService<ILogger<MigrationApplication>>();
var migrationApplication = new MigrationApplication(
    cancellationToken => host.MigrateDatabase<HagalazDbContext>(cancellationToken: cancellationToken),
    logger);

return await migrationApplication.RunAsync();
