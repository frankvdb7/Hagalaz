using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Database.Migrations.Seeding;

/// <summary>
///     Registers and runs the game-world seeders. To seed more data, register another <see cref="IGameWorldSeeder" />
///     (or another <see cref="IGameObjectLootSeedSource" /> / <see cref="IFarmingSeedSource" /> for loot tables / farming seeds) here.
/// </summary>
public static class GameWorldSeeding
{
    public static IServiceCollection AddGameWorldSeeding(this IServiceCollection services)
    {
        services.AddScoped<IGameObjectLootSeedSource, BarrowsLootSeedSource>();
        services.AddScoped<IGameWorldSeeder, GameObjectLootSeeder>();
        services.AddScoped<IFarmingSeedSource, FruitTreeSeedSource>();
        services.AddScoped<IFarmingSeedSource, BushSeedSource>();
        services.AddScoped<IGameWorldSeeder, FarmingSeedSeeder>();
        services.AddScoped<IGameWorldSeeder, GnomeAgilityCourseSeeder>();
        services.AddScoped<IGameWorldSeeder, FletchingDefinitionSeeder>();
        return services;
    }

    /// <summary>
    ///     Runs every registered seeder in its own scope.
    /// </summary>
    public static async Task SeedGameWorldAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        foreach (var seeder in scope.ServiceProvider.GetServices<IGameWorldSeeder>())
        {
            await seeder.SeedAsync(cancellationToken);
        }
    }
}
