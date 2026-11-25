using System.Globalization;
using Hagalaz.Data;
using Hagalaz.Database.Migrations.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.MySql;

namespace Hagalaz.Database.Migrations.Tests;

[TestClass]
[DoNotParallelize]
public sealed class HerblorePotionDefinitionSeederTests
{
    private static MySqlContainer? _database;

    [ClassInitialize]
    public static async Task InitializeAsync(TestContext _) => _database = await SeedTestDatabase.StartAsync("hagalaz-herblore-seed-test");

    [ClassCleanup]
    public static Task CleanupAsync() => SeedTestDatabase.StopAsync(_database);

    [TestInitialize]
    public async Task ResetAsync()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await context.SkillsHerblorePotionDefinitions.ExecuteDeleteAsync();
    }

    [TestMethod]
    [Timeout(180000)]
    public async Task Seed_CreatesEveryCurrentRecipeOnceAndIsIdempotent()
    {
        await SeedAsync();
        await SeedAsync();

        await using var context = CreateContext();
        var definitions = await context.SkillsHerblorePotionDefinitions.AsNoTracking().ToListAsync();
        Assert.HasCount(31, definitions);
        Assert.AreEqual(30, definitions.Count(definition => definition.Kind == 0));
        Assert.AreEqual(1, definitions.Count(definition => definition.Kind == 1));
    }

    [TestMethod]
    [Timeout(180000)]
    [DataRow((ushort)121, (byte)0, (ushort)91, (byte)1, "25", "249", "221", (byte)1)]
    [DataRow((ushort)181, (byte)0, (ushort)101, (byte)48, "106.3", "259", "235", (byte)1)]
    [DataRow((ushort)15301, (byte)0, (ushort)3018, (byte)84, "200", "", "5972", (byte)1)]
    [DataRow((ushort)15309, (byte)0, (ushort)145, (byte)88, "220", "", "261", (byte)1)]
    [DataRow((ushort)15321, (byte)0, (ushort)3042, (byte)91, "250", "", "9594", (byte)1)]
    [DataRow((ushort)15325, (byte)0, (ushort)169, (byte)92, "260", "", "12539", (byte)5)]
    [DataRow((ushort)21632, (byte)0, (ushort)21628, (byte)94, "190", "21624", "21622", (byte)1)]
    [DataRow((ushort)15333, (byte)1, (ushort)269, (byte)96, "1000", "", "15309,15313,15317,15321,15325", (byte)1)]
    public async Task Seed_StoresRepresentativeRecipesVerbatim(
        ushort potionId, byte kind, ushort unfinishedPotionId, byte requiredLevel, string experience, string primary, string secondary, byte secondaryCount)
    {
        await SeedAsync();

        await using var context = CreateContext();
        var definition = await context.SkillsHerblorePotionDefinitions.AsNoTracking().SingleAsync(d => d.PotionId == potionId);
        Assert.AreEqual(kind, definition.Kind);
        Assert.AreEqual(unfinishedPotionId, definition.UnfinishedPotionId);
        Assert.AreEqual(requiredLevel, definition.RequiredLevel);
        Assert.AreEqual(decimal.Parse(experience, CultureInfo.InvariantCulture), definition.Experience);
        Assert.AreEqual(primary, definition.PrimaryItemIds);
        Assert.AreEqual(secondary, definition.SecondaryItemIds);
        Assert.AreEqual(secondaryCount, definition.SecondaryItemCount);
    }

    [TestMethod]
    [Timeout(180000)]
    public async Task Seed_NeverChangesAnAlreadySeededDefinition()
    {
        await SeedAsync();
        await using (var context = CreateContext())
        {
            await context.SkillsHerblorePotionDefinitions.Where(definition => definition.PotionId == 15333)
                .ExecuteUpdateAsync(s => s.SetProperty(definition => definition.Experience, 999m));
        }

        await SeedAsync();

        await using var verify = CreateContext();
        Assert.AreEqual(999m, (await verify.SkillsHerblorePotionDefinitions.AsNoTracking().SingleAsync(definition => definition.PotionId == 15333)).Experience);
    }

    private static async Task SeedAsync()
    {
        await using var context = CreateContext();
        await new HerblorePotionDefinitionSeeder(context, NullLogger<HerblorePotionDefinitionSeeder>.Instance).SeedAsync();
    }

    private static HagalazDbContext CreateContext() => SeedTestDatabase.CreateContext(_database!);
}
