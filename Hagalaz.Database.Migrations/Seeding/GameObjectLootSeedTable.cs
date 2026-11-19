namespace Hagalaz.Database.Migrations.Seeding;

/// <summary>
///     One game-object loot table to seed: its id, name and item rows.
/// </summary>
public sealed record GameObjectLootSeedTable(ushort Id, string Name, IReadOnlyList<GameObjectLootSeedItem> Items);

/// <summary>
///     One item row of a seeded loot table. Every seeded table drops at most one item per roll, without randomized counts.
/// </summary>
public sealed record GameObjectLootSeedItem(ushort ItemId, uint Minimum, uint Maximum, decimal Weight);
