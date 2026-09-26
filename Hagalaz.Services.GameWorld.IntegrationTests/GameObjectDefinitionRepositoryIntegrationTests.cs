using System.Data.Common;
using Hagalaz.Data;
using Hagalaz.Services.GameWorld.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MySql.EntityFrameworkCore.Extensions;
using Testcontainers.MySql;

namespace Hagalaz.Services.GameWorld.IntegrationTests;

[TestClass]
[DoNotParallelize]
public sealed class GameObjectDefinitionRepositoryIntegrationTests
{
    private static MySqlContainer? _database;

    [ClassInitialize]
    public static async Task InitializeAsync(TestContext _)
    {
        _database = new MySqlBuilder("mysql:8.4")
            .WithDatabase("hagalaz-gameworld-object-definition-test")
            .WithUsername("root")
            .WithPassword("hagalaz-gameworld-object-definition-test")
            .WithCommand("--character-set-server=utf8mb4", "--collation-server=utf8mb4_0900_ai_ci")
            .Build();
        await _database.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await context.Database.ExecuteSqlRawAsync("""
            INSERT INTO gameobject_definitions (gameobject_id, name, examine, gameobject_loot_id)
            VALUES (880001, 'probe one', 'first override', NULL),
                   (880002, 'probe two', 'second override', NULL),
                   (880003, 'unrequested', 'unrequested override', NULL)
            """);
    }

    [ClassCleanup]
    public static async Task CleanupAsync()
    {
        if (_database != null)
        {
            await _database.DisposeAsync();
        }
    }

    [TestMethod]
    [Timeout(120000)]
    public async Task FindOverridesByIdsAsync_ReturnsOnlyRequestedRowsInOneSqlStatement()
    {
        var counter = new DefinitionQueryCounter();
        await using var context = CreateContext(counter);
        var repository = new GameObjectDefinitionRepository(context);

        var definitions = await repository.FindOverridesByIdsAsync([880001, 880002, 990000, 880001]);

        Assert.AreEqual(1, counter.Statements.Count);
        Assert.IsTrue(counter.Statements[0].Contains("gameobject_definitions", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(counter.Statements[0].Contains("name", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(counter.Statements[0].Contains(" IN ", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(counter.Statements[0].Contains("WHERE", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual(2, definitions.Count);
        Assert.AreEqual("first override", definitions[880001].Examine);
        Assert.IsNull(definitions[880001].LootTableId);
        Assert.AreEqual("second override", definitions[880002].Examine);
        Assert.AreEqual(definitions.Count, definitions.Keys.Distinct().Count());
        Assert.IsFalse(definitions.ContainsKey(990000));
        Assert.IsFalse(definitions.ContainsKey(880003));
    }

    [TestMethod]
    public async Task FindOverridesByIdsAsync_WithEmptyIdsDoesNotExecuteSql()
    {
        var counter = new DefinitionQueryCounter();
        await using var context = CreateContext(counter);
        var repository = new GameObjectDefinitionRepository(context);

        var definitions = await repository.FindOverridesByIdsAsync([]);

        Assert.AreEqual(0, definitions.Count);
        Assert.AreEqual(0, counter.Statements.Count);
    }

    [TestMethod]
    public async Task FindOverridesByIdsAsync_PropagatesCancellationBeforeExecutingSql()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var counter = new DefinitionQueryCounter();
        await using var context = CreateContext(counter);
        var repository = new GameObjectDefinitionRepository(context);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            repository.FindOverridesByIdsAsync([880001], cancellation.Token));

        Assert.AreEqual(0, counter.Statements.Count);
    }

    private static HagalazDbContext CreateContext(DbCommandInterceptor? interceptor = null)
    {
        var options = new DbContextOptionsBuilder<HagalazDbContext>()
            .UseMySQL(_database!.GetConnectionString());
        if (interceptor != null)
        {
            options.AddInterceptors(interceptor);
        }
        return new HagalazDbContext(options.Options);
    }

    private sealed class DefinitionQueryCounter : DbCommandInterceptor
    {
        public List<string> Statements { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Statements.Add(command.CommandText);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
