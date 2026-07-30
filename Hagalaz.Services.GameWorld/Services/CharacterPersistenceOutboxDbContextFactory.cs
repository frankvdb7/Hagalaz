using System;
using System.Linq;
using Hagalaz.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Hagalaz.Services.GameWorld.Services;

public sealed class CharacterPersistenceOutboxDbContextFactory
    : IDesignTimeDbContextFactory<CharacterPersistenceOutboxDbContext>
{
    public CharacterPersistenceOutboxDbContext CreateDbContext(string[] args)
    {
        var connectionString = args
            .FirstOrDefault(static argument => argument.StartsWith("--connection=", StringComparison.OrdinalIgnoreCase))?
            .Split('=', 2)[1]
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__hagalaz-db")
            ?? "Server=localhost;Database=hagalaz;User=root;Password=;";

        connectionString = MySqlConnectionStringCompatibility.NormalizeForOracle(connectionString, "design-time");

        var options = new DbContextOptionsBuilder<CharacterPersistenceOutboxDbContext>()
            .UseMySQL(connectionString, mysqlOptions =>
            {
                mysqlOptions.EnableRetryOnFailure(6);
                mysqlOptions.MigrationsHistoryTable("__MassTransitOutboxMigrationsHistory");
            })
            .Options;
        return new CharacterPersistenceOutboxDbContext(options);
    }
}
