using System.Globalization;
using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Database.Migrations.Seeding;

/// <summary>
///     Seeds every Fletching recipe (wood, bows and crossbows, ammunition, bolt tips) the Fletching scripts used to hard-code, values verbatim.
///     A recipe is only created when its resource id is absent. An existing row is never changed once it has a category; a row
///     without a category is a legacy row this seeder never wrote, so it is rewritten with the canonical recipe.
/// </summary>
public sealed class FletchingDefinitionSeeder : IGameWorldSeeder
{
    private const byte Wood = 1;
    private const byte Bow = 2;
    private const byte Ammo = 3;
    private const byte Tips = 4;

    private sealed record Recipe(byte Category, ushort ResourceId, ushort ToolId, int[] ProductIds, int[] ProductCounts, double[] ProductExperiences, int[] RequiredLevels, ushort? AnimationId);

    private static readonly Recipe[] _recipes =
    [
        new(Wood, 1511, 946, [52, 50, 48, 9440], [15, 1, 1, 1], [0.33, 5.0, 10.0, 6.0], [1, 5, 10, 9], 6702), // normal bow
        new(Wood, 1521, 946, [54, 56, 9442], [1, 1, 1], [16.5, 25.0, 16.0], [20, 25, 24], 6702), // oak bow
        new(Wood, 1519, 946, [60, 58, 9444], [1, 1, 1], [33.3, 41.5, 22.0], [35, 40, 39], 6702), // willow bow
        new(Wood, 1517, 946, [64, 62, 9448], [1, 1, 1], [50.0, 58.3, 32.0], [50, 55, 54], 6702), // maple bow
        new(Wood, 1515, 946, [68, 66, 9452], [1, 1, 1], [67.5, 75.0, 50.0], [65, 70, 69], 6702), // yew bow
        new(Wood, 1513, 946, [72, 70], [1, 1], [83.25, 91.5], [80, 85], 6702), // magic bow
        new(Bow, 50, 1777, [841], [1], [5.0], [5], 6678), // unstrung short bow
        new(Bow, 48, 1777, [839], [1], [10.0], [10], 6684), // unstrung long bow
        new(Bow, 54, 1777, [843], [1], [16.5], [20], 6679), // unstrung oak short bow
        new(Bow, 56, 1777, [845], [1], [25.0], [25], 6685), // unstrung oak long bow
        new(Bow, 60, 1777, [849], [1], [33.3], [35], 6680), // unstrung willow short bow
        new(Bow, 58, 1777, [847], [1], [41.5], [40], 6686), // unstrung willow long bow
        new(Bow, 64, 1777, [853], [1], [50.0], [50], 6681), // unstrung maple short bow
        new(Bow, 62, 1777, [851], [1], [58.3], [55], 6687), // unstrung maple long bow
        new(Bow, 68, 1777, [857], [1], [67.5], [65], 6682), // unstrung yew short bow
        new(Bow, 66, 1777, [855], [1], [75.0], [70], 6688), // unstrung yew long bow
        new(Bow, 72, 1777, [861], [1], [83.25], [80], 6683), // unstrung magic short bow
        new(Bow, 70, 1777, [859], [1], [91.5], [85], 6689), // unstrung magic long bow
        new(Bow, 9440, 9420, [9454], [1], [6.0], [9], null), // bronze cbow (u)
        new(Bow, 9442, 9422, [9456], [1], [16.0], [24], null), // blurite cbow (u)
        new(Bow, 9444, 9423, [9457], [1], [22.0], [39], null), // iron cbow (u)
        new(Bow, 9446, 9425, [9459], [1], [27.0], [46], null), // steel cbow (u)
        new(Bow, 9448, 9427, [9461], [1], [32.0], [54], null), // mithril cbow (u)
        new(Bow, 9450, 9429, [9463], [1], [41.0], [61], null), // adamant cbow (u)
        new(Bow, 9452, 9431, [9465], [1], [50.0], [69], null), // rune cbow (u)
        new(Bow, 9454, 9438, [9174], [1], [6.0], [9], 6671), // bronze cbow
        new(Bow, 9456, 9438, [9176], [1], [16.0], [24], 6672), // blurite cbow
        new(Bow, 9457, 9438, [9177], [1], [22.0], [39], 6673), // iron cbow
        new(Bow, 9459, 9438, [9179], [1], [27.0], [46], 6674), // steel cbow
        new(Bow, 9461, 9438, [9181], [1], [32.0], [54], 6675), // mithril cbow
        new(Bow, 9463, 9438, [9183], [1], [41.0], [61], 6676), // adamant cbow
        new(Bow, 9465, 9438, [9185], [1], [50.0], [69], 6677), // rune cbow
        new(Ammo, 52, 314, [53], [15], [1.0], [1], null), // headless arrows
        new(Ammo, 39, 53, [882], [15], [2.6], [1], null), // bronze arrows
        new(Ammo, 40, 53, [884], [15], [3.8], [15], null), // iron arrows
        new(Ammo, 41, 53, [886], [15], [6.3], [30], null), // steel arrows
        new(Ammo, 42, 53, [888], [15], [8.8], [45], null), // mithril arrows
        new(Ammo, 43, 53, [890], [15], [10.0], [60], null), // adamant arrows
        new(Ammo, 44, 53, [892], [15], [13.8], [75], null), // rune arrows
        new(Ammo, 11237, 53, [11212], [15], [16.3], [90], null), // dragon arrows
        new(Ammo, 9375, 314, [877], [10], [0.5], [9], null), // bronze bolts
        new(Ammo, 9376, 314, [9139], [10], [1.0], [24], null), // blurite bolts
        new(Ammo, 9337, 314, [9140], [10], [1.5], [39], null), // iron bolts
        new(Ammo, 9382, 314, [9145], [10], [2.5], [43], null), // silver bolts
        new(Ammo, 9339, 314, [9141], [10], [3.5], [46], null), // steel bolts
        new(Ammo, 3979, 314, [9142], [10], [5.0], [54], null), // mithril bolts
        new(Ammo, 9380, 314, [9143], [10], [7.0], [61], null), // adamant bolts
        new(Ammo, 9381, 314, [9144], [10], [10.0], [69], null), // rune bolts
        new(Ammo, 45, 877, [879], [10], [1.6], [11], null), // opal bolts
        new(Ammo, 9187, 9376, [9335], [10], [2.4], [26], null), // jade bolts
        new(Ammo, 46, 9140, [880], [10], [3.2], [41], null), // pearl bolts
        new(Ammo, 9188, 9141, [9336], [10], [3.9], [48], null), // red topaz bolts
        new(Ammo, 47, 877, [881], [10], [9.5], [51], null), // barbed bolts
        new(Ammo, 9189, 9142, [9337], [10], [4.7], [56], null), // sapphire bolts
        new(Ammo, 9190, 9376, [9335], [10], [5.5], [58], null), // emerald bolts
        new(Ammo, 9191, 9143, [9339], [10], [6.3], [63], null), // ruby bolts
        new(Ammo, 9192, 9143, [9340], [10], [7.0], [65], null), // diamond bolts
        new(Ammo, 9193, 9144, [9341], [10], [8.2], [71], null), // dragon bolts
        new(Ammo, 9194, 9144, [9342], [10], [9.4], [73], null), // onyx bolts
        new(Ammo, 819, 314, [806], [10], [18.0], [1], null), // bronze darts
        new(Ammo, 820, 314, [807], [10], [32.0], [22], null), // iron darts
        new(Ammo, 821, 314, [808], [10], [75.0], [37], null), // steel darts
        new(Ammo, 822, 314, [809], [10], [112.0], [52], null), // mithril darts
        new(Ammo, 823, 314, [810], [10], [150.0], [67], null), // adamant darts
        new(Ammo, 824, 314, [811], [10], [188.0], [81], null), // rune darts
        new(Ammo, 11232, 314, [11230], [10], [250.0], [95], null), // dragon darts
        new(Ammo, 9416, 954, [9418], [1], [59.0], [59], null), // mithril grapple
        new(Tips, 1609, 1755, [45], [12], [1.5], [11], null),
        new(Tips, 1611, 1755, [9187], [12], [2.4], [26], null), // jade bolt tips
        new(Tips, 413, 1755, [46], [24], [3.2], [41], null), // 24 pearl bolt tips
        new(Tips, 411, 1755, [46], [6], [3.2], [41], null), // 6 pearl bolt tips
        new(Tips, 1613, 1755, [9188], [12], [3.9], [48], null), // topaz bolt tips
        new(Tips, 1607, 1755, [9189], [12], [4.7], [56], null), // sapphire bolt tips
        new(Tips, 1605, 1755, [9190], [12], [5.5], [58], null), // emerald bolt tips
        new(Tips, 1603, 1755, [9191], [12], [6.3], [63], null), // ruby bolt tips
        new(Tips, 1601, 1755, [9192], [12], [7.0], [65], null), // diamond bolt tips
        new(Tips, 1615, 1755, [9193], [12], [8.2], [71], null), // dragon bolt tips
        new(Tips, 6573, 1755, [9194], [24], [9.4], [73], null), // onyx bolt tips
    ];

    private readonly HagalazDbContext _context;
    private readonly ILogger<FletchingDefinitionSeeder> _logger;

    public FletchingDefinitionSeeder(HagalazDbContext context, ILogger<FletchingDefinitionSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existing = await _context.SkillsFletchingDefinitions.ToDictionaryAsync(definition => definition.ResourceId, cancellationToken);
        foreach (var recipe in _recipes)
        {
            if (!existing.TryGetValue(recipe.ResourceId, out var definition))
            {
                definition = new SkillsFletchingDefinition { ResourceId = recipe.ResourceId };
                _context.SkillsFletchingDefinitions.Add(definition);
                _logger.LogInformation("Seeded fletching definition {ResourceId}.", recipe.ResourceId);
            }
            else if (definition.Category != 0)
            {
                continue;
            }

            definition.ToolId = recipe.ToolId;
            definition.Category = recipe.Category;
            definition.ProductIds = Join(recipe.ProductIds);
            definition.ProductCounts = Join(recipe.ProductCounts);
            definition.ProductExperiences = Join(recipe.ProductExperiences);
            definition.RequiredLevels = Join(recipe.RequiredLevels);
            definition.AnimationId = recipe.AnimationId;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string Join<T>(IEnumerable<T> values) where T : IFormattable => string.Join(',', values.Select(value => value.ToString(null, CultureInfo.InvariantCulture)));
}
