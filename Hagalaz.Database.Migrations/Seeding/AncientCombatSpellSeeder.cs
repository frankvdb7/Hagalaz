using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Database.Migrations.Seeding;

/// <summary>
///     Seeds the Ice, Blood, Smoke, Shadow and Miasmic Ancient combat spells: the runes, level, experience, damage and autocast config the spell
///     scripts used to hard-code. Each family occupies four consecutive Ancient spellbook buttons laid out as rush, blitz, burst, barrage.
///     Animation, graphic and projectile ids stay in the spell scripts, so those columns keep their <c>-1</c> default. A definition is only
///     created when its key is absent; an existing row is never changed.
/// </summary>
public sealed class AncientCombatSpellSeeder : IGameWorldSeeder
{
    public const string AncientBook = "AncientBook";

    private const int Air = 556, Water = 555, Earth = 557, Fire = 554, Chaos = 562, Death = 560, Blood = 565, Soul = 566;

    // Button offset within a family for spell types 0 = rush, 1 = burst, 2 = blitz, 3 = barrage.
    private static readonly int[] _buttonOffsetByType = [0, 2, 1, 3];

    private sealed record Family(
        ushort FirstButtonId,
        int[][] Runes,
        int[][] RuneCounts,
        byte[] Levels,
        uint[] AutocastConfigs,
        decimal[] Experience,
        uint[] BaseDamage);

    private static readonly Family[] _families =
    [
        // Ice
        new(20, [[Chaos, Death, Water], [Chaos, Death, Water], [Death, Blood, Water], [Death, Blood, Water]],
            [[2, 2, 2], [4, 2, 4], [2, 2, 3], [4, 2, 6]], [58, 70, 82, 94], [69, 77, 85, 93], [34m, 40m, 46m, 52m], [180, 220, 260, 300]),
        // Blood
        new(24, [[Chaos, Death, Blood], [Chaos, Death, Blood], [Death, Blood], [Death, Blood, Soul]],
            [[2, 2, 1], [4, 2, 2], [2, 4], [4, 2, 1]], [56, 68, 80, 92], [67, 75, 83, 91], [33m, 39m, 45m, 51m], [150, 210, 250, 290]),
        // Smoke
        new(28, [[Chaos, Death, Fire, Air], [Chaos, Death, Fire, Air], [Death, Blood, Fire, Air], [Death, Blood, Fire, Air]],
            [[2, 2, 1, 1], [4, 2, 2, 2], [2, 2, 2, 2], [4, 2, 4, 4]], [50, 62, 74, 86], [63, 71, 79, 87], [30m, 36m, 42m, 48m], [130, 170, 264, 270]),
        // Shadow
        new(32, [[Chaos, Death, Air, Soul], [Chaos, Death, Air, Soul], [Death, Blood, Air, Soul], [Death, Blood, Air, Soul]],
            [[2, 2, 1, 1], [4, 2, 1, 2], [2, 2, 2, 2], [4, 2, 4, 3]], [52, 64, 76, 88], [65, 73, 81, 89], [31m, 37m, 43m, 49m], [140, 180, 240, 280]),
        // Miasmic
        new(36, [[Chaos, Earth, Soul], [Chaos, Earth, Soul], [Blood, Earth, Soul], [Blood, Earth, Soul]],
            [[2, 1, 1], [4, 2, 2], [2, 3, 3], [4, 4, 4]], [61, 73, 85, 97], [95, 97, 99, 101], [36m, 42m, 48m, 54m], [180, 240, 280, 352])
    ];

    private readonly HagalazDbContext _context;
    private readonly ILogger<AncientCombatSpellSeeder> _logger;

    public AncientCombatSpellSeeder(HagalazDbContext context, ILogger<AncientCombatSpellSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existing = (await _context.SkillsMagicCombatDefinitions
            .AsNoTracking()
            .Where(definition => definition.SpellBook == AncientBook)
            .Select(definition => definition.ButtonId)
            .ToListAsync(cancellationToken)).ToHashSet();

        foreach (var definition in CreateDefinitions().Where(definition => !existing.Contains(definition.ButtonId)))
        {
            _context.SkillsMagicCombatDefinitions.Add(definition);
            _logger.LogInformation("Seeded Ancient combat spell definition {ButtonId}.", definition.ButtonId);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    ///     The 20 Ancient combat spell definitions with the values the spell scripts used to hard-code.
    /// </summary>
    public static IEnumerable<SkillsMagicCombatDefinition> CreateDefinitions()
    {
        foreach (var family in _families)
        {
            for (var type = 0; type < 4; type++)
            {
                yield return new SkillsMagicCombatDefinition
                {
                    ButtonId = (ushort)(family.FirstButtonId + _buttonOffsetByType[type]),
                    SpellBook = AncientBook,
                    BaseDamage = family.BaseDamage[type],
                    RequiredLevel = family.Levels[type],
                    BaseExperience = family.Experience[type],
                    RequiredRunes = string.Join(',', family.Runes[type]),
                    RequiredRunesCounts = string.Join(',', family.RuneCounts[type]),
                    CastAnimationId = -1,
                    CastGraphicId = -1,
                    EndGraphicId = -1,
                    ProjectileId = -1,
                    AutocastConfig = family.AutocastConfigs[type]
                };
            }
        }
    }
}
