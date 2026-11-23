using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Database.Migrations.Seeding;

/// <summary>
///     Seeds the farming seed and patch definitions supplied by the registered sources. A definition is only created when
///     its id is absent. An existing seed definition is never changed, except that a product experience of 0 (the value
///     rows get when the column is added) is backfilled from the source.
/// </summary>
public sealed class FarmingSeedSeeder : IGameWorldSeeder
{
    private readonly HagalazDbContext _context;
    private readonly IEnumerable<IFarmingSeedSource> _sources;
    private readonly ILogger<FarmingSeedSeeder> _logger;

    public FarmingSeedSeeder(HagalazDbContext context, IEnumerable<IFarmingSeedSource> sources, ILogger<FarmingSeedSeeder> logger)
    {
        _context = context;
        _sources = sources;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var seeds = _sources.SelectMany(source => source.Seeds).ToList();
        var ids = seeds.Select(seed => seed.ItemId).ToArray();
        var existing = await _context.SkillsFarmingSeedDefinitions
            .Where(seed => ids.Contains(seed.ItemId))
            .ToDictionaryAsync(seed => seed.ItemId, cancellationToken);

        foreach (var seed in seeds)
        {
            if (!existing.TryGetValue(seed.ItemId, out var definition))
            {
                _context.SkillsFarmingSeedDefinitions.Add(ToEntity(seed));
                _logger.LogInformation("Seeded farming seed definition {ItemId} ({Type}).", seed.ItemId, seed.Type);
                continue;
            }

            // Upgrade path: rows created before product_experience existed carry 0. Only that default is filled in;
            // any non-zero (custom) value and every other column stays as the operator left it.
            if (definition.ProductExperience == 0m && seed.ProductExperience > 0m)
            {
                definition.ProductExperience = seed.ProductExperience;
                _logger.LogInformation("Backfilled product experience of farming seed definition {ItemId}.", seed.ItemId);
            }
        }

        var patches = _sources.SelectMany(source => source.Patches).DistinctBy(patch => patch.ObjectId).ToList();
        var patchIds = patches.Select(patch => patch.ObjectId).ToArray();
        var existingPatches = (await _context.SkillsFarmingPatchDefinitions
            .AsNoTracking()
            .Where(patch => patchIds.Contains(patch.ObjectId))
            .Select(patch => patch.ObjectId)
            .ToListAsync(cancellationToken)).ToHashSet();

        foreach (var patch in patches.Where(patch => !existingPatches.Contains(patch.ObjectId)))
        {
            _context.SkillsFarmingPatchDefinitions.Add(new SkillsFarmingPatchDefinition { ObjectId = patch.ObjectId, Type = patch.Type });
            _logger.LogInformation("Seeded farming patch definition {ObjectId} ({Type}).", patch.ObjectId, patch.Type);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static SkillsFarmingSeedDefinition ToEntity(FarmingSeedSeed seed) =>
        new()
        {
            ItemId = seed.ItemId,
            ProductId = seed.ProductId,
            MinimumProductCount = seed.MinimumProductCount,
            MaximumProductCount = seed.MaximumProductCount,
            RequiredLevel = seed.RequiredLevel,
            PlantingExperience = seed.PlantingExperience,
            HarvestExperience = seed.HarvestExperience,
            ProductExperience = seed.ProductExperience,
            ProductRegrowTicks = seed.ProductRegrowTicks,
            VarpbitIndex = seed.VarpBitIndex,
            MaxCycles = seed.MaxCycles,
            CycleTicks = seed.CycleTicks,
            Type = seed.Type
        };
}
