using AutoMapper;
using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Services.GameWorld.Data;
using Hagalaz.Services.GameWorld.Profiles;
using Hagalaz.Services.GameWorld.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.MySql;

namespace Hagalaz.Services.GameWorld.IntegrationTests;

[TestClass]
[DoNotParallelize]
public sealed class HerbloreServiceIntegrationTests
{
    private static MySqlContainer? _database;

    [ClassInitialize]
    public static async Task InitializeAsync(TestContext _)
    {
        _database = new MySqlBuilder("mysql:8.4")
            .WithDatabase("hagalaz-gameworld-herblore-test")
            .WithUsername("root")
            .WithPassword("hagalaz-gameworld-herblore-test")
            .WithCommand("--character-set-server=utf8mb4", "--collation-server=utf8mb4_0900_ai_ci")
            .Build();
        await _database.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        context.SkillsHerblorePotionDefinitions.AddRange(
            // inserted out of potion id order: the lowest potion id wins a shared primary ingredient
            Potion(60020, PotionKind.Standard, 60100, "60200", "60300"),
            Potion(60010, PotionKind.Standard, 60101, "60200", "60301", requiredLevel: 44, experience: 106.3m),
            // shares unfinished potion 60101 with 60010 (like super attack/super antipoison sharing 101)
            Potion(60050, PotionKind.Standard, 60101, "60201", "60303"),
            // made from final potion 60010 (like extreme attack from super attack)
            Potion(60030, PotionKind.Standard, 60010, "", "60302", secondaryItemCount: 5),
            // consumes final potions 60030 and 60010 (like overload from the extreme potions)
            Potion(60040, PotionKind.Overload, 60400, "", "60030,60010"));
        context.SkillsHerbloreHerbDefinitions.Add(new SkillsHerbloreHerbDefinition { GrimyItemId = 60500, CleanItemId = 60501, RequiredLevel = 3, Experience = 2.5m });
        await context.SaveChangesAsync();
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
    public void PotionLookups_BeforeLoadAreEmpty()
    {
        using var provider = CreateServiceProvider();
        var service = new HerbloreService(provider);

        Assert.IsNull(service.FindPotionByPrimaryItemId(60200));
        Assert.IsEmpty(service.FindPotions(PotionKind.Standard));
    }

    [TestMethod]
    [Timeout(120000)]
    public async Task LoadAsync_ServesPersistedPotionsFromMemoryAfterTheProviderIsDisposed()
    {
        HerbloreService service;
        using (var provider = CreateServiceProvider())
        {
            service = new HerbloreService(provider);
            await service.LoadAsync();
        }

        var byPrimary = service.FindPotionByPrimaryItemId(60200);
        Assert.IsNotNull(byPrimary);
        Assert.AreEqual(60010, byPrimary.PotionId);
        Assert.AreEqual(60101, byPrimary.UnfinishedPotionId);
        Assert.AreEqual(44, byPrimary.RequiredLevel);
        Assert.AreEqual(106.3, byPrimary.Experience);

        var bySecondary = service.FindPotionBySecondaryItemId(PotionKind.Standard, 60010, 60302);
        Assert.IsNotNull(bySecondary);
        Assert.AreEqual(60030, bySecondary.PotionId);
        Assert.AreEqual(5, bySecondary.SecondaryItemCount);
        Assert.IsNull(service.FindPotionBySecondaryItemId(PotionKind.Standard, 60100, 60302));

        var overload = service.FindPotionBySecondaryItemId(PotionKind.Overload, 60400, 60010);
        Assert.IsNotNull(overload);
        Assert.AreEqual(60040, overload.PotionId);
        Assert.IsNull(service.FindPotionBySecondaryItemId(PotionKind.Standard, 60400, 60010));

        CollectionAssert.AreEqual(new[] { 60010, 60020, 60030, 60050 }, service.FindPotions(PotionKind.Standard).Select(p => p.PotionId).ToArray());
        CollectionAssert.AreEqual(new[] { 60040 }, service.FindPotions(PotionKind.Overload).Select(p => p.PotionId).ToArray());
    }

    /// <summary>
    ///     Issue #375 asks for lookup by unfinished and by final potion. Production only ever resolves a potion from the item a secondary
    ///     ingredient is used on, so both are served by the (unfinished/base item, secondary ingredient) lookup rather than id-only queries.
    /// </summary>
    [TestMethod]
    [Timeout(120000)]
    public async Task SecondaryLookup_ResolvesByUnfinishedPotionIncludingSharedIdsAndByFinalPotion()
    {
        var service = await LoadAsync();

        // a shared unfinished potion resolves to the recipe of the secondary ingredient used on it
        Assert.AreEqual(60010, service.FindPotionBySecondaryItemId(PotionKind.Standard, 60101, 60301)?.PotionId);
        Assert.AreEqual(60050, service.FindPotionBySecondaryItemId(PotionKind.Standard, 60101, 60303)?.PotionId);
        Assert.IsNull(service.FindPotionBySecondaryItemId(PotionKind.Standard, 60101, 60302));

        // a final potion is the base of another standard potion
        Assert.AreEqual(60030, service.FindPotionBySecondaryItemId(PotionKind.Standard, 60010, 60302)?.PotionId);

        // each final potion consumed by the overload resolves it, from either final potion
        Assert.AreEqual(60040, service.FindPotionBySecondaryItemId(PotionKind.Overload, 60400, 60030)?.PotionId);
        Assert.AreEqual(60040, service.FindPotionBySecondaryItemId(PotionKind.Overload, 60400, 60010)?.PotionId);
        Assert.IsNull(service.FindPotionBySecondaryItemId(PotionKind.Overload, 60400, 60050));
    }

    [TestMethod]
    [Timeout(120000)]
    public async Task HerbLookups_QueryTheRepository()
    {
        using var provider = CreateServiceProvider();
        var service = new HerbloreService(provider);

        var herb = await service.FindGrimyHerbById(60500);

        Assert.IsNotNull(herb);
        Assert.AreEqual(60501, herb.CleanHerbId);
        Assert.IsTrue((await service.FindAllHerbs()).Any(h => h.GrimyHerbId == 60500));
    }

    private static async Task<HerbloreService> LoadAsync()
    {
        using var provider = CreateServiceProvider();
        var service = new HerbloreService(provider);
        await service.LoadAsync();
        return service;
    }

    private static SkillsHerblorePotionDefinition Potion(ushort potionId, PotionKind kind, ushort unfinishedPotionId, string primary, string secondary,
        byte requiredLevel = 1, decimal experience = 1, byte secondaryItemCount = 1) =>
        new()
        {
            PotionId = potionId, Kind = (byte)kind, UnfinishedPotionId = unfinishedPotionId, RequiredLevel = requiredLevel, Experience = experience,
            PrimaryItemIds = primary, SecondaryItemIds = secondary, SecondaryItemCount = secondaryItemCount
        };

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<HagalazDbContext>(options => options.UseMySQL(_database!.GetConnectionString()));
        services.AddScoped<IHerbloreRepository, HerbloreRepository>();
        services.AddScoped<IHerblorePotionDefinitionRepository, HerblorePotionDefinitionRepository>();
        services.AddSingleton<IMapper>(provider =>
            new MapperConfiguration(expression => expression.AddProfile<SkillsProfile>(), provider.GetRequiredService<ILoggerFactory>()).CreateMapper());
        return services.BuildServiceProvider();
    }

    private static HagalazDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<HagalazDbContext>().UseMySQL(_database!.GetConnectionString()).Options);
}
