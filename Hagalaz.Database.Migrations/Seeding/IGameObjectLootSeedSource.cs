namespace Hagalaz.Database.Migrations.Seeding;

/// <summary>
///     Supplies game-object loot tables to seed. A feature adds its tables by providing one source; the seeder itself
///     does not change.
/// </summary>
public interface IGameObjectLootSeedSource
{
    IReadOnlyList<GameObjectLootSeedTable> Tables { get; }
}
