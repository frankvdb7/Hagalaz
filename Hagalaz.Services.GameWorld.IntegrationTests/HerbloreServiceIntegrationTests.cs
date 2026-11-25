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
using MySql.EntityFrameworkCore.Extensions;
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
            Potion(60030, PotionKind.Standard, 60010, "", "60302", secondaryItemCount: 5),
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

        CollectionAssert.AreEqual(new[] { 60010, 60020, 60030 }, service.FindPotions(PotionKind.Standard).Select(p => p.PotionId).ToArray());
        CollectionAssert.AreEqual(new[] { 60040 }, service.FindPotions(PotionKind.Overload).Select(p => p.PotionId).ToArray());
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
