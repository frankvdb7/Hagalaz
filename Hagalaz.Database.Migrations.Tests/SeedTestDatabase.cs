using Hagalaz.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MySql;

namespace Hagalaz.Database.Migrations.Tests;

/// <summary>
///     Starts the throwaway MySQL container the seeder tests run against and opens contexts on it.
/// </summary>
internal static class SeedTestDatabase
{
    public static async Task<MySqlContainer> StartAsync(string name)
    {
        var database = new MySqlBuilder("mysql:8.4")
            .WithDatabase(name)
            .WithUsername("root")
            .WithPassword(name)
            .WithCommand("--character-set-server=utf8mb4", "--collation-server=utf8mb4_0900_ai_ci")
            .Build();
        await database.StartAsync();
        return database;
    }

    public static async Task StopAsync(MySqlContainer? database)
    {
        if (database != null)
        {
            await database.DisposeAsync();
        }
    }

    public static HagalazDbContext CreateContext(MySqlContainer database) =>
        new(new DbContextOptionsBuilder<HagalazDbContext>().UseMySQL(database.GetConnectionString()).Options);
}
