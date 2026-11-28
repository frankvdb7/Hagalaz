using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Skills.Herblore.Potions;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Skills.Herblore;

/// <summary>
///     Pins the dose handling of the potions that are the base of an extreme potion (Super attack/strength/defence, Magic,
///     Ranging). Making the extreme potion from the seeded pairs is covered by <see cref="ExtremePotionDispatchTests" />.
/// </summary>
[TestClass]
public sealed class IngredientPotionTests
{
    private const int UnrelatedId = 995;
    private const int Avantoe = 261;
    private const int GroundMudRunes = 9594;

    // potion, ingredient, doses
    [TestMethod]
    [DataRow("superAttack", 2436)]
    [DataRow("superStrength", 2440)]
    [DataRow("superDefence", 2442)]
    [DataRow("magic", 3040)]
    [DataRow("ranging", 2444)]
    public void Potion_DeclaresItsFourDoseIds(string potion, int fullDoseId)
    {
        var world = new World();

        Assert.AreEqual(fullDoseId, world.Create(potion).DoseIds[0]);
        Assert.HasCount(4, world.Create(potion).DoseIds);
    }

    [TestMethod]
    [DataRow("superAttack", Avantoe)]
    [DataRow("magic", GroundMudRunes)]
    public void IngredientWithoutAnUnfinishedPotion_DoesNothing(string potion, int ingredientId)
    {
        var world = new World();

        var result = world.Create(potion).UseItemOnItem(world.Item(ingredientId), world.Item(UnrelatedId), world.Character);

        Assert.IsFalse(result);
        world.Herblore.DidNotReceiveWithAnyArgs().MakePotion(default!, default!, default!, default!, default);
    }

    [TestMethod]
    [DataRow("superAttack")]
    [DataRow("superStrength")]
    [DataRow("superDefence")]
    [DataRow("magic")]
    [DataRow("ranging")]
    public void TwoDosesOfThePotion_AreCombined(string potion)
    {
        var world = new World();
        var script = world.Create(potion);
        var used = world.Item(script.DoseIds[0]);
        var usedWith = world.Item(script.DoseIds[1]);
        world.Potions.CombinePotions(world.Character, used, usedWith, Arg.Is<int[]>(ids => ids.SequenceEqual(script.DoseIds))).Returns(true);

        Assert.IsTrue(script.UseItemOnItem(used, usedWith, world.Character));
    }

    [TestMethod]
    [DataRow("superAttack")]
    [DataRow("superStrength")]
    [DataRow("superDefence")]
    [DataRow("magic")]
    [DataRow("ranging")]
    public void UnrelatedItems_ReturnFalseAndTouchNothing(string potion)
    {
        var world = new World();

        var result = world.Create(potion).UseItemOnItem(world.Item(UnrelatedId), world.Item(UnrelatedId + 1), world.Character);

        Assert.IsFalse(result);
        world.Herblore.DidNotReceiveWithAnyArgs().MakePotion(default!, default!, default!, default!, default);
        world.Potions.DidNotReceiveWithAnyArgs().CombinePotions(default!, default!, default!, default!);
    }

    private sealed class World
    {
        public IPotionSkillService Potions { get; } = Substitute.For<IPotionSkillService>();
        public IHerbloreSkillService Herblore { get; } = Substitute.For<IHerbloreSkillService>();
        public IHerbloreService Definitions { get; } = Substitute.For<IHerbloreService>();
        public ICharacter Character { get; } = Substitute.For<ICharacter>();

        public IItem Item(int id)
        {
            var item = Substitute.For<IItem>();
            item.Id.Returns(id);
            return item;
        }

        public ScriptView Create(string potion) => new(potion switch
        {
            "superAttack" => new SuperAttackPotion(Potions, Herblore, Definitions),
            "superStrength" => new SuperStrengthPotion(Potions, Herblore, Definitions),
            "superDefence" => new SuperDefencePotion(Potions, Herblore, Definitions),
            "magic" => new MagicPotion(Potions, Herblore, Definitions),
            _ => new RangingPotion(Potions, Herblore, Definitions)
        });
    }

    /// <summary>Exposes the script's dose ids (read from its registration attribute) next to its item-use handler.</summary>
    private sealed class ScriptView(Potion script)
    {
        public int[] DoseIds { get; } = script.GetType()
            .GetCustomAttributes(typeof(ItemScriptMetaDataAttribute), false)
            .Cast<ItemScriptMetaDataAttribute>().SelectMany(a => a.ItemIds).ToArray();

        public bool UseItemOnItem(IItem used, IItem usedWith, ICharacter character) => script.UseItemOnItem(used, usedWith, character);
    }
}
