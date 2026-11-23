namespace Hagalaz.Database.Migrations.Seeding;

/// <summary>
///     Supplies farming seed definitions to seed. A feature adds its seeds by providing one source; the seeder itself
///     does not change.
/// </summary>
public interface IFarmingSeedSource
{
    IReadOnlyList<FarmingSeedSeed> Seeds { get; }
}

/// <summary>
///     One farming seed definition to seed. <see cref="VarpBitIndex" /> is interpreted per patch type by the game world.
/// </summary>
public sealed record FarmingSeedSeed(
    ushort ItemId,
    ushort ProductId,
    uint MinimumProductCount,
    uint MaximumProductCount,
    byte RequiredLevel,
    decimal PlantingExperience,
    decimal HarvestExperience,
    decimal ProductExperience,
    uint ProductRegrowTicks,
    sbyte VarpBitIndex,
    byte MaxCycles,
    uint CycleTicks,
    string Type);
