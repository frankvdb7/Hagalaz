using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Database.Migrations.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.MySql;

namespace Hagalaz.Database.Migrations.Tests;

[TestClass]
[DoNotParallelize]
public sealed class AncientCombatSpellSeederTests
{
    private static MySqlContainer? _database;

    [ClassInitialize]
    public static async Task InitializeAsync(TestContext _) => _database = await SeedTestDatabase.StartAsync("hagalaz-ancient-combat-seed-test");

    [ClassCleanup]
    public static Task CleanupAsync() => SeedTestDatabase.StopAsync(_database);

    [TestInitialize]
    public async Task ResetAsync()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await context.SkillsMagicCombatDefinitions.ExecuteDeleteAsync();
    }

    // button, level, experience, base damage, autocast config, runes, counts: the values the Ancient spell classes used to hard-code.
    [TestMethod]
    [DataRow(20, 58, 34.0, 180, 69, "562,560,555", "2,2,2")] // ice rush
    [DataRow(21, 82, 46.0, 260, 85, "560,565,555", "2,2,3")] // ice blitz
    [DataRow(22, 70, 40.0, 220, 77, "562,560,555", "4,2,4")] // ice burst
    [DataRow(23, 94, 52.0, 300, 93, "560,565,555", "4,2,6")] // ice barrage
    [DataRow(24, 56, 33.0, 150, 67, "562,560,565", "2,2,1")] // blood rush
    [DataRow(25, 80, 45.0, 250, 83, "560,565", "2,4")] // blood blitz
    [DataRow(26, 68, 39.0, 210, 75, "562,560,565", "4,2,2")] // blood burst
    [DataRow(27, 92, 51.0, 290, 91, "560,565,566", "4,2,1")] // blood barrage
    [DataRow(28, 50, 30.0, 130, 63, "562,560,554,556", "2,2,1,1")] // smoke rush
    [DataRow(29, 74, 42.0, 264, 79, "560,565,554,556", "2,2,2,2")] // smoke blitz
    [DataRow(30, 62, 36.0, 170, 71, "562,560,554,556", "4,2,2,2")] // smoke burst
    [DataRow(31, 86, 48.0, 270, 87, "560,565,554,556", "4,2,4,4")] // smoke barrage
    [DataRow(32, 52, 31.0, 140, 65, "562,560,556,566", "2,2,1,1")] // shadow rush
    [DataRow(33, 76, 43.0, 240, 81, "560,565,556,566", "2,2,2,2")] // shadow blitz
    [DataRow(34, 64, 37.0, 180, 73, "562,560,556,566", "4,2,1,2")] // shadow burst
    [DataRow(35, 88, 49.0, 280, 89, "560,565,556,566", "4,2,4,3")] // shadow barrage
    [DataRow(36, 61, 36.0, 180, 95, "562,557,566", "2,1,1")] // miasmic rush
    [DataRow(37, 85, 48.0, 280, 99, "565,557,566", "2,3,3")] // miasmic blitz
    [DataRow(38, 73, 42.0, 240, 97, "562,557,566", "4,2,2")] // miasmic burst
    [DataRow(39, 97, 54.0, 352, 101, "565,557,566", "4,4,4")] // miasmic barrage
    [Timeout(180000)]
    public async Task Seed_PersistsTheOriginalValuesOfEveryAncientButton(
        int buttonId, int level, double experience, int baseDamage, int autocastConfig, string runes, string counts)
    {
        await SeedAsync();

        await using var context = CreateContext();
        var row = await context.SkillsMagicCombatDefinitions.AsNoTracking().SingleAsync(d => d.ButtonId == buttonId && d.SpellBook == AncientCombatSpellSeeder.AncientBook);
        Assert.AreEqual(level, row.RequiredLevel);
        Assert.AreEqual((decimal)experience, row.BaseExperience);
        Assert.AreEqual((uint)baseDamage, row.BaseDamage);
        Assert.AreEqual((uint)autocastConfig, row.AutocastConfig);
        Assert.AreEqual(runes, row.RequiredRunes);
        Assert.AreEqual(counts, row.RequiredRunesCounts);
        Assert.AreEqual(-1, row.CastAnimationId);
        Assert.AreEqual(-1, row.ProjectileId);
    }

    [TestMethod]
    [Timeout(180000)]
    public async Task Seed_AddsTwentyRowsOnceAndLeavesStandardBookSpellsAlone()
    {
        await using (var context = CreateContext())
        {
            context.SkillsMagicCombatDefinitions.Add(CreateStandardDefinition(25, 7));
            await context.SaveChangesAsync();
        }

        await SeedAsync();
        await SeedAsync();

        await using var verify = CreateContext();
        Assert.AreEqual(20, await verify.SkillsMagicCombatDefinitions.CountAsync(d => d.SpellBook == AncientCombatSpellSeeder.AncientBook));
        var standard = await verify.SkillsMagicCombatDefinitions.AsNoTracking().SingleAsync(d => d.SpellBook == "StandartBook");
        Assert.AreEqual(25, standard.ButtonId);
        Assert.AreEqual(7, standard.RequiredLevel);
    }

    [TestMethod]
    [Timeout(180000)]
    public async Task Seed_NeverChangesAnExistingDefinition()
    {
        await SeedAsync();
        await using (var context = CreateContext())
        {
            await context.SkillsMagicCombatDefinitions.Where(d => d.ButtonId == 36 && d.SpellBook == AncientCombatSpellSeeder.AncientBook)
                .ExecuteUpdateAsync(s => s.SetProperty(d => d.BaseDamage, 999u));
        }

        await SeedAsync();

        await using var verify = CreateContext();
        Assert.AreEqual(999u, (await verify.SkillsMagicCombatDefinitions.AsNoTracking().SingleAsync(d => d.ButtonId == 36)).BaseDamage);
    }

    private static SkillsMagicCombatDefinition CreateStandardDefinition(ushort buttonId, byte level) => new()
    {
        ButtonId = buttonId,
        SpellBook = "StandartBook",
        RequiredLevel = level,
        RequiredRunes = "556",
        RequiredRunesCounts = "1",
        CastAnimationId = -1,
        CastGraphicId = -1,
        EndGraphicId = -1,
        ProjectileId = -1
    };

    private static async Task SeedAsync()
    {
        await using var context = CreateContext();
        await new AncientCombatSpellSeeder(context, NullLogger<AncientCombatSpellSeeder>.Instance).SeedAsync();
    }

    private static HagalazDbContext CreateContext() => SeedTestDatabase.CreateContext(_database!);
}
