using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Database.Migrations.Seeding;

/// <summary>
///     Seeds the game-object loot tables supplied by the registered sources. A table is only created when its id is
///     absent: an existing table, however it was edited, is never changed, and a table whose id is used under another
///     name is reported and left alone.
/// </summary>
public sealed class GameObjectLootSeeder : IGameWorldSeeder
{
    private readonly HagalazDbContext _context;
    private readonly IEnumerable<IGameObjectLootSeedSource> _sources;
    private readonly ILogger<GameObjectLootSeeder> _logger;

    public GameObjectLootSeeder(HagalazDbContext context, IEnumerable<IGameObjectLootSeedSource> sources, ILogger<GameObjectLootSeeder> logger)
    {
        _context = context;
        _sources = sources;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var tables = _sources.SelectMany(source => source.Tables).ToList();
        var ids = tables.Select(table => table.Id).ToArray();
        var existing = await _context.GameobjectLoots
            .AsNoTracking()
            .Where(loot => ids.Contains(loot.Id))
            .ToDictionaryAsync(loot => loot.Id, loot => loot.Name, cancellationToken);

        foreach (var table in tables)
        {
            if (existing.TryGetValue(table.Id, out var name))
            {
                if (name != table.Name)
                {
                    _logger.LogWarning(
                        "Game-object loot table {Id} is '{Name}', not '{Expected}'; it was not seeded and the feature using it may not work.",
                        table.Id,
                        name,
                        table.Name);
                }

                continue;
            }

            _context.GameobjectLoots.Add(ToEntity(table));
            _logger.LogInformation("Seeded game-object loot table {Id} '{Name}'.", table.Id, table.Name);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static GameobjectLoot ToEntity(GameObjectLootSeedTable table) =>
        new()
        {
            Id = table.Id,
            Name = table.Name,
            MaximumLootCount = 1,
            RandomizeLootCount = 0,
            GameobjectLootItems = table.Items
                .Select(item => new GameobjectLootItem
                {
                    ItemId = item.ItemId, MinimumCount = item.Minimum, MaximumCount = item.Maximum, Probability = item.Weight
                })
                .ToList()
        };
}
