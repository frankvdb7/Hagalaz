using Hagalaz.Data;
using Hagalaz.Database.Migrations.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.MySql;

namespace Hagalaz.Database.Migrations.Tests;

[TestClass]
[DoNotParallelize]
public sealed class FarmingSeedSeederTests
{
    private static MySqlContainer? _database;

    [ClassInitialize]
    public static async Task InitializeAsync(TestContext _) => _database = await SeedTestDatabase.StartAsync("hagalaz-farming-seed-test");

    [ClassCleanup]
    public static Task CleanupAsync() => SeedTestDatabase.StopAsync(_database);

    [TestInitialize]
    public async Task ResetAsync()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await context.SkillsFarmingSeedDefinitions.Where(seed => seed.Type == "FruitTree" || seed.Type == "Bush").ExecuteDeleteAsync();
        await context.SkillsFarmingPatchDefinitions.Where(patch => patch.ObjectId >= 7577 && patch.ObjectId <= 7580).ExecuteDeleteAsync();
    }

    [TestMethod]
    [Timeout(180000)]
    public async Task Seed_OnAnEmptyTargetCreatesEveryFruitTreeWithItsProductExperience()
    {
        await SeedAsync();
        await SeedAsync();

        await using var context = CreateContext();
        var seeds = await context.SkillsFarmingSeedDefinitions.Where(seed => seed.Type == "FruitTree").OrderBy(seed => seed.RequiredLevel).ToListAsync();
        Assert.HasCount(7, seeds);
        Assert.IsTrue(seeds.All(seed => seed.ProductExperience > 0 && seed.MaximumProductCount == 6 && seed.ProductRegrowTicks > 0));
        var apple = seeds.Single(seed => seed.ItemId == 5283);
        Assert.AreEqual(27, apple.RequiredLevel);
        Assert.AreEqual(1199.5m, apple.HarvestExperience);
        Assert.AreEqual(8.5m, apple.ProductExperience);
    }

    [TestMethod]
    [Timeout(180000)]
    public async Task Seed_CreatesEveryBushWithItsPatchObjects()
    {
        await using (var context = CreateContext())
        {
            await new FarmingSeedSeeder(context, [new BushSeedSource()], NullLogger<FarmingSeedSeeder>.Instance).SeedAsync();
            await new FarmingSeedSeeder(context, [new BushSeedSource()], NullLogger<FarmingSeedSeeder>.Instance).SeedAsync();
        }

        await using var verify = CreateContext();
        var seeds = await verify.SkillsFarmingSeedDefinitions.Where(seed => seed.Type == "Bush").ToListAsync();
        Assert.HasCount(6, seeds);
        Assert.IsTrue(seeds.All(seed => seed.ProductExperience > 0 && seed.MaximumProductCount == 4));
        Assert.AreEqual(4.5m, seeds.Single(seed => seed.ItemId == 5101).ProductExperience);
        Assert.AreEqual(5, seeds.Single(seed => seed.ItemId == 5101).MaxCycles);
        var patches = await verify.SkillsFarmingPatchDefinitions.Where(patch => patch.ObjectId >= 7577 && patch.ObjectId <= 7580).ToListAsync();
        Assert.HasCount(4, patches);
        Assert.IsTrue(patches.All(patch => patch.Type == "Bush"));
    }

    [TestMethod]
    [Timeout(180000)]
    public async Task Seed_NeverChangesAnExistingDefinition()
    {
        await using (var context = CreateContext())
        {
            await new FarmingSeedSeeder(context, [new FruitTreeSeedSource()], NullLogger<FarmingSeedSeeder>.Instance).SeedAsync();
            await context.SkillsFarmingSeedDefinitions.Where(seed => seed.ItemId == 5283).ExecuteUpdateAsync(s => s.SetProperty(seed => seed.ProductExperience, 99m));
        }

        await SeedAsync();

        await using var verify = CreateContext();
        Assert.AreEqual(99m, (await verify.SkillsFarmingSeedDefinitions.SingleAsync(seed => seed.ItemId == 5283)).ProductExperience);
    }

    [TestMethod]
    [Timeout(240000)]
    public async Task Upgrade_LegacyDefinitionsWithoutProductExperience_AreBackfilledAndCustomValuesKept()
    {
        const string beforeProductExperience = "20260818192702_RemoveObsoleteCharacterPreferences";
        await using (var legacy = CreateContext())
        {
            // A database from before the product_experience column existed, holding hand-made fruit tree and bush rows.
            await legacy.GetService<IMigrator>().MigrateAsync(beforeProductExperience);
            await legacy.Database.ExecuteSqlRawAsync(
                "INSERT INTO skills_farming_seed_definitions (item_id, product_id, minimum_product_count, maximum_product_count, required_level, planting_experience, harvest_experience, varpbit_index, max_cycles, cycle_ticks, type) VALUES " +
                "(5283, 1955, 6, 6, 27, 1, 2, 0, 6, 16000, 'FruitTree'), (5284, 1963, 6, 6, 33, 1, 2, 1, 6, 16000, 'FruitTree'), (5101, 1951, 4, 4, 10, 1, 2, 0, 5, 2000, 'Bush')");
        }

        await using (var upgraded = CreateContext())
        {
            await upgraded.Database.MigrateAsync();
            await upgraded.SkillsFarmingSeedDefinitions.Where(seed => seed.ItemId == 5101).ExecuteUpdateAsync(s => s.SetProperty(seed => seed.ProductExperience, 99m));
            await upgraded.SkillsFarmingSeedDefinitions.Where(seed => seed.ItemId == 5283).ExecuteUpdateAsync(s => s.SetProperty(seed => seed.ProductExperience, 0m));
            await upgraded.SkillsFarmingSeedDefinitions.Where(seed => seed.ItemId == 5284).ExecuteUpdateAsync(s => s.SetProperty(seed => seed.ProductRegrowTicks, 1234u));
        }

        await SeedAsync();
        await SeedAsync();

        await using var verify = CreateContext();
        var apple = await verify.SkillsFarmingSeedDefinitions.AsNoTracking().SingleAsync(seed => seed.ItemId == 5283);
        Assert.AreEqual(8.5m, apple.ProductExperience);
        Assert.AreEqual(1m, apple.PlantingExperience);
        Assert.AreEqual(2m, apple.HarvestExperience);
        Assert.AreEqual(99m, (await verify.SkillsFarmingSeedDefinitions.AsNoTracking().SingleAsync(seed => seed.ItemId == 5101)).ProductExperience);

        // A legacy fruit tree regrows at the seeded per-fruit rate, not once per 16000 tick growth cycle; a custom interval is kept.
        Assert.AreEqual(6000u, apple.ProductRegrowTicks);
        Assert.AreEqual(1234u, (await verify.SkillsFarmingSeedDefinitions.AsNoTracking().SingleAsync(seed => seed.ItemId == 5284)).ProductRegrowTicks);
        Assert.AreEqual(16000u, apple.CycleTicks);
    }

    private static async Task SeedAsync()
    {
        await using var context = CreateContext();
        await new FarmingSeedSeeder(context, [new FruitTreeSeedSource(), new BushSeedSource()], NullLogger<FarmingSeedSeeder>.Instance).SeedAsync();
    }

    private static HagalazDbContext CreateContext() => SeedTestDatabase.CreateContext(_database!);
}
