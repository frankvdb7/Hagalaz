using AutoMapper;
using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Scripts.Skills.Agility;
using Hagalaz.Game.Scripts.Skills.Agility.Courses.Gnome;
using Hagalaz.Services.GameWorld.Data;
using Hagalaz.Services.GameWorld.Profiles;
using Hagalaz.Services.GameWorld.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
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
            new SkillsAgilityObstacleDefinition { ObjectId = 880001, Experience = 13.25m, RequiredLevel = 7 },
            new SkillsAgilityObstacleDefinition { ObjectId = 880002, Experience = 41.5m, RequiredLevel = 63 });
        context.SkillsAgilityCourseDefinitions.AddRange(
            new SkillsAgilityCourseDefinition { Id = (byte)AgilityCourseId.GnomeBasic, CompletionExperience = 77.5m },
            new SkillsAgilityCourseDefinition { Id = (byte)AgilityCourseId.GnomeAdvanced, CompletionExperience = 912.25m });
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
    public async Task LoadAsync_ServesCustomizedDefinitionsFromMemoryAfterTheProviderIsDisposed()
    {
        AgilityService service;
        using (var provider = CreateServiceProvider())
        {
            service = new AgilityService(provider);
            await service.LoadAsync();
        }

        var obstacle = service.FindObstacleByObjectId(880002);
        Assert.IsNotNull(obstacle);
        Assert.AreEqual(41.5d, obstacle.Experience);
        Assert.AreEqual(63, obstacle.RequiredLevel);
        var other = service.FindObstacleByObjectId(880001);
        Assert.IsNotNull(other);
        Assert.AreEqual(13.25d, other.Experience);
        Assert.AreEqual(7, other.RequiredLevel);
        Assert.IsNull(service.FindObstacleByObjectId(990000));
        Assert.AreEqual(77.5d, service.FindCourseById(AgilityCourseId.GnomeBasic)!.CompletionExperience);
        Assert.AreEqual(912.25d, service.FindCourseById(AgilityCourseId.GnomeAdvanced)!.CompletionExperience);
        Assert.IsNull(service.FindCourseById((AgilityCourseId)99));
    }

    [TestMethod]
    [Timeout(120000)]
    public async Task AgilitySkillService_GrantsTheCustomizedLoadedRewards()
    {
        AgilityService service;
        using (var provider = CreateServiceProvider())
        {
            service = new AgilityService(provider);
            await service.LoadAsync();
        }

        var skillService = new AgilitySkillService(service, [new GnomeBasicCourse()]);
        var statistics = Substitute.For<ICharacterStatistics>();
        var character = Substitute.For<ICharacter>();
        character.Statistics.Returns(statistics);
        character.HasState<GnomeCourseLogBalanceState>().Returns(true);
        character.HasState<GnomeCourseFirstObstacleNetState>().Returns(true);
        character.HasState<GnomeCourseFirstTreeBranchState>().Returns(true);
        character.HasState<GnomeCourseBalancingRopeState>().Returns(true);
        character.HasState<GnomeCourseTreeBranchDownState>().Returns(true);
        character.HasState<GnomeCourseSecondObstacleNetState>().Returns(true);
        character.HasState<GnomeCourseObstaclePipeState>().Returns(true);

        skillService.AddObstacleExperience(character, 880002);
        skillService.AddObstacleExperience(character, 990000);
        skillService.CheckCourseCompletion(character);

        Assert.AreEqual(63, skillService.GetRequiredLevel(880002));
        Assert.AreEqual(1, skillService.GetRequiredLevel(990000));
        statistics.Received(1).AddExperience(StatisticsConstants.Agility, 41.5);
        statistics.Received(1).AddExperience(StatisticsConstants.Agility, 77.5);
        statistics.Received(2).AddExperience(Arg.Any<int>(), Arg.Any<double>());
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
