using Hagalaz.Game.Scripts.Skills.Fletching;

namespace Hagalaz.Game.Scripts.Tests.Skills.Fletching;

[TestClass]
public sealed class FletchingScriptFactoryTests
{
    [TestMethod]
    public async Task Factories_RegisterTheResourceOfEveryRecipeOfTheirCategoryOnly()
    {
        var service = TestFletchingRecipes.Service;

        CollectionAssert.AreEquivalent(
            new[] { (50, typeof(FletchingBow)), (9440, typeof(FletchingBow)) },
            await new FletchingBowScriptFactory(service).GetScripts().ToListAsync());
        CollectionAssert.AreEquivalent(
            new[] { (52, typeof(FletchingAmmo)), (819, typeof(FletchingAmmo)), (9375, typeof(FletchingAmmo)), (9416, typeof(FletchingAmmo)) },
            await new FletchingAmmoScriptFactory(service).GetScripts().ToListAsync());
        CollectionAssert.AreEquivalent(
            new[] { (1609, typeof(FletchingTips)) },
            await new FletchingTipScriptFactory(service).GetScripts().ToListAsync());
    }
}
