using System;
using System.Collections.Generic;
using System.Linq;
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Skills.Herblore;

[TestClass]
public sealed class HerbloreSkillServiceTests
{
    [TestMethod]
    public void MakePotion_WithFewerThanFiveSpikes_DoesNotMutateInventoryOrGrantExperience()
    {
        var (scenario, task) = PrepareExtremeRangingTask(spikes: 4, rangingPotions: 1, requestedCount: 1);

        task.Tick();
        task.Tick();

        Assert.IsTrue(task.IsCancelled, "Task should be cancelled when spikes are fewer than required 5.");
        Assert.AreEqual(1, scenario.Inventory.Items.GetCountById(2444), "Ranging potion (3) should remain untouched.");
        Assert.AreEqual(4, scenario.Inventory.Items.GetCountById(12539), "4 Grenwall spikes should remain untouched.");
        Assert.AreEqual(0, scenario.Inventory.Items.GetCountById(15325), "No Extreme ranging potion should be created.");
        scenario.Character.Statistics.DidNotReceive().AddExperience(Arg.Any<int>(), Arg.Any<double>());
    }

    [TestMethod]
    public void MakePotion_WithExactlyFiveSpikes_ConsumesFiveSpikesAndTransformsPotionAndGrantsExperience()
    {
        var (scenario, task) = PrepareExtremeRangingTask(spikes: 5, rangingPotions: 1, requestedCount: 1);

        task.Tick();
        task.Tick();

        Assert.AreEqual(0, scenario.Inventory.Items.GetCountById(12539), "5 spikes should be consumed.");
        Assert.AreEqual(0, scenario.Inventory.Items.GetCountById(2444), "Source potion (3) should be transformed.");
        Assert.AreEqual(1, scenario.Inventory.Items.GetCountById(15325), "1 Extreme ranging potion (3) should be created.");
        scenario.Character.Statistics.Received(1).AddExperience(StatisticsConstants.Herblore, 260);
    }

    [TestMethod]
    public void MakePotion_WithSixSpikesAndTwoRequestedPotions_TransformsFirstPotionOnly()
    {
        var (scenario, task) = PrepareExtremeRangingTask(spikes: 6, rangingPotions: 2, requestedCount: 2);

        // First completion iteration (delay 2)
        task.Tick();
        task.Tick();

        Assert.AreEqual(1, scenario.Inventory.Items.GetCountById(12539), "5 spikes consumed in first iteration, leaving 1 spike.");
        Assert.AreEqual(1, scenario.Inventory.Items.GetCountById(2444), "1 Ranging potion (3) remaining.");
        Assert.AreEqual(1, scenario.Inventory.Items.GetCountById(15325), "1 Extreme ranging potion (3) created.");
        scenario.Character.Statistics.Received(1).AddExperience(StatisticsConstants.Herblore, 260);

        // Second completion iteration (delay 2) - should fail because only 1 spike remains
        task.Tick();
        task.Tick();

        Assert.IsTrue(task.IsCancelled, "Task should be cancelled on second iteration due to insufficient spikes.");
        Assert.AreEqual(1, scenario.Inventory.Items.GetCountById(12539), "1 final spike remains.");
        Assert.AreEqual(1, scenario.Inventory.Items.GetCountById(2444), "1 source potion remains.");
        Assert.AreEqual(1, scenario.Inventory.Items.GetCountById(15325), "Only 1 Extreme ranging potion (3) created in total.");
        scenario.Character.Statistics.Received(1).AddExperience(StatisticsConstants.Herblore, 260);
    }

    [TestMethod]
    public void MakePotion_CalculatesCorrectDialogueMaxCountForSpikes()
    {
        var scenario = CreateScenario((2444, 1), (2444, 1), (2444, 1), (12539, 12));
        var vial = scenario.Items[0];
        var spikes = scenario.Items[3];
        var potionDto = CreateExtremeRangingDto();

        scenario.Service.MakePotion(scenario.Character, vial, spikes, potionDto, makeUnfinished: false);

        scenario.Dialogue.Received(1).SetMaxCount(2, false);
    }

    [TestMethod]
    public void MakePotion_WithGenericSingleIngredient_ConsumesOneIngredientAndTransformsPotion()
    {
        var scenario = CreateScenario((227, 1), (257, 1));
        var vial = scenario.Items[0];
        var herb = scenario.Items[1];
        var potionDto = new PotionDto(
            PotionId: 3000,
            UnfinishedPotionId: 299,
            RequiredLevel: 1,
            Experience: 0,
            PrimaryItemIds: [],
            SecondaryItemIds: [257]);

        scenario.Service.MakePotion(scenario.Character, vial, herb, potionDto, makeUnfinished: true);
        scenario.Dialogue.PerformMakeProductCallback(299, 1);

        var task = scenario.QueuedTasks.Single();
        task.Tick();
        task.Tick();

        Assert.AreEqual(0, scenario.Inventory.Items.GetCountById(257), "Herb consumed.");
        Assert.AreEqual(0, scenario.Inventory.Items.GetCountById(227), "Vial consumed.");
        Assert.AreEqual(1, scenario.Inventory.Items.GetCountById(299), "Unfinished potion created.");
    }

    private static (Scenario Scenario, ITaskItem Task) PrepareExtremeRangingTask(int spikes, int rangingPotions, int requestedCount)
    {
        var itemSpecs = new List<(int Id, int Count)>();
        for (var i = 0; i < rangingPotions; i++)
        {
            itemSpecs.Add((2444, 1));
        }
        itemSpecs.Add((12539, spikes));

        var scenario = CreateScenario([.. itemSpecs]);
        var vial = scenario.Items[0];
        var spikeItem = scenario.Items[^1];
        var potionDto = CreateExtremeRangingDto();

        scenario.Service.MakePotion(scenario.Character, vial, spikeItem, potionDto, makeUnfinished: false);
        scenario.Dialogue.PerformMakeProductCallback(15325, requestedCount);

        return (scenario, scenario.QueuedTasks.Single());
    }

    private static PotionDto CreateExtremeRangingDto() => new(
        PotionId: 15325,
        UnfinishedPotionId: 0,
        RequiredLevel: 1,
        Experience: 260,
        PrimaryItemIds: [],
        SecondaryItemIds: [12539]);

    private static Scenario CreateScenario(params (int Id, int Count)[] itemSpecs)
    {
        var inventory = new ComposedTestContainer(28);
        var items = new List<IItem>();
        for (var slot = 0; slot < itemSpecs.Length; slot++)
        {
            var item = CreateHerbloreMockItem(itemSpecs[slot].Id, itemSpecs[slot].Count);
            items.Add(item);
            inventory.SetItem(slot, item);
        }

        var context = Substitute.For<ICharacterContext>();
        var character = Substitute.For<ICharacter>();
        context.Character.Returns(character);
        var accessor = Substitute.For<ICharacterContextAccessor>();
        accessor.Context.Returns(context);

        var statistics = Substitute.For<ICharacterStatistics>();
        statistics.GetSkillLevel(StatisticsConstants.Herblore).Returns(99);
        character.Inventory.Returns(inventory);
        character.Statistics.Returns(statistics);

        var configurations = Substitute.For<IConfigurations>();
        character.Configurations.Returns(configurations);

        var widgets = Substitute.For<IWidgetContainer>();
        widgets.GetOpenWidget(Arg.Any<int>()).Returns(Substitute.For<IWidget>());
        widgets.OpenWidget(Arg.Any<int>(), Arg.Any<IWidget>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<IWidgetScript>(), Arg.Any<bool>()).Returns(true);
        character.Widgets.Returns(widgets);

        var dialogue = Substitute.ForPartsOf<InteractiveDialogueScript>(accessor, Substitute.For<IItemService>(),
            Substitute.For<IWidgetOptionBuilder>());

        var herbloreService = Substitute.For<IHerbloreService>();
        var serviceScope = Substitute.For<IServiceScope>();
        var serviceScopeFactory = Substitute.For<IServiceScopeFactory>();
        var scopeServiceProvider = Substitute.For<IServiceProvider>();
        scopeServiceProvider.GetService(typeof(IHerbloreService)).Returns(herbloreService);
        serviceScope.ServiceProvider.Returns(scopeServiceProvider);
        serviceScopeFactory.CreateScope().Returns(serviceScope);

        var provider = Substitute.For<IServiceProvider>();
        provider.GetService(typeof(IServiceScopeFactory)).Returns(serviceScopeFactory);
        provider.GetService(typeof(InteractiveDialogueScript)).Returns(dialogue);
        provider.GetService(typeof(DefaultDialogueScript)).Returns(new DefaultDialogueScript(accessor));
        character.ServiceProvider.Returns(provider);

        var tasks = new List<ITaskItem>();
        character.QueueTask(Arg.Do<ITaskItem>(tasks.Add)).Returns(Substitute.For<IRsTaskHandle>());

        var builder = new TestItemBuilder((id, count) => CreateHerbloreMockItem(id, count));
        var service = new HerbloreSkillService(provider, builder);

        return new Scenario(character, inventory, items.ToArray(), dialogue, tasks, service, configurations);
    }

    private static IItem CreateHerbloreMockItem(int itemId, int quantity)
    {
        var mockItem = Substitute.For<IItem, IItemScript, IItemDefinition>();
        var definition = (IItemDefinition)mockItem;
        var script = (IItemScript)mockItem;

        var isStackable = itemId == 12539;
        mockItem.Id.Returns(itemId);
        mockItem.Count.Returns(quantity);
        mockItem.Name.Returns($"HerbloreItem_{itemId}");
        mockItem.ItemDefinition.Returns(definition);
        mockItem.ItemScript.Returns(script);

        definition.Stackable.Returns(isStackable);
        script.CanStackItem(Arg.Any<IItem>(), Arg.Any<IItem>(), Arg.Any<bool>()).Returns(isStackable);

        mockItem.Equals(Arg.Any<IItem>(), Arg.Any<bool>()).Returns(call =>
            call.ArgAt<IItem>(0) is { } target && target.Id == itemId && (call.ArgAt<bool>(1) || target.Count == quantity));

        mockItem.Clone().Returns(_ => CreateHerbloreMockItem(itemId, quantity));
        mockItem.Clone(Arg.Any<int>()).Returns(call => CreateHerbloreMockItem(itemId, call.ArgAt<int>(0)));
        return mockItem;
    }

    private sealed record Scenario(
        ICharacter Character,
        ComposedTestContainer Inventory,
        IItem[] Items,
        InteractiveDialogueScript Dialogue,
        List<ITaskItem> QueuedTasks,
        HerbloreSkillService Service,
        IConfigurations Configurations);
}
