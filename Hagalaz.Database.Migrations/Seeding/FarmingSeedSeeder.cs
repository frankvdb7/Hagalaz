using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Database.Migrations.Seeding;

/// <summary>
///     Seeds the farming seed definitions supplied by the registered sources. A seed is only created when its item id is
///     absent: an existing definition, however it was edited, is never changed.
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
        var existing = (await _context.SkillsFarmingSeedDefinitions
            .AsNoTracking()
            .Where(seed => ids.Contains(seed.ItemId))
            .Select(seed => seed.ItemId)
            .ToListAsync(cancellationToken)).ToHashSet();

        foreach (var seed in seeds.Where(seed => !existing.Contains(seed.ItemId)))
        {
            _context.SkillsFarmingSeedDefinitions.Add(ToEntity(seed));
            _logger.LogInformation("Seeded farming seed definition {ItemId} ({Type}).", seed.ItemId, seed.Type);
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
