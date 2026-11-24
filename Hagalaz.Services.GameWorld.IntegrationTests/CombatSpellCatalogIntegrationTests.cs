using Hagalaz.Data.Entities;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Data;
using Hagalaz.Services.GameWorld.Services;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MySql;

namespace Hagalaz.Services.GameWorld.IntegrationTests;

[TestClass]
[DoNotParallelize]
public sealed class CombatSpellCatalogIntegrationTests
{
    private static MySqlContainer? _database;

    [ClassInitialize]
    public static async Task InitializeAsync(TestContext _)
    {
        _database = await GameWorldTestDatabase.StartAsync("hagalaz-gameworld-combat-spell-test");
        await using var context = GameWorldTestDatabase.CreateContext(_database);
        context.SkillsMagicCombatDefinitions.AddRange(
            CreateDefinition(25, "StandartBook", 20, "556,558", "1,1"),
            CreateDefinition(25, "AncientBook", 80, "560,565", "2,4"));
        await context.SaveChangesAsync();
    }

    [ClassCleanup]
    public static Task CleanupAsync() => GameWorldTestDatabase.StopAsync(_database);

    [TestMethod]
    [Timeout(120000)]
    public void CombatSpells_BeforeLoadAreEmpty()
    {
        using var provider = CreateProvider();

        Assert.IsEmpty(new CombatSpellCatalog(provider).CombatSpells);
    }

    [TestMethod]
    [Timeout(120000)]
    public async Task LoadAsync_ServesBothSpellbooksFromMemoryAfterTheProviderIsDisposed()
    {
        CombatSpellCatalog catalog;
        using (var provider = CreateProvider())
        {
            catalog = new CombatSpellCatalog(provider);
            await catalog.LoadAsync();
        }

        Assert.HasCount(2, catalog.CombatSpells);
        var standard = catalog.CombatSpells.Single(spell => spell.SpellBook == MagicBook.StandardBook);
        var ancient = catalog.CombatSpells.Single(spell => spell.SpellBook == MagicBook.AncientBook);
        Assert.AreEqual(25, standard.ButtonId);
        Assert.AreEqual(25, ancient.ButtonId);
        Assert.AreEqual(80, ancient.RequiredLevel);
        CollectionAssert.AreEqual(new[] { RuneType.Death, RuneType.Blood }, ancient.RequiredRunes);
        CollectionAssert.AreEqual(new[] { 2, 4 }, ancient.RequiredRunesCounts);
    }

    private static ServiceProvider CreateProvider() =>
        GameWorldTestDatabase.CreateServiceProvider(_database!, services => services.AddScoped<IMagicCombatSpellsRepository, MagicCombatSpellsRepository>());

    private static SkillsMagicCombatDefinition CreateDefinition(ushort buttonId, string spellBook, byte level, string runes, string counts) => new()
    {
        ButtonId = buttonId,
        SpellBook = spellBook,
        RequiredLevel = level,
        RequiredRunes = runes,
        RequiredRunesCounts = counts,
        CastAnimationId = -1,
        CastGraphicId = -1,
        EndGraphicId = -1,
        ProjectileId = -1
    };
}
