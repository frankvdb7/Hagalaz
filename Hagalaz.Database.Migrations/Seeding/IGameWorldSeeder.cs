namespace Hagalaz.Database.Migrations.Seeding;

/// <summary>
///     Seeds one kind of game-world data. Implementations must be idempotent and must never change data that already
///     exists in the database. They run in the migration step, under the migration lock, after the schema is current.
/// </summary>
public interface IGameWorldSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
