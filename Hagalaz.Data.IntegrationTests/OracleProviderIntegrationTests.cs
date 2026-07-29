using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Testcontainers.MySql;

namespace Hagalaz.Data.IntegrationTests;

[TestClass]
[DoNotParallelize]
public sealed class OracleProviderIntegrationTests
{
    private static MySqlContainer? _database;

    [ClassInitialize]
    public static async Task InitializeAsync(TestContext _)
    {
        _database = new MySqlBuilder("mysql:8.4")
            .WithDatabase("hagalaz-db")
            .WithUsername("root")
            .WithPassword("hagalaz-integration-test")
            .WithCommand(
                "--character-set-server=utf8mb4",
                "--collation-server=utf8mb4_0900_ai_ci")
            .Build();

        await _database.StartAsync();
    }

    [ClassCleanup]
    public static async Task CleanupAsync()
    {
        if (_database is not null)
        {
            await _database.DisposeAsync();
        }
    }

    [TestMethod]
    [Timeout(120_000)]
    public async Task Migrations_ApplyToEmptyMySqlDatabase_WithoutPendingChanges()
    {
        await using var context = CreateContext();

        await context.Database.MigrateAsync();

        Assert.IsFalse((await context.Database.GetPendingMigrationsAsync()).Any());
        Assert.IsTrue(await context.Database.CanConnectAsync());
    }

    [TestMethod]
    [Timeout(120_000)]
    public async Task Migrations_AreIdempotent_WhenMultipleServicesStart()
    {
        await using var first = CreateContext();
        await using var second = CreateContext();

        await Task.WhenAll(first.Database.MigrateAsync(), second.Database.MigrateAsync());

        var applied = await first.Database.GetAppliedMigrationsAsync();
        Assert.HasCount(3, applied);
        Assert.Contains("20240316233038_InitialCreate", applied);
        Assert.Contains("20250721222703_UpdateOpenIddict7", applied);
        Assert.Contains("20251119194916_UpdateStateId", applied);
    }

    private static HagalazDbContext CreateContext()
    {
        var database = _database ?? throw new InvalidOperationException("The MySQL test container was not initialized.");
        var options = new DbContextOptionsBuilder<HagalazDbContext>()
            .UseMySQL(database.GetConnectionString(), mysql => mysql.EnableRetryOnFailure(6))
            .UseOpenIddict()
            .Options;

        return new HagalazDbContext(options);
    }
}
