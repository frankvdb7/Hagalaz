using System.Reflection;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Scripts.Skills.Herblore.Herbs;
using Hagalaz.Game.Scripts.Skills.Herblore.Potions;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Skills.Herblore;

/// <summary>
///     Pins making each extreme potion from its seeded (base potion, ingredient) pair through the production path: item scripts
///     registered the way <c>ItemScriptProvider</c> does (metadata scripts first, then the Herblore factories, first registration wins)
///     and dispatched the way the inventory does (the used item's script, then the used-with item's script).
/// </summary>
[TestClass]
public sealed class ExtremePotionDispatchTests
{
    // Every seeded definition that involves the five extreme bases or their ingredients, values as in HerblorePotionDefinitionSeeder.
    private static readonly PotionDto[] _definitions =
    [
        Standard(145, 101, [259], [221]), // Super attack potion
        Standard(3018, 103, [261], [2970]), // Super energy: avantoe is also a primary ingredient
        Standard(157, 105, [263], [225]), // Super strength potion
        Standard(163, 107, [265], [239]), // Super defence
        Standard(2454, 2483, [2481], [241]), // Antifire: lantadyme is also a primary ingredient
        Standard(169, 109, [267], [245]), // Ranging potion: dwarf weed is also a primary ingredient
        Standard(3042, 2483, [2481], [3138]), // Magic potion
        Standard(15309, 145, [], [261]), // Extreme attack
        Standard(15313, 157, [], [267]), // Extreme strength
        Standard(15317, 163, [], [2481]), // Extreme defence
        Standard(15321, 3042, [], [9594]), // Extreme magic
        Standard(15325, 169, [], [12539], 5), // Extreme ranging
    ];

    private readonly IPotionSkillService _potions = Substitute.For<IPotionSkillService>();
    private readonly IHerbloreSkillService _herblore = Substitute.For<IHerbloreSkillService>();
    private readonly IHerbloreService _service = Substitute.For<IHerbloreService>();
    private readonly ICharacter _character = Substitute.For<ICharacter>();

    public ExtremePotionDispatchTests()
    {
        _service.FindPotions(PotionKind.Standard).Returns(_definitions);
        _service.FindPotionByPrimaryItemId(Arg.Any<int>())
            .Returns(call => _definitions.FirstOrDefault(p => p.PrimaryItemIds.Contains(call.Arg<int>())));
        _service.FindPotionBySecondaryItemId(PotionKind.Standard, Arg.Any<int>(), Arg.Any<int>())
            .Returns(call => _definitions.FirstOrDefault(p => p.UnfinishedPotionId == call.ArgAt<int>(1) && p.SecondaryItemIds.Contains(call.ArgAt<int>(2))));
        _herblore.MakePotion(default!, default!, default!, default!, default).ReturnsForAnyArgs(true);
    }

    [TestMethod]
    [DataRow(145, 261, 15309, typeof(SuperAttackPotion), typeof(PrimaryIngredientItemScript))]
    [DataRow(157, 267, 15313, typeof(SuperStrengthPotion), typeof(PrimaryIngredientItemScript))]
    [DataRow(163, 2481, 15317, typeof(SuperDefencePotion), typeof(PrimaryIngredientItemScript))]
    [DataRow(3042, 9594, 15321, typeof(MagicPotion), typeof(SecondaryIngredientItemScript))]
    [DataRow(169, 12539, 15325, typeof(RangingPotion), typeof(SecondaryIngredientItemScript))]
    public async Task SeededPair_IsRegisteredAndMakesItsExtremePotionInEitherOrder(int baseId, int ingredientId, int productId, Type baseScript, Type ingredientScript)
    {
        var scripts = await RegisterScripts();
        Assert.IsInstanceOfType(scripts[baseId], baseScript);
        Assert.IsInstanceOfType(scripts[ingredientId], ingredientScript);
        var basePotion = Item(baseId);
        var ingredient = Item(ingredientId);

        Assert.IsTrue(Dispatch(scripts, ingredient, basePotion));
        Assert.IsTrue(Dispatch(scripts, basePotion, ingredient));

        // the base potion is always passed as the vial and the ingredient as the ingredient
        _herblore.Received(2).MakePotion(_character, basePotion, ingredient, Arg.Is<PotionDto>(p => p.PotionId == productId), false);
        _herblore.ReceivedWithAnyArgs(2).MakePotion(default!, default!, default!, default!, default);
        _potions.DidNotReceiveWithAnyArgs().CombinePotions(default!, default!, default!, default!);
    }

    /// <summary>Mirrors the inventory: the used item's script first, then the used-with item's script; unregistered items have no behaviour.</summary>
    private bool Dispatch(Dictionary<int, IItemScript> scripts, IItem used, IItem usedWith) =>
        (scripts.TryGetValue(used.Id, out var usedScript) && usedScript.UseItemOnItem(used, usedWith, _character)) ||
        (scripts.TryGetValue(usedWith.Id, out var usedWithScript) && usedWithScript.UseItemOnItem(used, usedWith, _character));

    private async Task<Dictionary<int, IItemScript>> RegisterScripts()
    {
        var scripts = new Dictionary<int, IItemScript>();
        IItemScript[] metadataScripts =
        [
            new SuperAttackPotion(_potions, _herblore, _service), new SuperStrengthPotion(_potions, _herblore, _service),
            new SuperDefencePotion(_potions, _herblore, _service), new MagicPotion(_potions, _herblore, _service),
            new RangingPotion(_potions, _herblore, _service)
        ];
        foreach (var script in metadataScripts)
        {
            foreach (var id in script.GetType().GetCustomAttribute<ItemScriptMetaDataAttribute>()!.ItemIds) scripts.TryAdd(id, script);
        }

        await Register(scripts, new PrimaryIngredientItemScriptFactory(_service), new PrimaryIngredientItemScript(_service, _herblore));
        await Register(scripts, new SecondaryIngredientItemScriptFactory(_service), new SecondaryIngredientItemScript(_service, _herblore));
        await Register(scripts, new UnfinishedPotionFactory(_service), new UnfinishedPotion(_potions));
        return scripts;
    }

    private static async Task Register(Dictionary<int, IItemScript> scripts, IItemScriptFactory factory, IItemScript script)
    {
        await foreach (var (itemId, scriptType) in factory.GetScripts())
        {
            Assert.AreEqual(script.GetType(), scriptType);
            scripts.TryAdd(itemId, script);
        }
    }

    private static IItem Item(int id)
    {
        var item = Substitute.For<IItem>();
        item.Id.Returns(id);
        return item;
    }

    private static PotionDto Standard(int potionId, int unfinishedPotionId, int[] primary, int[] secondary, int secondaryCount = 1) =>
        new()
        {
            PotionId = potionId, Kind = PotionKind.Standard, UnfinishedPotionId = unfinishedPotionId, RequiredLevel = 1, Experience = 1,
            PrimaryItemIds = primary, SecondaryItemIds = secondary, SecondaryItemCount = secondaryCount
        };
}
