using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Database.Migrations.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.MySql;

namespace Hagalaz.Database.Migrations.Tests;

[TestClass]
[DoNotParallelize]
public sealed class FletchingDefinitionSeederTests
{
    private static MySqlContainer? _database;

    [ClassInitialize]
    public static async Task InitializeAsync(TestContext _) => _database = await SeedTestDatabase.StartAsync("hagalaz-fletching-seed-test");

    [ClassCleanup]
    public static Task CleanupAsync() => SeedTestDatabase.StopAsync(_database);

    [TestInitialize]
    public async Task ResetAsync()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await context.SkillsFletchingDefinitions.ExecuteDeleteAsync();
    }

    [TestMethod]
    [Timeout(180000)]
    public async Task Seed_CreatesEveryCurrentRecipeOnceAndIsIdempotent()
    {
        await SeedAsync();
        await SeedAsync();

        await using var context = CreateContext();
        var definitions = await context.SkillsFletchingDefinitions.AsNoTracking().ToListAsync();
        Assert.HasCount(78, definitions);
        Assert.AreEqual(6, definitions.Count(definition => definition.Category == 1));
        Assert.AreEqual(26, definitions.Count(definition => definition.Category == 2));
        Assert.AreEqual(35, definitions.Count(definition => definition.Category == 3));
        Assert.AreEqual(11, definitions.Count(definition => definition.Category == 4));
    }

    [TestMethod]
    [Timeout(180000)]
    [DataRow((ushort)1511, (ushort)946, (byte)1, "52,50,48,9440", "15,1,1,1", "0.33,5,10,6", "1,5,10,9", (ushort)6702)]
    [DataRow((ushort)72, (ushort)1777, (byte)2, "861", "1", "83.25", "80", (ushort)6683)]
    [DataRow((ushort)9440, (ushort)9420, (byte)2, "9454", "1", "6", "9", null)]
    [DataRow((ushort)9465, (ushort)9438, (byte)2, "9185", "1", "50", "69", (ushort)6677)]
    [DataRow((ushort)11237, (ushort)53, (byte)3, "11212", "15", "16.3", "90", null)]
    [DataRow((ushort)9194, (ushort)9144, (byte)3, "9342", "10", "9.4", "73", null)]
    [DataRow((ushort)11232, (ushort)314, (byte)3, "11230", "10", "250", "95", null)]
    [DataRow((ushort)9416, (ushort)954, (byte)3, "9418", "1", "59", "59", null)]
    [DataRow((ushort)411, (ushort)1755, (byte)4, "46", "6", "3.2", "41", null)]
    [DataRow((ushort)6573, (ushort)1755, (byte)4, "9194", "24", "9.4", "73", null)]
    public async Task Seed_StoresRepresentativeRecipesVerbatim(
        ushort resourceId, ushort toolId, byte category, string productIds, string counts, string experiences, string levels, ushort? animationId)
    {
        await SeedAsync();

        await using var context = CreateContext();
        var definition = await context.SkillsFletchingDefinitions.AsNoTracking().SingleAsync(d => d.ResourceId == resourceId);
        Assert.AreEqual(toolId, definition.ToolId);
        Assert.AreEqual(category, definition.Category);
        Assert.AreEqual(productIds, definition.ProductIds);
        Assert.AreEqual(counts, definition.ProductCounts);
        Assert.AreEqual(experiences, definition.ProductExperiences);
        Assert.AreEqual(levels, definition.RequiredLevels);
        Assert.AreEqual(animationId, definition.AnimationId);
    }

    [TestMethod]
    [Timeout(180000)]
    public async Task Seed_NeverChangesAnAlreadySeededDefinition()
    {
        await SeedAsync();
        await using (var context = CreateContext())
        {
            await context.SkillsFletchingDefinitions.Where(definition => definition.ResourceId == 1511)
                .ExecuteUpdateAsync(s => s.SetProperty(definition => definition.ProductExperiences, "9,9,9,9"));
        }

        await SeedAsync();

        await using var verify = CreateContext();
        Assert.AreEqual("9,9,9,9", (await verify.SkillsFletchingDefinitions.AsNoTracking().SingleAsync(definition => definition.ResourceId == 1511)).ProductExperiences);
    }

    [TestMethod]
    [Timeout(180000)]
    public async Task Seed_ClaimsALegacyDefinitionWithoutCategory()
    {
        await using (var context = CreateContext())
        {
            context.SkillsFletchingDefinitions.Add(new SkillsFletchingDefinition
            {
                ResourceId = 1511, ToolId = 1, ProductIds = "1", ProductCounts = "1", ProductExperiences = "1", RequiredLevels = "1", AnimationId = 1
            });
            await context.SaveChangesAsync();
        }

        await SeedAsync();

        await using var verify = CreateContext();
        var definition = await verify.SkillsFletchingDefinitions.AsNoTracking().SingleAsync(d => d.ResourceId == 1511);
        Assert.AreEqual((byte)1, definition.Category);
        Assert.AreEqual((ushort)946, definition.ToolId);
        Assert.AreEqual("0.33,5,10,6", definition.ProductExperiences);
    }

    private static async Task SeedAsync()
    {
        await using var context = CreateContext();
        await new FletchingDefinitionSeeder(context, NullLogger<FletchingDefinitionSeeder>.Instance).SeedAsync();
    }

    private static HagalazDbContext CreateContext() => SeedTestDatabase.CreateContext(_database!);
}
