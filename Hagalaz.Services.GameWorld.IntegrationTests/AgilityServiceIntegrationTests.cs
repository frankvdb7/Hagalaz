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
public sealed class AgilityServiceIntegrationTests
{
    private static MySqlContainer? _database;

    [ClassInitialize]
    public static async Task InitializeAsync(TestContext _)
    {
        _database = new MySqlBuilder("mysql:8.4")
            .WithDatabase("hagalaz-gameworld-agility-test")
            .WithUsername("root")
            .WithPassword("hagalaz-gameworld-agility-test")
            .WithCommand("--character-set-server=utf8mb4", "--collation-server=utf8mb4_0900_ai_ci")
            .Build();
        await _database.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        context.SkillsAgilityObstacleDefinitions.AddRange(
            new SkillsAgilityObstacleDefinition { ObjectId = 880001, Experience = 7.5m, RequiredLevel = 1 },
            new SkillsAgilityObstacleDefinition { ObjectId = 880002, Experience = 25m, RequiredLevel = 85 });
        context.SkillsAgilityCourseDefinitions.AddRange(
            new SkillsAgilityCourseDefinition { Id = (byte)AgilityCourseId.GnomeBasic, CompletionExperience = 39m },
            new SkillsAgilityCourseDefinition { Id = (byte)AgilityCourseId.GnomeAdvanced, CompletionExperience = 605m });
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
        var service = new AgilityService(provider);

        Assert.IsNull(service.FindObstacleByObjectId(880001));
        Assert.IsNull(service.FindCourseById(AgilityCourseId.GnomeBasic));
    }

    [TestMethod]
    [Timeout(120000)]
    public async Task LoadAsync_ServesPersistedObstacleAndCourseDefinitionsFromMemory()
    {
        using var provider = CreateServiceProvider();
        var service = new AgilityService(provider);

        await service.LoadAsync();

        var obstacle = service.FindObstacleByObjectId(880002);
        Assert.IsNotNull(obstacle);
        Assert.AreEqual(25d, obstacle.Experience);
        Assert.AreEqual(85, obstacle.RequiredLevel);
        Assert.AreEqual(7.5d, service.FindObstacleByObjectId(880001)!.Experience);
        Assert.IsNull(service.FindObstacleByObjectId(990000));
        Assert.AreEqual(39d, service.FindCourseById(AgilityCourseId.GnomeBasic)!.CompletionExperience);
        Assert.AreEqual(605d, service.FindCourseById(AgilityCourseId.GnomeAdvanced)!.CompletionExperience);
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<HagalazDbContext>(options => options.UseMySQL(_database!.GetConnectionString()));
        services.AddScoped<IAgilityObstacleDefinitionRepository, AgilityObstacleDefinitionRepository>();
        services.AddScoped<IAgilityCourseDefinitionRepository, AgilityCourseDefinitionRepository>();
        services.AddSingleton<IMapper>(provider =>
            new MapperConfiguration(expression => expression.AddProfile<SkillsProfile>(), provider.GetRequiredService<ILoggerFactory>()).CreateMapper());
        return services.BuildServiceProvider();
    }

    private static HagalazDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<HagalazDbContext>().UseMySQL(_database!.GetConnectionString()).Options);
}
