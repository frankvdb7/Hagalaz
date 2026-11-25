using Hagalaz.Game.Abstractions.Builders.Widget;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Dialogues.Generic;
using Hagalaz.Game.Scripts.Model.Widgets;
using Hagalaz.Game.Scripts.Skills.Herblore.Herbs;
using Hagalaz.Game.Scripts.Skills.Herblore.Potions;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Skills.Herblore;

/// <summary>
///     Pins the overload recipe read from its definition: the extreme potion script starts it when the definition's herb is used on one of
///     its extreme potions, and one iteration consumes every extreme potion, turns the herb into the product and grants the definition's experience.
/// </summary>
[TestClass]
public sealed class HerbloreOverloadTests
{
    private const int Torstol = 269;
    private const int Product = 15333;
    private static readonly int[] _extremes = [15309, 15313, 15317, 15321, 15325];

    private static readonly PotionDto _overload = new()
    {
        PotionId = Product, Kind = PotionKind.Overload, UnfinishedPotionId = Torstol, RequiredLevel = 96, Experience = 1000,
        PrimaryItemIds = [], SecondaryItemIds = _extremes, SecondaryItemCount = 1
    };

    [TestMethod]
    public void HerbUsedOnExtremePotion_StartsTheOverloadFromItsDefinitionInEitherOrder()
    {
        var potions = Substitute.For<IPotionSkillService>();
        var herblore = Substitute.For<IHerbloreSkillService>();
        var definitions = Substitute.For<IHerbloreService>();
        definitions.FindPotionBySecondaryItemId(PotionKind.Overload, Torstol, 15309).Returns(_overload);
        var character = Substitute.For<ICharacter>();
        var torstol = Item(Torstol);
        var extreme = Item(15309);
        herblore.MakeOverload(character, torstol, _overload).Returns(true);
        var script = new ExtremeAttackPotion(potions, herblore, definitions);

        Assert.IsTrue(script.UseItemOnItem(torstol, extreme, character));
        Assert.IsTrue(script.UseItemOnItem(extreme, torstol, character));

        herblore.Received(2).MakeOverload(character, torstol, _overload);
        potions.DidNotReceiveWithAnyArgs().CombinePotions(default!, default!, default!, default!);
    }

    [TestMethod]
    public void ItemThatIsNotPartOfAnOverload_DoesNotStartOne()
    {
        var herblore = Substitute.For<IHerbloreSkillService>();
        var script = new ExtremeAttackPotion(Substitute.For<IPotionSkillService>(), herblore, Substitute.For<IHerbloreService>());

        Assert.IsFalse(script.UseItemOnItem(Item(995), Item(15309), Substitute.For<ICharacter>()));

        herblore.DidNotReceiveWithAnyArgs().MakeOverload(default!, default!, default!);
    }

    [TestMethod]
    public void MakeOverload_ConsumesEveryExtremePotionAndGrantsTheDefinitionsProductAndExperience()
    {
        var scenario = CreateScenario(99, [Torstol, .. _extremes]);

        Assert.IsTrue(scenario.Service.MakeOverload(scenario.Character, scenario.Inventory.Items[0]!, _overload));
        CollectionAssert.AreEqual(new[] { Product }, scenario.Dialogue.ProductIds);
        Assert.IsTrue(scenario.Dialogue.PerformMakeProductCallback(Product, 1));
        for (var tick = 0; tick < 3; tick++) scenario.Tasks.Single().Tick();

        Assert.AreEqual(1, scenario.Inventory.Items.GetCountById(Product));
        Assert.AreEqual(0, scenario.Inventory.Items.GetCountById(Torstol));
        foreach (var extreme in _extremes) Assert.AreEqual(0, scenario.Inventory.Items.GetCountById(extreme));
        scenario.Character.Statistics.Received(1).AddExperience(StatisticsConstants.Herblore, 1000);
    }

    [TestMethod]
    public void MakeOverload_BelowTheDefinitionsLevel_SendsTheLevelAndMakesNothing()
    {
        var scenario = CreateScenario(95, [Torstol, .. _extremes]);

        Assert.IsTrue(scenario.Service.MakeOverload(scenario.Character, scenario.Inventory.Items[0]!, _overload));

        scenario.Character.Received(1).SendChatMessage("You need herblore level of 96 to make this potion.", Arg.Any<ChatMessageType>(), Arg.Any<string?>(), Arg.Any<string?>());
        Assert.IsEmpty(scenario.Tasks);
        Assert.AreEqual(1, scenario.Inventory.Items.GetCountById(Torstol));
    }

    private static Scenario CreateScenario(int herbloreLevel, int[] itemIds)
    {
        var inventory = new ComposedTestContainer(28);
        for (var slot = 0; slot < itemIds.Length; slot++) inventory.SetItem(slot, Item(itemIds[slot]));
        var context = Substitute.For<ICharacterContext>();
        var character = Substitute.For<ICharacter>();
        context.Character.Returns(character);
        var accessor = Substitute.For<ICharacterContextAccessor>();
        accessor.Context.Returns(context);
        var statistics = Substitute.For<ICharacterStatistics>();
        statistics.GetSkillLevel(StatisticsConstants.Herblore).Returns(herbloreLevel);
        character.Inventory.Returns(inventory);
        character.Statistics.Returns(statistics);
        var widgets = Substitute.For<IWidgetContainer>();
        widgets.GetOpenWidget(Arg.Any<int>()).Returns(Substitute.For<IWidget>());
        widgets.OpenWidget(Arg.Any<int>(), Arg.Any<IWidget>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<IWidgetScript>(), Arg.Any<bool>()).Returns(true);
        character.Widgets.Returns(widgets);
        var dialogue = new InteractiveDialogueScript(accessor, Substitute.For<IItemService>(), Substitute.For<IWidgetOptionBuilder>());
        var provider = Substitute.For<IServiceProvider>();
        provider.GetService(typeof(InteractiveDialogueScript)).Returns(dialogue);
        provider.GetService(typeof(DefaultDialogueScript)).Returns(new DefaultDialogueScript(accessor));
        provider.GetService(typeof(IItemService)).Returns(Substitute.For<IItemService>());
        character.ServiceProvider.Returns(provider);
        var tasks = new List<ITaskItem>();
        character.QueueTask(Arg.Do<ITaskItem>(tasks.Add)).Returns(Substitute.For<IRsTaskHandle>());
        var service = new HerbloreSkillService(new TestItemBuilder((id, count) => SkillTaskTestSupport.CreateItem(id, count)));
        return new Scenario(character, inventory, dialogue, tasks, service);
    }

    private static IItem Item(int id) => SkillTaskTestSupport.CreateItem(id, 1);

    private sealed record Scenario(ICharacter Character, ComposedTestContainer Inventory, InteractiveDialogueScript Dialogue, List<ITaskItem> Tasks,
        HerbloreSkillService Service);
}
