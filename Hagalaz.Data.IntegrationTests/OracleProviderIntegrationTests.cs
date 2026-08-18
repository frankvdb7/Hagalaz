using Hagalaz.Data.Extensions;
using Hagalaz.ServiceDefaults;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        await using var app = CreateApplication();
        await app.MigrateDatabase<HagalazDbContext>();

        await using var context = CreateContext();

        Assert.IsFalse((await context.Database.GetPendingMigrationsAsync()).Any());
        Assert.IsTrue(await context.Database.CanConnectAsync());

        await context.Database.OpenConnectionAsync();

        foreach (var tableName in new[]
                 {
                     "minigames_barrows",
                     "minigames_duel_arena",
                     "minigames_godwars",
                     "minigames_tzhaar_cave",
                     "characters_permissions",
                     "characters_preferences"
                 })
        {
            Assert.IsFalse(
                await TableExistsAsync(context, tableName),
                $"The legacy table '{tableName}' should have been removed.");
        }

        Assert.IsTrue(
            await TableExistsAsync(context, "minigames_tzhaar_cave_waves"),
            "The TzHaar wave-definition table must remain.");
    }

    [TestMethod]
    [Timeout(120_000)]
    public async Task Migrations_AreIdempotent_WhenMultipleServicesStart()
    {
        await using var first = CreateApplication();
        await using var second = CreateApplication();

        await Task.WhenAll(
            first.MigrateDatabase<HagalazDbContext>(),
            second.MigrateDatabase<HagalazDbContext>());

        await using var context = CreateContext();
        var applied = await context.Database.GetAppliedMigrationsAsync();
        await context.Database.OpenConnectionAsync();
        Assert.HasCount(9, applied);
        Assert.Contains("20240316233038_InitialCreate", applied);
        Assert.Contains("20250721222703_UpdateOpenIddict7", applied);
        Assert.Contains("20251119194916_UpdateStateId", applied);
        Assert.Contains("20260730191421_AddCharacterSnapshotRevision", applied);
        Assert.Contains("20260730225943_AddMassTransitCharacterPersistenceOutbox", applied);
        Assert.Contains("20260810072238_AddCharacterSnapshotFingerprint", applied);
        Assert.Contains("20260810120113_RemoveLegacyMinigamePersistence", applied);
        Assert.Contains("20260818183057_RemoveObsoleteCharacterPermissions", applied);
        Assert.Contains("20260818192702_RemoveObsoleteCharacterPreferences", applied);
        Assert.IsTrue(await TableExistsAsync(context, "aspnetroles"));
        Assert.IsTrue(await TableExistsAsync(context, "aspnetuserroles"));
    }

    [TestMethod]
    [Timeout(120_000)]
    public async Task LegacySslModeNone_IsTranslatedForOracleConnectorNet()
    {
        var database = _database ?? throw new InvalidOperationException("The MySQL test container was not initialized.");
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:hagalaz-db"] = $"{database.GetConnectionString()};SSL Mode=None"
        });
        builder.AddHagalazDbContextPool("hagalaz-db");

        using var host = builder.Build();
        await using var scope = host.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<HagalazDbContext>();

        Assert.IsTrue(await context.Database.CanConnectAsync());
    }

    private static WebApplication CreateApplication()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(OracleProviderIntegrationTests).Assembly.GetName().Name,
            EnvironmentName = Environments.Development
        });
        builder.Services.AddScoped(_ => CreateContext());
        return builder.Build();
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

    private static async Task<bool> TableExistsAsync(HagalazDbContext context, string tableName)
    {
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.tables
            WHERE table_schema = DATABASE()
              AND table_name = @tableName;
            """;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@tableName";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);

        return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
    }
}
