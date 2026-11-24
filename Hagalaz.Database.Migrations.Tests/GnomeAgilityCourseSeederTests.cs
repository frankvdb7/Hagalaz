using Hagalaz.Data;
using Hagalaz.Database.Migrations.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.MySql;

namespace Hagalaz.Database.Migrations.Tests;

[TestClass]
[DoNotParallelize]
public sealed class GnomeAgilityCourseSeederTests
{
    private static MySqlContainer? _database;

    [ClassInitialize]
    public static async Task InitializeAsync(TestContext _) => _database = await SeedTestDatabase.StartAsync("hagalaz-agility-seed-test");

    [ClassCleanup]
    public static Task CleanupAsync() => SeedTestDatabase.StopAsync(_database);

    [TestInitialize]
    public async Task ResetAsync()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await context.SkillsAgilityObstacleDefinitions.ExecuteDeleteAsync();
        await context.SkillsAgilityCourseDefinitions.ExecuteDeleteAsync();
    }

    [TestMethod]
    [Timeout(180000)]
    public async Task Seed_CreatesTheGnomeValuesOnceAndIsIdempotent()
    {
        await SeedAsync();
        await SeedAsync();

        await using var context = CreateContext();
        var obstacles = await context.SkillsAgilityObstacleDefinitions.AsNoTracking().ToDictionaryAsync(obstacle => obstacle.ObjectId, obstacle => obstacle.Experience);
        Assert.HasCount(13, obstacles);
        foreach (var objectId in new uint[] { 69526, 69383, 2312, 4059, 69384, 69377, 69378 })
        {
            Assert.AreEqual(7.5m, obstacles[objectId]);
        }

        foreach (var objectId in new uint[] { 69508, 69507 })
        {
            Assert.AreEqual(5m, obstacles[objectId]);
        }

        foreach (var objectId in new uint[] { 69506, 69514, 43529, 69389 })
        {
            Assert.AreEqual(25m, obstacles[objectId]);
        }

        Assert.AreEqual(85, (await context.SkillsAgilityObstacleDefinitions.AsNoTracking().SingleAsync(obstacle => obstacle.ObjectId == 69506)).RequiredLevel);
        Assert.AreEqual(1, (await context.SkillsAgilityObstacleDefinitions.AsNoTracking().CountAsync(obstacle => obstacle.RequiredLevel != 1)));

        var courses = await context.SkillsAgilityCourseDefinitions.AsNoTracking().ToDictionaryAsync(course => course.Id, course => course.CompletionExperience);
        Assert.HasCount(2, courses);
        Assert.AreEqual(39m, courses[GnomeAgilityCourseSeeder.BasicCourseId]);
        Assert.AreEqual(605m, courses[GnomeAgilityCourseSeeder.AdvancedCourseId]);
    }

    [TestMethod]
    [Timeout(180000)]
    public async Task Seed_NeverChangesAnExistingDefinition()
    {
        await SeedAsync();
        await using (var context = CreateContext())
        {
            await context.SkillsAgilityObstacleDefinitions.Where(obstacle => obstacle.ObjectId == 69526).ExecuteUpdateAsync(s => s.SetProperty(obstacle => obstacle.Experience, 99m));
            await context.SkillsAgilityCourseDefinitions.Where(course => course.Id == GnomeAgilityCourseSeeder.BasicCourseId).ExecuteUpdateAsync(s => s.SetProperty(course => course.CompletionExperience, 77m));
        }

        await SeedAsync();

        await using var verify = CreateContext();
        Assert.AreEqual(99m, (await verify.SkillsAgilityObstacleDefinitions.AsNoTracking().SingleAsync(obstacle => obstacle.ObjectId == 69526)).Experience);
        Assert.AreEqual(77m, (await verify.SkillsAgilityCourseDefinitions.AsNoTracking().SingleAsync(course => course.Id == GnomeAgilityCourseSeeder.BasicCourseId)).CompletionExperience);
    }

    private static async Task SeedAsync()
    {
        await using var context = CreateContext();
        await new GnomeAgilityCourseSeeder(context, NullLogger<GnomeAgilityCourseSeeder>.Instance).SeedAsync();
    }

    private static HagalazDbContext CreateContext() => SeedTestDatabase.CreateContext(_database!);
}
