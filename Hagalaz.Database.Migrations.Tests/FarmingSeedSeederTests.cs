using Hagalaz.Data;
using Hagalaz.Database.Migrations.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MySql.EntityFrameworkCore.Extensions;
using Testcontainers.MySql;

namespace Hagalaz.Database.Migrations.Tests;

[TestClass]
[DoNotParallelize]
public sealed class FarmingSeedSeederTests
{
    private static MySqlContainer? _database;

    [ClassInitialize]
    public static async Task InitializeAsync(TestContext _)
    {
        _database = new MySqlBuilder("mysql:8.4")
            .WithDatabase("hagalaz-farming-seed-test")
            .WithUsername("root")
            .WithPassword("hagalaz-farming-seed-test")
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

    private static async Task SeedAsync()
    {
        await using var context = CreateContext();
        await new FarmingSeedSeeder(context, [new FruitTreeSeedSource()], NullLogger<FarmingSeedSeeder>.Instance).SeedAsync();
    }

    private static HagalazDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<HagalazDbContext>().UseMySQL(_database!.GetConnectionString()).Options);
}
