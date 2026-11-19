namespace Hagalaz.Database.Migrations.Seeding;

/// <summary>
///     The Barrows chest loot tables (game-object loot tables 36-50) that <c>BarrowsRewards</c> rolls: one armour table per
///     standard brother and one table per ordinary reward band. Table names must not contain "rare": the loot store adds the
///     ring of wealth modifier to such tables.
/// </summary>
public sealed class BarrowsLootSeedSource : IGameObjectLootSeedSource
{
    public IReadOnlyList<GameObjectLootSeedTable> Tables { get; } =
    [
        Armour(36, "ahrim", 4708, 4710, 4712, 4714),
        Armour(37, "dharok", 4716, 4718, 4720, 4722),
        Armour(38, "guthan", 4724, 4726, 4728, 4730),
        Armour(39, "karil", 4732, 4734, 4736, 4738),
        Armour(40, "torag", 4745, 4747, 4749, 4751),
        Armour(41, "verac", 4753, 4755, 4757, 4759),

        Reward(42, "coins", 995, 1, 3931),
        Reward(43, "mind", 558, 250, 4900),
        Reward(44, "chaos", 562, 115, 1890),
        Reward(45, "death", 560, 70, 1190),
        Reward(46, "blood", 565, 35, 630),
        Reward(47, "bolts", 4740, 35, 280),
        Reward(48, "loop_half", 985, 1, 1),
        Reward(49, "tooth_half", 987, 1, 1),
        Reward(50, "dragon_med", 1149, 1, 1)
    ];

    private static GameObjectLootSeedTable Armour(ushort id, string brother, params ushort[] pieces) =>
        new(id, $"barrows_set_{brother}", pieces.Select(piece => new GameObjectLootSeedItem(piece, 1, 1, 25m)).ToArray());

    private static GameObjectLootSeedTable Reward(ushort id, string name, ushort itemId, uint minimum, uint maximum) =>
        new(id, $"barrows_reward_{name}", [new GameObjectLootSeedItem(itemId, minimum, maximum, 100m)]);
}
