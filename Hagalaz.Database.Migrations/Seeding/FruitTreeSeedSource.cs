namespace Hagalaz.Database.Migrations.Seeding;

/// <summary>
///     The seven fruit tree seeds planted in fruit tree patches. Seed and product item ids come from the 742 item
///     definitions. <c>VarpBitIndex</c> is the species slot in the layout of patch object 7965 in the pinned 742 cache
///     (6 growth stages and up to 6 fruit per species). XP values are from the 2012 Fruit tree reference; the 16000 tick
///     growth cycle (160 minutes) and 6000 tick regrow interval come from the Farming reference and are not cache
///     verified. See docs/revision-742/data/researched/farming-fruit-trees.csv.
/// </summary>
public sealed class FruitTreeSeedSource : IFarmingSeedSource
{
    private const uint _cycleTicks = 16000;
    private const uint _regrowTicks = 6000;
    private const uint _fruitCount = 6;
    private const byte _growthCycles = 6;

    public IReadOnlyList<FarmingSeedSeed> Seeds { get; } =
    [
        Create(5283, 1955, 27, 22m, 1199.5m, 8.5m, 0), // apple (cooking apple)
        Create(5284, 1963, 33, 28m, 1750.5m, 10.5m, 1), // banana
        Create(5285, 2108, 39, 35m, 2470m, 13.5m, 2), // orange
        Create(5286, 5970, 42, 40m, 2907m, 15m, 3), // curry leaf
        Create(5287, 2114, 51, 57m, 4605.7m, 21.5m, 4), // pineapple
        Create(5288, 5972, 57, 72m, 6146.4m, 27m, 5), // papaya fruit
        Create(5289, 5974, 68, 110.5m, 10150.1m, 41.5m, 6) // coconut
    ];

    private static FarmingSeedSeed Create(ushort seedId, ushort productId, byte level, decimal plantingXp, decimal healthCheckXp, decimal fruitXp, sbyte slot) =>
        new(seedId, productId, _fruitCount, _fruitCount, level, plantingXp, healthCheckXp, fruitXp, _regrowTicks, slot, _growthCycles, _cycleTicks, "FruitTree");
}
