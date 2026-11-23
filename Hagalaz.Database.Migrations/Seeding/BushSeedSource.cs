namespace Hagalaz.Database.Migrations.Seeding;

/// <summary>
///     The six bush seeds and the four bush patch objects (7577-7580). Seed and product item ids come from the 742 item
///     definitions. <c>VarpBitIndex</c> is the species slot in the layout of the bush patch objects in the pinned 742
///     cache, which also gives the growth stage counts (5-8, equal to the 20 minute stages of the Seeds reference) and
///     the 4 berries per bush. Levels and XP are from the 2012 Seeds reference. The growth cycle is 2000 ticks (20
///     minutes) per that reference. The reference gives no regrow time for farmed bushes, so the regrow interval is left
///     at 0, which uses the growth cycle. See docs/revision-742/data/researched/farming-bushes.csv.
/// </summary>
public sealed class BushSeedSource : IFarmingSeedSource
{
    private const uint _cycleTicks = 2000;
    private const uint _berryCount = 4;

    public IReadOnlyList<FarmingSeedSeed> Seeds { get; } =
    [
        Create(5101, 1951, 10, 11.5m, 64m, 4.5m, 0, 5), // redberry
        Create(5102, 753, 22, 18m, 102.5m, 7m, 1, 6), // cadavaberry
        Create(5103, 2126, 36, 31.5m, 177.5m, 12m, 2, 7), // dwellberry
        Create(5104, 247, 48, 50.5m, 284.5m, 19m, 3, 8), // jangerberry
        Create(5105, 239, 59, 78m, 437.5m, 29m, 4, 8), // whiteberry
        Create(5106, 6018, 70, 120m, 675m, 45m, 5, 8) // poison ivy
    ];

    public IReadOnlyList<FarmingPatchSeed> Patches { get; } =
    [
        new(7577, "Bush"),
        new(7578, "Bush"),
        new(7579, "Bush"),
        new(7580, "Bush")
    ];

    private static FarmingSeedSeed Create(ushort seedId, ushort productId, byte level, decimal plantingXp, decimal healthCheckXp, decimal berryXp, sbyte slot, byte growthCycles) =>
        new(seedId, productId, _berryCount, _berryCount, level, plantingXp, healthCheckXp, berryXp, 0, slot, growthCycles, _cycleTicks, "Bush");
}
