using Hagalaz.Game.Abstractions.Builders.Widget;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Dialogues.Generic;
using Hagalaz.Game.Scripts.Model.Widgets;
using Hagalaz.Game.Scripts.Skills.Fletching;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Skills.Fletching;

[TestClass]
public sealed class FletchingCallbackRegressionTests
{
    [TestMethod]
    public void BowCallback_ConsumesOneLogAndOneStringAndAddsUnstrungBow()
    {
        var scenario = CreateScenario((50, 1), (1777, 1));
        Assert.IsTrue(scenario.Service.TryFletchBow(scenario.Character, scenario.Items[0], scenario.Items[1]));
        Assert.IsTrue(scenario.Dialogue.PerformMakeProductCallback(841, 1));

        var task = scenario.QueuedTasks.Single();
        task.Tick();
        task.Tick();

        Assert.AreEqual(0, scenario.Inventory.Items.GetCountById(50));
        Assert.AreEqual(0, scenario.Inventory.Items.GetCountById(1777));
        Assert.AreEqual(1, scenario.Inventory.Items.GetCountById(841));
    }

    [TestMethod]
    public void BowCallback_WhenResourceDisappearsBeforeFletching_DoesNotConsumeToolOrCreateBow()
    {
        var scenario = CreateScenario((50, 1), (1777, 1));
        scenario.Service.TryFletchBow(scenario.Character, scenario.Items[0], scenario.Items[1]);
        scenario.Dialogue.PerformMakeProductCallback(841, 1);
        scenario.Inventory.Items.Remove(scenario.Items[0], 0);

        var task = scenario.QueuedTasks.Single();
        task.Tick();
        task.Tick();

        Assert.IsTrue(task.IsCancelled);
        Assert.AreEqual(1, scenario.Inventory.Items.GetCountById(1777));
        Assert.AreEqual(0, scenario.Inventory.Items.GetCountById(841));
    }

    [TestMethod]
    public void BowCallback_WhenToolDisappearsBeforeFletching_DoesNotConsumeLogOrCreateBow()
    {
        var scenario = CreateScenario((50, 1), (1777, 1));
        scenario.Service.TryFletchBow(scenario.Character, scenario.Items[0], scenario.Items[1]);
        scenario.Dialogue.PerformMakeProductCallback(841, 1);
        scenario.Inventory.Items.Remove(scenario.Items[1], 1);

        var task = scenario.QueuedTasks.Single();
        task.Tick();
        task.Tick();

        Assert.IsTrue(task.IsCancelled);
        Assert.AreEqual(1, scenario.Inventory.Items.GetCountById(50));
        Assert.AreEqual(0, scenario.Inventory.Items.GetCountById(841));
    }

    [TestMethod]
    public void AmmoCallback_ConsumesOnlyTheAvailableResourceAndToolCount()
    {
        var scenario = CreateScenario((52, 8), (314, 3));
        Assert.IsTrue(scenario.Service.TryFletchAmmo(scenario.Character, scenario.Items[0], scenario.Items[1]));
        scenario.Dialogue.PerformMakeProductCallback(53, 1);

        scenario.QueuedTasks.Single().Tick();

        Assert.AreEqual(5, scenario.Inventory.Items.GetCountById(52));
        Assert.AreEqual(0, scenario.Inventory.Items.GetCountById(314));
        Assert.AreEqual(3, scenario.Inventory.Items.GetCountById(53));
    }

    [TestMethod]
    public void AmmoCallback_WhenResourceIsStale_DoesNotConsumeToolOrCreateAmmunition()
    {
        var scenario = CreateScenario((52, 8), (314, 3));
        scenario.Service.TryFletchAmmo(scenario.Character, scenario.Items[0], scenario.Items[1]);
        scenario.Dialogue.PerformMakeProductCallback(53, 1);
        scenario.Inventory.Items.Remove(scenario.Items[0], 0);

        var task = scenario.QueuedTasks.Single();
        task.Tick();

        Assert.IsTrue(task.IsCancelled);
        Assert.AreEqual(3, scenario.Inventory.Items.GetCountById(314));
        Assert.AreEqual(0, scenario.Inventory.Items.GetCountById(53));
    }

    [TestMethod]
    public void TipsCallback_ConsumesOneGemLeavesChiselAndAddsDefinitionAmount()
    {
        var scenario = CreateScenario((1609, 1), (1755, 1));
        Assert.IsTrue(scenario.Service.TryFletchTips(scenario.Character, scenario.Items[0], scenario.Items[1]));
        scenario.Dialogue.PerformMakeProductCallback(45, 1);

        for (var tick = 0; tick < 3; tick++) scenario.QueuedTasks.Single().Tick();

        Assert.AreEqual(0, scenario.Inventory.Items.GetCountById(1609));
        Assert.AreEqual(1, scenario.Inventory.Items.GetCountById(1755));
        Assert.AreEqual(12, scenario.Inventory.Items.GetCountById(45));
    }

    [TestMethod]
    public void TipsCallback_WhenResourceIsStale_LeavesChiselAndProducesNothing()
    {
        var scenario = CreateScenario((1609, 1), (1755, 1));
        scenario.Service.TryFletchTips(scenario.Character, scenario.Items[0], scenario.Items[1]);
        scenario.Dialogue.PerformMakeProductCallback(45, 1);
        scenario.Inventory.Items.Remove(scenario.Items[0], 0);

        var task = scenario.QueuedTasks.Single();
        for (var tick = 0; tick < 3; tick++) task.Tick();

        Assert.IsTrue(task.IsCancelled);
        Assert.AreEqual(1, scenario.Inventory.Items.GetCountById(1755));
        Assert.AreEqual(0, scenario.Inventory.Items.GetCountById(45));
    }

    [TestMethod]
    public void WoodRecipe_CutsOneLogIntoTheDefinitionAmountAndGrantsScaledExperience()
    {
        var scenario = CreateScenario((1511, 1), (946, 1));
        Assert.IsTrue(scenario.Service.TryFletchWood(scenario.Character, scenario.Items[0], scenario.Items[1]));
        scenario.Dialogue.PerformMakeProductCallback(52, 1);

        for (var tick = 0; tick < 3; tick++) scenario.QueuedTasks.Single().Tick();

        Assert.AreEqual(0, scenario.Inventory.Items.GetCountById(1511));
        Assert.AreEqual(15, scenario.Inventory.Items.GetCountById(52));
        scenario.Character.Statistics.Received(1).AddExperience(StatisticsConstants.Fletching, 0.33 * 15);
    }

    [TestMethod]
    public void WoodRecipe_WhenUsedInReverseOrder_StillOpensTheDialogue()
    {
        var scenario = CreateScenario((946, 1), (1511, 1));
        Assert.IsTrue(scenario.Service.TryFletchWood(scenario.Character, scenario.Items[0], scenario.Items[1]));
    }

    [TestMethod]
    public void CrossbowRecipe_HasNoAnimationAndAddsTheProduct()
    {
        var scenario = CreateScenario((9440, 1), (9420, 1));
        Assert.IsTrue(scenario.Service.TryFletchBow(scenario.Character, scenario.Items[0], scenario.Items[1]));
        scenario.Dialogue.PerformMakeProductCallback(9454, 1);

        var task = scenario.QueuedTasks.Single();
        task.Tick();
        task.Tick();

        scenario.Character.DidNotReceive().QueueAnimation(Arg.Any<Hagalaz.Game.Abstractions.Model.IAnimation>());
        Assert.AreEqual(1, scenario.Inventory.Items.GetCountById(9454));
    }

    [TestMethod]
    public void DartRecipe_MakesTheDefinitionAmountPerFeatherAndGrantsScaledExperience()
    {
        var scenario = CreateScenario((819, 10), (314, 10));
        Assert.IsTrue(scenario.Service.TryFletchAmmo(scenario.Character, scenario.Items[0], scenario.Items[1]));
        scenario.Dialogue.PerformMakeProductCallback(806, 1);

        scenario.QueuedTasks.Single().Tick();

        Assert.AreEqual(10, scenario.Inventory.Items.GetCountById(806));
        scenario.Character.Statistics.Received(1).AddExperience(StatisticsConstants.Fletching, 18.0 * 10);
    }

    [TestMethod]
    public void GrappleRecipe_IsFletchedThroughTheAmmoCategory()
    {
        var scenario = CreateScenario((9416, 1), (954, 1));
        Assert.IsTrue(scenario.Service.TryFletchAmmo(scenario.Character, scenario.Items[0], scenario.Items[1]));
        scenario.Dialogue.PerformMakeProductCallback(9418, 1);

        scenario.QueuedTasks.Single().Tick();

        Assert.AreEqual(1, scenario.Inventory.Items.GetCountById(9418));
    }

    [TestMethod]
    public void Fletch_WhenThePairBelongsToAnotherCategory_ReturnsFalse()
    {
        var scenario = CreateScenario((50, 1), (1777, 1));

        Assert.IsFalse(scenario.Service.TryFletchAmmo(scenario.Character, scenario.Items[0], scenario.Items[1]));
        Assert.IsFalse(scenario.Service.TryFletchWood(scenario.Character, scenario.Items[0], scenario.Items[1]));
        Assert.IsFalse(scenario.Service.TryFletchTips(scenario.Character, scenario.Items[0], scenario.Items[1]));
    }

    [TestMethod]
    public void Fletch_WhenTheLevelIsTooLow_SendsMessageAndQueuesNoTask()
    {
        var scenario = CreateScenario((819, 10), (314, 10));
        scenario.Character.Statistics.GetSkillLevel(StatisticsConstants.Fletching).Returns(0);
        scenario.Service.TryFletchAmmo(scenario.Character, scenario.Items[0], scenario.Items[1]);

        scenario.Dialogue.PerformMakeProductCallback(806, 1);

        Assert.AreEqual(0, scenario.QueuedTasks.Count);
    }

    private static Scenario CreateScenario(params (int Id, int Count)[] itemSpecs)
    {
        var inventory = new ComposedTestContainer(8);
        var items = itemSpecs.Select(spec => CreateItem(spec.Id, spec.Count)).ToArray();
        for (var slot = 0; slot < items.Length; slot++) inventory.SetItem(slot, items[slot]);
        var context = Substitute.For<ICharacterContext>();
        var character = Substitute.For<ICharacter>();
        context.Character.Returns(character);
        var accessor = Substitute.For<ICharacterContextAccessor>();
        accessor.Context.Returns(context);
        var statistics = Substitute.For<ICharacterStatistics>();
        statistics.GetSkillLevel(StatisticsConstants.Fletching).Returns(99);
        character.Inventory.Returns(inventory);
        character.Statistics.Returns(statistics);
        var widgets = Substitute.For<IWidgetContainer>();
        var dialogueWidget = Substitute.For<IWidget>();
        widgets.GetOpenWidget(Arg.Any<int>()).Returns(dialogueWidget);
        widgets.OpenWidget(Arg.Any<int>(), Arg.Any<IWidget>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<IWidgetScript>(), Arg.Any<bool>()).Returns(true);
        character.Widgets.Returns(widgets);
        var dialogue = new InteractiveDialogueScript(accessor, Substitute.For<IItemService>(),
            Substitute.For<IWidgetOptionBuilder>());
        var provider = Substitute.For<IServiceProvider>();
        provider.GetService(typeof(InteractiveDialogueScript)).Returns(dialogue);
        provider.GetService(typeof(DefaultDialogueScript)).Returns(new DefaultDialogueScript(accessor));
        character.ServiceProvider.Returns(provider);
        var tasks = new List<ITaskItem>();
        character.QueueTask(Arg.Do<ITaskItem>(tasks.Add)).Returns(Substitute.For<IRsTaskHandle>());
        var builder = new TestItemBuilder((id, count) => CreateItem(id, count));
        return new Scenario(character, inventory, items, dialogue, tasks, new FletchingSkillService(builder, TestFletchingRecipes.Service));
    }

    private static IItem CreateItem(int id, int count)
    {
        var item = Substitute.For<IItem>();
        item.Id.Returns(id);
        item.Count.Returns(count);
        item.Name.Returns($"Item {id}");
        var definition = Substitute.For<IItemDefinition>();
        definition.Stackable.Returns(true);
        item.ItemDefinition.Returns(definition);
        var script = Substitute.For<IItemScript>();
        script.CanStackItem(Arg.Any<IItem>(), Arg.Any<IItem>(), Arg.Any<bool>()).Returns(true);
        item.ItemScript.Returns(script);
        item.Equals(Arg.Any<IItem>(), Arg.Any<bool>()).Returns(call =>
            call.ArgAt<IItem>(0) is { } other && other.Id == id &&
            (call.ArgAt<bool>(1) || other.Count == count));
        item.Clone().Returns(_ => CreateItem(id, count));
        item.Clone(Arg.Any<int>()).Returns(call => CreateItem(id, call.ArgAt<int>(0)));
        return item;
    }

    private sealed record Scenario(ICharacter Character, ComposedTestContainer Inventory, IItem[] Items,
        InteractiveDialogueScript Dialogue, List<ITaskItem> QueuedTasks, FletchingSkillService Service);
}
