using System.Globalization;
using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Database.Migrations.Seeding;

/// <summary>
///     Seeds every Herblore potion recipe the Herblore scripts used to hard-code, values verbatim: the potion catalogue, the extreme
///     potions' herb or ingredient (five grenwall spikes for extreme ranging) and the overload recipe. A recipe is only created when its
///     potion id is absent; an existing row is never changed.
/// </summary>
public sealed class HerblorePotionDefinitionSeeder : IGameWorldSeeder
{
    private const byte Standard = 0;
    private const byte Overload = 1;

    private sealed record Recipe(ushort PotionId, ushort UnfinishedPotionId, byte RequiredLevel, decimal Experience, int[] PrimaryItemIds, int[] SecondaryItemIds, byte Kind = Standard, byte SecondaryItemCount = 1);

    private static readonly Recipe[] _recipes =
    [
        new(121, 91, 1, 25m, [249], [221]), // Attack potion
        new(175, 93, 5, 37.5m, [251], [235]), // Antipoison
        new(4844, 4840, 8, 40m, [1534], [1526]), // Relicym's balm
        new(115, 95, 12, 50m, [253], [225]), // Strength potion
        new(127, 97, 22, 62.5m, [255], [223]), // Restore potion
        new(3010, 97, 26, 67.5m, [253], [1975]), // Energy potion
        new(133, 93, 30, 75m, [257], [239]), // Defence potion
        new(139, 99, 38, 87.5m, [257], [231]), // Prayer potion
        new(12142, 12181, 40, 92m, [12172], [12109]), // Summoning potion
        new(145, 101, 45, 100m, [259], [221]), // Super attack potion
        new(181, 101, 48, 106.3m, [259], [235]), // Super antipoison
        new(3018, 103, 52, 117.5m, [261], [2970]), // Super energy
        new(157, 105, 55, 125m, [263], [225]), // Super strength potion
        new(187, 105, 60, 137.5m, [263], [241]), // Weapon poison
        new(3026, 3004, 63, 142.5m, [3000], [223]), // Super restore
        new(163, 107, 66, 150m, [265], [239]), // Super defence
        new(2454, 2483, 68, 155m, [2481], [241]), // Antifire
        new(169, 109, 72, 162.5m, [267], [245]), // Ranging potion
        new(3042, 2483, 76, 172.5m, [2481], [3138]), // Magic potion
        new(189, 111, 78, 175m, [269], [247]), // Zamorak brew
        new(6687, 3002, 81, 180m, [2998], [6693]), // Saradomin brew
        new(15301, 3018, 84, 200m, [], [5972]), // Recover special
        new(15305, 2454, 85, 210m, [], [4621]), // Super antifire
        new(15309, 145, 88, 220m, [], [261]), // Extreme attack: avantoe
        new(15313, 157, 89, 230m, [], [267]), // Extreme strength: dwarf weed
        new(15317, 163, 90, 240m, [], [2481]), // Extreme defence: lantadyme
        new(15321, 3042, 91, 250m, [], [9594]), // Extreme magic: ground mud runes
        new(15325, 169, 92, 260m, [], [12539], SecondaryItemCount: 5), // Extreme ranging: five grenwall spikes
        new(15329, 139, 94, 270m, [], [6810]), // Super prayer
        new(21632, 21628, 94, 190m, [21624], [21622]), // Prayer renewal
        new(15333, 269, 96, 1000m, [], [15309, 15313, 15317, 15321, 15325], Overload), // Overload: torstol and every extreme (3) potion
    ];

    private readonly HagalazDbContext _context;
    private readonly ILogger<HerblorePotionDefinitionSeeder> _logger;

    public HerblorePotionDefinitionSeeder(HagalazDbContext context, ILogger<HerblorePotionDefinitionSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existing = await _context.SkillsHerblorePotionDefinitions.Select(definition => definition.PotionId).ToHashSetAsync(cancellationToken);
        foreach (var recipe in _recipes.Where(recipe => !existing.Contains(recipe.PotionId)))
        {
            _context.SkillsHerblorePotionDefinitions.Add(new SkillsHerblorePotionDefinition
            {
                PotionId = recipe.PotionId,
                Kind = recipe.Kind,
                UnfinishedPotionId = recipe.UnfinishedPotionId,
                RequiredLevel = recipe.RequiredLevel,
                Experience = recipe.Experience,
                PrimaryItemIds = Join(recipe.PrimaryItemIds),
                SecondaryItemIds = Join(recipe.SecondaryItemIds),
                SecondaryItemCount = recipe.SecondaryItemCount
            });
            _logger.LogInformation("Seeded herblore potion definition {PotionId}.", recipe.PotionId);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string Join(IEnumerable<int> values) => string.Join(',', values.Select(value => value.ToString(CultureInfo.InvariantCulture)));
}
