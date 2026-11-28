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
public sealed class FletchingServiceIntegrationTests
{
    private static MySqlContainer? _database;

    [ClassInitialize]
    public static async Task InitializeAsync(TestContext _)
    {
        _database = new MySqlBuilder("mysql:8.4")
            .WithDatabase("hagalaz-gameworld-fletching-test")
            .WithUsername("root")
            .WithPassword("hagalaz-gameworld-fletching-test")
            .WithCommand("--character-set-server=utf8mb4", "--collation-server=utf8mb4_0900_ai_ci")
            .Build();
        await _database.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        context.SkillsFletchingDefinitions.AddRange(
            new SkillsFletchingDefinition
            {
                ResourceId = 60001, ToolId = 60002, Category = (byte)FletchingCategory.Wood, ProductIds = "60010,60011", ProductCounts = "7,1",
                ProductExperiences = "0.33,83.25", RequiredLevels = "3,44", AnimationId = 6702
            },
            new SkillsFletchingDefinition
            {
                ResourceId = 60003, ToolId = 60004, Category = (byte)FletchingCategory.Bow, ProductIds = "60012", ProductCounts = "1",
                ProductExperiences = "91.5", RequiredLevels = "85", AnimationId = null
            });
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
    public void Lookups_BeforeLoadAreEmpty()
    {
        using var provider = CreateServiceProvider();
        var service = new FletchingService(provider);

        Assert.IsNull(service.FindRecipe(FletchingCategory.Wood, 60001, 60002));
        Assert.IsEmpty(service.FindRecipes(FletchingCategory.Wood));
    }

    [TestMethod]
    [Timeout(120000)]
    public async Task LoadAsync_ServesPersistedRecipesFromMemoryAfterTheProviderIsDisposed()
    {
        FletchingService service;
        using (var provider = CreateServiceProvider())
        {
            service = new FletchingService(provider);
            await service.LoadAsync();
        }

        var wood = service.FindRecipe(FletchingCategory.Wood, 60002, 60001);
        Assert.IsNotNull(wood);
        Assert.AreEqual(60001, wood.ResourceId);
        CollectionAssert.AreEqual(new[] { 60010, 60011 }, wood.ProductIds);
        CollectionAssert.AreEqual(new[] { 7, 1 }, wood.ProductCounts);
        CollectionAssert.AreEqual(new[] { 0.33, 83.25 }, wood.ProductExperiences);
        CollectionAssert.AreEqual(new[] { 3, 44 }, wood.RequiredLevels);
        Assert.AreEqual(6702, wood.AnimationId);

        var bow = service.FindRecipe(FletchingCategory.Bow, 60003, 60004);
        Assert.IsNotNull(bow);
        Assert.AreEqual(-1, bow.AnimationId);

        Assert.IsNull(service.FindRecipe(FletchingCategory.Ammo, 60001, 60002));
        Assert.IsNull(service.FindRecipe(FletchingCategory.Wood, 60001, 60004));
        Assert.IsTrue(service.FindRecipes(FletchingCategory.Wood).Any(recipe => recipe.ResourceId == 60001));
        Assert.IsTrue(service.FindRecipes(FletchingCategory.Bow).Any(recipe => recipe.ResourceId == 60003));
        Assert.IsEmpty(service.FindRecipes(FletchingCategory.Tips));
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<HagalazDbContext>(options => options.UseMySQL(_database!.GetConnectionString()));
        services.AddScoped<IFletchingDefinitionRepository, FletchingDefinitionRepository>();
        services.AddSingleton<IMapper>(provider =>
            new MapperConfiguration(expression => expression.AddProfile<SkillsProfile>(), provider.GetRequiredService<ILoggerFactory>()).CreateMapper());
        return services.BuildServiceProvider();
    }

    private static HagalazDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<HagalazDbContext>().UseMySQL(_database!.GetConnectionString()).Options);
}
