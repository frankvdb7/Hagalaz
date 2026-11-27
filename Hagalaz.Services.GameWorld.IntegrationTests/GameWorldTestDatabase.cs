using AutoMapper;
using Hagalaz.Data;
using Hagalaz.Services.GameWorld.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.MySql;

namespace Hagalaz.Services.GameWorld.IntegrationTests;

/// <summary>
///     A migrated MySQL container plus the service provider wiring (DbContext, mapper) that startup-loaded services resolve from.
/// </summary>
internal static class GameWorldTestDatabase
{
    public static async Task<MySqlContainer> StartAsync(string name)
    {
        var database = new MySqlBuilder("mysql:8.4")
            .WithDatabase(name)
            .WithUsername("root")
            .WithPassword(name)
            .WithCommand("--character-set-server=utf8mb4", "--collation-server=utf8mb4_0900_ai_ci")
            .Build();
        await database.StartAsync();
        await using var context = CreateContext(database);
        await context.Database.MigrateAsync();
        return database;
    }

    public static async Task StopAsync(MySqlContainer? database)
    {
        if (database != null)
        {
            await database.DisposeAsync();
        }
    }

    public static HagalazDbContext CreateContext(MySqlContainer database) =>
        new(new DbContextOptionsBuilder<HagalazDbContext>().UseMySQL(database.GetConnectionString()).Options);

    public static ServiceProvider CreateServiceProvider(MySqlContainer database, Action<IServiceCollection> addRepositories)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<HagalazDbContext>(options => options.UseMySQL(database.GetConnectionString()));
        addRepositories(services);
        services.AddSingleton<IMapper>(provider =>
            new MapperConfiguration(expression => expression.AddProfile<SkillsProfile>(), provider.GetRequiredService<ILoggerFactory>()).CreateMapper());
        return services.BuildServiceProvider();
    }
}
