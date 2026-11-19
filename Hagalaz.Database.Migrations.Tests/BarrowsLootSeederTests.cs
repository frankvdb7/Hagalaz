using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MySql.EntityFrameworkCore.Extensions;
using Testcontainers.MySql;

namespace Hagalaz.Database.Migrations.Tests;

[TestClass]
[DoNotParallelize]
public sealed class BarrowsLootSeederTests
{
    private static MySqlContainer? _database;

    [ClassInitialize]
    public static async Task InitializeAsync(TestContext _)
    {
        _database = new MySqlBuilder("mysql:8.4")
            .WithDatabase("hagalaz-barrows-seed-test")
            .WithUsername("root")
            .WithPassword("hagalaz-barrows-seed-test")
            .WithCommand("--character-set-server=utf8mb4", "--collation-server=utf8mb4_0900_ai_ci")
            .Build();
        await _database.StartAsync();
    }

    [ClassCleanup]
    public static async Task CleanupAsync()
    {
        if (_database != null)
        {
            await _database.DisposeAsync();
        }
    }

    [TestInitialize]
    public async Task ResetAsync()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await context.GameobjectLootItems.Where(item => item.GameobjectLootId >= 36).ExecuteDeleteAsync();
        await context.GameobjectLoots.Where(loot => loot.Id >= 36).ExecuteDeleteAsync();
    }

    [TestMethod]
    [Timeout(180000)]
    public async Task Seed_OnAnEmptyTargetCreatesEveryTableWithItsItems()
    {
        await SeedAsync();

        await using var context = CreateContext();
        var tables = await context.GameobjectLoots.Include(loot => loot.GameobjectLootItems).Where(loot => loot.Id >= 36).ToListAsync();
        Assert.AreEqual(BarrowsLootSeeder.Tables.Count, tables.Count);
        foreach (var expected in BarrowsLootSeeder.Tables)
        {
            var actual = tables.Single(table => table.Id == expected.Id);
            Assert.AreEqual(expected.Name, actual.Name);
            Assert.AreEqual(1u, actual.MaximumLootCount);
            CollectionAssert.AreEquivalent(expected.Items.Select(item => (int)item.ItemId).ToArray(),
                actual.GameobjectLootItems.Select(item => (int)item.ItemId).ToArray());
        }

        Assert.IsFalse(tables.Any(table => table.Name.Contains("rare", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    [Timeout(180000)]
    public async Task Seed_RunTwiceChangesNothing()
    {
        await SeedAsync();
        await SeedAsync();

        await using var context = CreateContext();
        Assert.AreEqual(BarrowsLootSeeder.Tables.Count, await context.GameobjectLoots.CountAsync(loot => loot.Id >= 36));
        Assert.AreEqual(BarrowsLootSeeder.Tables.Sum(table => table.Items.Count), await context.GameobjectLootItems.CountAsync(item => item.GameobjectLootId >= 36));
    }

    [TestMethod]
    [Timeout(180000)]
    public async Task Seed_NeverOverwritesATableThatAlreadyExists()
    {
        await using (var context = CreateContext())
        {
            context.GameobjectLoots.Add(new GameobjectLoot
            {
                Id = 36,
                Name = "barrows_set_ahrim",
                MaximumLootCount = 3,
                RandomizeLootCount = 1,
                GameobjectLootItems = [new GameobjectLootItem { ItemId = 4708, MinimumCount = 5, MaximumCount = 9, Probability = 1m }]
            });
            await context.SaveChangesAsync();
        }

        await SeedAsync();

        await using var verify = CreateContext();
        var edited = await verify.GameobjectLoots.Include(loot => loot.GameobjectLootItems).SingleAsync(loot => loot.Id == 36);
        Assert.AreEqual(3u, edited.MaximumLootCount);
        Assert.AreEqual(1, edited.GameobjectLootItems.Count);
        Assert.AreEqual(5u, edited.GameobjectLootItems.Single().MinimumCount);
        Assert.AreEqual(BarrowsLootSeeder.Tables.Count, await verify.GameobjectLoots.CountAsync(loot => loot.Id >= 36));
    }

    [TestMethod]
    [Timeout(180000)]
    public async Task Seed_LeavesATableAloneWhenItsIdBelongsToSomethingElse()
    {
        await using (var context = CreateContext())
        {
            context.GameobjectLoots.Add(new GameobjectLoot { Id = 42, Name = "someone_elses_table", MaximumLootCount = 2, RandomizeLootCount = 0 });
            await context.SaveChangesAsync();
        }

        await SeedAsync();

        await using var verify = CreateContext();
        var other = await verify.GameobjectLoots.Include(loot => loot.GameobjectLootItems).SingleAsync(loot => loot.Id == 42);
        Assert.AreEqual("someone_elses_table", other.Name);
        Assert.AreEqual(0, other.GameobjectLootItems.Count);
    }

    [TestMethod]
    [Timeout(180000)]
    public async Task Seed_DoesNotTouchOtherLootTables()
    {
        await using (var context = CreateContext())
        {
            context.GameobjectLoots.Add(new GameobjectLoot { Id = 35, Name = "unrelated", MaximumLootCount = 1, RandomizeLootCount = 0 });
            await context.SaveChangesAsync();
        }

        await SeedAsync();

        await using var verify = CreateContext();
        Assert.IsTrue(await verify.GameobjectLoots.AnyAsync(loot => loot.Id == 35 && loot.Name == "unrelated"));
        await verify.GameobjectLoots.Where(loot => loot.Id == 35).ExecuteDeleteAsync();
    }

    private static async Task SeedAsync()
    {
        await using var context = CreateContext();
        await new BarrowsLootSeeder(context, NullLogger<BarrowsLootSeeder>.Instance).SeedAsync();
    }

    private static HagalazDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<HagalazDbContext>().UseMySQL(_database!.GetConnectionString()).Options);
}
