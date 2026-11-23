namespace Hagalaz.Database.Migrations.Seeding;

/// <summary>
///     Supplies farming seed definitions to seed. A feature adds its seeds by providing one source; the seeder itself
///     does not change.
/// </summary>
public interface IFarmingSeedSource
{
    IReadOnlyList<FarmingSeedSeed> Seeds { get; }

    /// <summary>
    ///     The farming patch objects the seeds are planted in, when the patch table does not know them yet.
    /// </summary>
    IReadOnlyList<FarmingPatchSeed> Patches => [];
}

/// <summary>
///     One farming patch object to seed.
/// </summary>
public sealed record FarmingPatchSeed(uint ObjectId, string Type);

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
