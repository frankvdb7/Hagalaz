using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Scripts.Tests.Skills.Fletching;

/// <summary>
///     A representative recipe of every Fletching category, with the values the scripts used to hard-code, served through an in-memory <see cref="IFletchingService" />.
/// </summary>
internal static class TestFletchingRecipes
{
    public static readonly FletchingDto Wood = Create(FletchingCategory.Wood, 1511, 946, [52, 50, 48, 9440], [15, 1, 1, 1], [0.33, 5.0, 10.0, 6.0], [1, 5, 10, 9], 6702);
    public static readonly FletchingDto Bow = Create(FletchingCategory.Bow, 50, 1777, [841], [1], [5.0], [5], 6678);
    public static readonly FletchingDto CrossbowUnstrung = Create(FletchingCategory.Bow, 9440, 9420, [9454], [1], [6.0], [9], -1);
    public static readonly FletchingDto Ammo = Create(FletchingCategory.Ammo, 52, 314, [53], [15], [1.0], [1], -1);
    public static readonly FletchingDto Dart = Create(FletchingCategory.Ammo, 819, 314, [806], [10], [18.0], [1], -1);
    public static readonly FletchingDto Bolt = Create(FletchingCategory.Ammo, 9375, 314, [877], [10], [0.5], [9], -1);
    public static readonly FletchingDto Grapple = Create(FletchingCategory.Ammo, 9416, 954, [9418], [1], [59.0], [59], -1);
    public static readonly FletchingDto Tips = Create(FletchingCategory.Tips, 1609, 1755, [45], [12], [1.5], [11], -1);

    public static readonly IFletchingService Service = new InMemoryFletchingService([Wood, Bow, CrossbowUnstrung, Ammo, Dart, Bolt, Grapple, Tips]);

    public static FletchingDto Create(FletchingCategory category, int resourceId, int toolId, int[] productIds, int[] counts, double[] experiences, int[] levels, int animationId) =>
        new()
        {
            ResourceId = resourceId,
            ToolId = toolId,
            Category = category,
            ProductIds = productIds,
            ProductCounts = counts,
            ProductExperiences = experiences,
            RequiredLevels = levels,
            AnimationId = animationId
        };

    private sealed class InMemoryFletchingService(IReadOnlyList<FletchingDto> recipes) : IFletchingService
    {
        public FletchingDto? FindRecipe(FletchingCategory category, int itemId, int otherItemId) =>
            recipes.FirstOrDefault(recipe => recipe.Category == category &&
                                             ((recipe.ResourceId == itemId && recipe.ToolId == otherItemId) || (recipe.ResourceId == otherItemId && recipe.ToolId == itemId)));

        public IReadOnlyList<FletchingDto> FindRecipes(FletchingCategory category) => recipes.Where(recipe => recipe.Category == category).ToArray();
    }
}
