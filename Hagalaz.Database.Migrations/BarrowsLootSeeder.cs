using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Database.Migrations;

/// <summary>
///     Seeds the Barrows chest loot tables that <c>BarrowsRewards</c> rolls (game-object loot tables 36-50).
///     A table is only seeded when its id is absent: an existing table, however it was edited, is never changed,
///     and a table whose id is used under another name is reported and left alone.
/// </summary>
public sealed class BarrowsLootSeeder
{
    private const int CoinsMaximum = 3931;

    private readonly HagalazDbContext _context;
    private readonly ILogger<BarrowsLootSeeder> _logger;

    public BarrowsLootSeeder(HagalazDbContext context, ILogger<BarrowsLootSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    ///     One loot table: its id, name and (item id, minimum, maximum, weight) rows.
    /// </summary>
    public sealed record SeedTable(ushort Id, string Name, IReadOnlyList<SeedItem> Items);

    public sealed record SeedItem(ushort ItemId, uint Minimum, uint Maximum, decimal Weight);

    public static IReadOnlyList<SeedTable> Tables { get; } =
    [
        // One armour piece per roll for each standard brother, equal weight.
        Armour(36, "ahrim", 4708, 4710, 4712, 4714),
        Armour(37, "dharok", 4716, 4718, 4720, 4722),
        Armour(38, "guthan", 4724, 4726, 4728, 4730),
        Armour(39, "karil", 4732, 4734, 4736, 4738),
        Armour(40, "torag", 4745, 4747, 4749, 4751),
        Armour(41, "verac", 4753, 4755, 4757, 4759),

        // Ordinary reward bands, one item each. Names must not contain "rare": the loot store adds the ring of wealth
        // modifier to such tables.
        Reward(42, "coins", 995, 1, CoinsMaximum),
        Reward(43, "mind", 558, 250, 4900),
        Reward(44, "chaos", 562, 115, 1890),
        Reward(45, "death", 560, 70, 1190),
        Reward(46, "blood", 565, 35, 630),
        Reward(47, "bolts", 4740, 35, 280),
        Reward(48, "loop_half", 985, 1, 1),
        Reward(49, "tooth_half", 987, 1, 1),
        Reward(50, "dragon_med", 1149, 1, 1)
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var ids = Tables.Select(table => table.Id).ToArray();
        var existing = await _context.GameobjectLoots
            .AsNoTracking()
            .Where(loot => ids.Contains(loot.Id))
            .ToDictionaryAsync(loot => loot.Id, loot => loot.Name, cancellationToken);

        foreach (var table in Tables)
        {
            if (existing.TryGetValue(table.Id, out var name))
            {
                if (name != table.Name)
                {
                    _logger.LogWarning(
                        "Game-object loot table {Id} is '{Name}', not '{Expected}'; the Barrows loot table was not seeded and chests may not be claimable.",
                        table.Id,
                        name,
                        table.Name);
                }

                continue;
            }

            _context.GameobjectLoots.Add(new GameobjectLoot
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
            });
            _logger.LogInformation("Seeded game-object loot table {Id} '{Name}'.", table.Id, table.Name);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static SeedTable Armour(ushort id, string brother, params ushort[] pieces) =>
        new(id, $"barrows_set_{brother}", pieces.Select(piece => new SeedItem(piece, 1, 1, 25m)).ToArray());

    private static SeedTable Reward(ushort id, string name, ushort itemId, uint minimum, uint maximum) =>
        new(id, $"barrows_reward_{name}", [new SeedItem(itemId, minimum, maximum, 100m)]);
}
