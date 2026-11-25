using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Scripts.Skills.Herblore.Herbs;
using Hagalaz.Game.Scripts.Skills.Herblore.Potions;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Skills.Herblore;

/// <summary>
///     Pins which item ids the Herblore factories register from the standard potion definitions, so moving the extreme potions' herbs into
///     the definitions does not displace the scripts those items already had.
/// </summary>
[TestClass]
public sealed class HerbloreItemScriptFactoryTests
{
    private static readonly PotionDto[] _potions =
    [
        Potion(145, 101, [259], [221]), // Super attack
        Potion(3018, 103, [261], [2970]), // Super energy: avantoe is a primary ingredient
        Potion(15309, 145, [], [261]), // Extreme attack: avantoe on super attack
        Potion(15325, 169, [], [12539], 5) // Extreme ranging: five grenwall spikes on ranging potion
    ];

    [TestMethod]
    public async Task PrimaryFactory_RegistersEveryPrimaryIngredient()
    {
        var scripts = await GetScripts(service => new PrimaryIngredientItemScriptFactory(service));

        CollectionAssert.AreEquivalent(new[] { 259, 261 }, scripts.Select(s => s.itemId).ToArray());
        Assert.IsTrue(scripts.All(s => s.scriptType == typeof(PrimaryIngredientItemScript)));
    }

    [TestMethod]
    public async Task SecondaryFactory_SkipsIngredientsThatAreAlsoPrimary()
    {
        var scripts = await GetScripts(service => new SecondaryIngredientItemScriptFactory(service));

        CollectionAssert.AreEquivalent(new[] { 221, 2970, 12539 }, scripts.Select(s => s.itemId).ToArray());
        Assert.IsTrue(scripts.All(s => s.scriptType == typeof(SecondaryIngredientItemScript)));
    }

    [TestMethod]
    public async Task UnfinishedFactory_SkipsFinishedPotionsThatAreTheBaseOfAnotherPotion()
    {
        var scripts = await GetScripts(service => new UnfinishedPotionFactory(service));

        CollectionAssert.AreEquivalent(new[] { 101, 103 }, scripts.Select(s => s.itemId).ToArray());
        Assert.IsTrue(scripts.All(s => s.scriptType == typeof(UnfinishedPotion)));
    }

    private static async Task<List<(int itemId, Type scriptType)>> GetScripts(Func<IHerbloreService, IItemScriptFactory> create)
    {
        var service = Substitute.For<IHerbloreService>();
        service.FindPotions(PotionKind.Standard).Returns(_potions);
        var scripts = new List<(int itemId, Type scriptType)>();
        await foreach (var script in create(service).GetScripts()) scripts.Add(script);
        return scripts;
    }

    private static PotionDto Potion(int potionId, int unfinishedPotionId, int[] primary, int[] secondary, int secondaryCount = 1) =>
        new()
        {
            PotionId = potionId, Kind = PotionKind.Standard, UnfinishedPotionId = unfinishedPotionId, RequiredLevel = 1, Experience = 1,
            PrimaryItemIds = primary, SecondaryItemIds = secondary, SecondaryItemCount = secondaryCount
        };
}
