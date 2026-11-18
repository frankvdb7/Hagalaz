using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Skills.Smithing;
using NSubstitute;
using static Hagalaz.Game.Scripts.Tests.SkillTaskTestSupport;

namespace Hagalaz.Game.Scripts.Tests.Skills.Smithing;

[TestClass]
public sealed class SmithingTaskTests
{
    private const int Copper = 436;
    private const int Tin = 438;
    private const int Iron = 440;
    private const int Coal = 453;
    private const int BronzeBar = 2349;
    private const int SteelBar = 2353;

    [TestMethod]
    public void Smelt_WhenOneOreIsMissingAtConsumeTick_ConsumesNothingAndGrantsNothing()
    {
        var (inventory, performer, itemBuilder, contextAccessor) = CreateScenario(Copper, Tin);
        var task = CreateSmeltTask(contextAccessor, itemBuilder, BronzeBar, new ItemDto(Copper), new ItemDto(Tin));

        task.Tick();
        RemoveAll(inventory, Copper);
        task.Tick();
        task.Tick();

        Assert.IsTrue(task.IsCancelled);
        Assert.AreEqual(1, inventory.Items.GetCountById(Tin));
        Assert.AreEqual(0, inventory.Items.GetCountById(BronzeBar));
        performer.Statistics.DidNotReceive().AddExperience(Arg.Any<int>(), Arg.Any<double>());
    }

    [TestMethod]
    public void Smelt_WhenCoalIsInsufficientAtConsumeTick_ConsumesNothingAndGrantsNothing()
    {
        var (inventory, performer, itemBuilder, contextAccessor) = CreateScenario(Iron, Coal, Coal);
        var task = CreateSmeltTask(contextAccessor, itemBuilder, SteelBar, new ItemDto(Iron), new ItemDto(Coal, 2));

        task.Tick();
        RemoveAll(inventory, Coal, 1);
        task.Tick();
        task.Tick();

        Assert.IsTrue(task.IsCancelled);
        Assert.AreEqual(1, inventory.Items.GetCountById(Iron));
        Assert.AreEqual(1, inventory.Items.GetCountById(Coal));
        Assert.AreEqual(0, inventory.Items.GetCountById(SteelBar));
        performer.Statistics.DidNotReceive().AddExperience(Arg.Any<int>(), Arg.Any<double>());
    }

    [TestMethod]
    public void Smelt_WhenRecipeIsComplete_ConsumesExactlyOnceAndGrantsOneBarAndExperience()
    {
        var (inventory, performer, itemBuilder, contextAccessor) = CreateScenario(Copper, Tin);
        var task = CreateSmeltTask(contextAccessor, itemBuilder, BronzeBar, new ItemDto(Copper), new ItemDto(Tin));

        task.Tick();
        task.Tick();
        task.Tick();

        Assert.AreEqual(0, inventory.Items.GetCountById(Copper));
        Assert.AreEqual(0, inventory.Items.GetCountById(Tin));
        Assert.AreEqual(1, inventory.Items.GetCountById(BronzeBar));
        performer.Statistics.Received(1).AddExperience(Arg.Any<int>(), 6.2);
    }

    [TestMethod]
    public void Smelt_WhenInterruptedBeforeConsumeTick_DoesNotMutateInventory()
    {
        var (inventory, _, itemBuilder, contextAccessor) = CreateScenario(Copper, Tin);
        var task = CreateSmeltTask(contextAccessor, itemBuilder, BronzeBar, new ItemDto(Copper), new ItemDto(Tin));

        task.Tick();
        task.Cancel();
        task.Tick();
        task.Tick();

        Assert.AreEqual(1, inventory.Items.GetCountById(Copper));
        Assert.AreEqual(1, inventory.Items.GetCountById(Tin));
        Assert.AreEqual(0, inventory.Items.GetCountById(BronzeBar));
    }

    [TestMethod]
    public void Forge_WhenFewerBarsThanRequiredAtConsumeTick_KeepsRemainingBarsAndGrantsNothing()
    {
        var (inventory, performer, itemBuilder, contextAccessor) = CreateScenario(BronzeBar, BronzeBar, BronzeBar);
        var task = CreateForgeTask(contextAccessor, itemBuilder, requiredBars: 3);

        task.Tick();
        RemoveAll(inventory, BronzeBar, 1);
        task.Tick();
        task.Tick();

        Assert.IsTrue(task.IsCancelled);
        Assert.AreEqual(2, inventory.Items.GetCountById(BronzeBar));
        Assert.AreEqual(0, inventory.Items.GetCountById(1205));
        performer.Statistics.DidNotReceive().AddExperience(Arg.Any<int>(), Arg.Any<double>());
    }

    [TestMethod]
    public void Forge_WhenAllBarsPresent_ConsumesExactCountAndGrantsProductAndExperience()
    {
        var (inventory, performer, itemBuilder, contextAccessor) = CreateScenario(BronzeBar, BronzeBar, BronzeBar, BronzeBar);
        var task = CreateForgeTask(contextAccessor, itemBuilder, requiredBars: 3);

        task.Tick();
        task.Tick();
        task.Tick();

        Assert.AreEqual(1, inventory.Items.GetCountById(BronzeBar));
        Assert.AreEqual(1, inventory.Items.GetCountById(1205));
        performer.Statistics.Received(1).AddExperience(Arg.Any<int>(), 37.5);
    }

    [TestMethod]
    [DataRow(1, 50)]
    [DataRow(15, 50)]
    [DataRow(16, 51)]
    [DataRow(30, 65)]
    [DataRow(44, 79)]
    [DataRow(45, 80)]
    [DataRow(99, 80)]
    public void IronSmeltChance_FollowsPerLevelProgressionCappedAtEighty(int level, int expectedPercent)
    {
        Assert.AreEqual(expectedPercent, SmeltTask.GetIronSmeltChancePercent(level));
    }

    [TestMethod]
    [DataRow(15, 49, true)]
    [DataRow(15, 50, false)]
    [DataRow(45, 79, true)]
    [DataRow(45, 80, false)]
    [DataRow(99, 80, false)]
    public void IronSmelt_RollBelowChanceSucceedsAndRollAtChanceFails(int level, int roll, bool expected)
    {
        Assert.AreEqual(expected, SmeltTask.IsIronSmeltSuccessful(level, roll));
    }

    private static SmeltTask CreateSmeltTask(ICharacterContextAccessor contextAccessor, TestItemBuilder itemBuilder,
        int barId, params ItemDto[] ores)
    {
        return new SmeltTask(contextAccessor, itemBuilder, Substitute.For<IItemService>())
        {
            Definition = new SmithingDefinition(barId, new SmeltingBarDefinition(ores, 1, 6.2), new ForgingBarDefinition()),
            TotalSmeltCount = 1
        };
    }

    private static ForgeTask CreateForgeTask(ICharacterContextAccessor contextAccessor, TestItemBuilder itemBuilder,
        int requiredBars)
    {
        var entry = new ForgingBarEntry(new ItemDto(1205), 1, requiredBars);
        return new ForgeTask(contextAccessor, Substitute.For<IItemService>(), itemBuilder)
        {
            Definition = new SmithingDefinition(BronzeBar, new SmeltingBarDefinition([], 1, 0),
                new ForgingBarDefinition(12.5, [entry])),
            ForgeDefinition = entry,
            TotalForgeCount = 1
        };
    }
}
