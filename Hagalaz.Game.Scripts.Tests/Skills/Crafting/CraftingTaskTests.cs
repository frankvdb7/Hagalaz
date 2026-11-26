using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Scripts.Skills.Crafting;
using NSubstitute;
using static Hagalaz.Game.Scripts.Tests.SkillTaskTestSupport;

namespace Hagalaz.Game.Scripts.Tests.Skills.Crafting;

[TestClass]
public sealed class CraftingTaskTests
{
    private const int GoldBar = CraftingSkillService.GoldBar;
    private const int Gem = 1607;
    private const int GemRing = 1639;
    private const int GoldRing = 1635;
    private const int Needle = CraftingSkillService.NeedleId;
    private const int Thread = CraftingSkillService.ThreadId;
    private const int Hide = 1741;
    private const int Body = 1129;

    [TestMethod]
    public void Jewelry_WhenGemIsMissingAtConsumeTick_ConsumesNothingAndGrantsNothing()
    {
        var (inventory, performer, itemBuilder, contextAccessor) = CreateScenario(GoldBar, Gem);
        var task = CreateJewelryTask(contextAccessor, itemBuilder, Gem, GemRing);

        task.Tick();
        RemoveAll(inventory, Gem);
        task.Tick();
        task.Tick();

        Assert.IsTrue(task.IsCancelled);
        Assert.AreEqual(1, inventory.Items.GetCountById(GoldBar));
        Assert.AreEqual(0, inventory.Items.GetCountById(GemRing));
        performer.Statistics.DidNotReceive().AddExperience(Arg.Any<int>(), Arg.Any<double>());
    }

    [TestMethod]
    public void Jewelry_WhenGoldBarIsMissingAtConsumeTick_ConsumesNothingAndGrantsNothing()
    {
        var (inventory, performer, itemBuilder, contextAccessor) = CreateScenario(GoldBar, Gem);
        var task = CreateJewelryTask(contextAccessor, itemBuilder, Gem, GemRing);

        task.Tick();
        RemoveAll(inventory, GoldBar);
        task.Tick();
        task.Tick();

        Assert.IsTrue(task.IsCancelled);
        Assert.AreEqual(1, inventory.Items.GetCountById(Gem));
        Assert.AreEqual(0, inventory.Items.GetCountById(GemRing));
        performer.Statistics.DidNotReceive().AddExperience(Arg.Any<int>(), Arg.Any<double>());
    }

    [TestMethod]
    public void Jewelry_WhenGemAndBarPresent_ConsumesBothAndGrantsProductAndExperience()
    {
        var (inventory, performer, itemBuilder, contextAccessor) = CreateScenario(GoldBar, Gem);
        var task = CreateJewelryTask(contextAccessor, itemBuilder, Gem, GemRing);

        task.Tick();
        task.Tick();
        task.Tick();

        Assert.AreEqual(0, inventory.Items.GetCountById(GoldBar));
        Assert.AreEqual(0, inventory.Items.GetCountById(Gem));
        Assert.AreEqual(1, inventory.Items.GetCountById(GemRing));
        performer.Statistics.Received(1).AddExperience(Arg.Any<int>(), 15.0);
    }

    [TestMethod]
    public void Jewelry_WhenPlainGold_ConsumesExactlyOneGoldBar()
    {
        var (inventory, performer, itemBuilder, contextAccessor) = CreateScenario(GoldBar, GoldBar);
        var task = CreateJewelryTask(contextAccessor, itemBuilder, GoldBar, GoldRing);

        task.Tick();
        task.Tick();
        task.Tick();

        Assert.AreEqual(1, inventory.Items.GetCountById(GoldBar));
        Assert.AreEqual(1, inventory.Items.GetCountById(GoldRing));
        performer.Statistics.Received(1).AddExperience(Arg.Any<int>(), 15.0);
    }

    [TestMethod]
    public void Jewelry_WhenInterruptedBeforeConsumeTick_DoesNotMutateInventory()
    {
        var (inventory, performer, itemBuilder, contextAccessor) = CreateScenario(GoldBar, Gem);
        var task = CreateJewelryTask(contextAccessor, itemBuilder, Gem, GemRing);

        task.Tick();
        task.Cancel();
        task.Tick();
        task.Tick();

        Assert.AreEqual(1, inventory.Items.GetCountById(GoldBar));
        Assert.AreEqual(1, inventory.Items.GetCountById(Gem));
        Assert.AreEqual(0, inventory.Items.GetCountById(GemRing));
        performer.Statistics.DidNotReceive().AddExperience(Arg.Any<int>(), Arg.Any<double>());
    }

    [TestMethod]
    public void Leather_WhenHideRemovedBeforeConsumeTick_KeepsRemainingHidesAndThreadAndGrantsNothing()
    {
        var (inventory, performer, itemBuilder, contextAccessor) = CreateScenario(Needle, Thread, Hide, Hide, Hide);
        var task = CreateLeatherTask(contextAccessor, itemBuilder, requiredHides: 3);

        task.Tick();
        RemoveAll(inventory, Hide, 1);
        task.Tick();
        task.Tick();

        Assert.IsTrue(task.IsCancelled);
        Assert.AreEqual(2, inventory.Items.GetCountById(Hide));
        Assert.AreEqual(1, inventory.Items.GetCountById(Thread));
        Assert.AreEqual(1, inventory.Items.GetCountById(Needle));
        Assert.AreEqual(0, inventory.Items.GetCountById(Body));
        performer.Statistics.DidNotReceive().AddExperience(Arg.Any<int>(), Arg.Any<double>());
    }

    [TestMethod]
    public void Leather_WhenAllHidesPresent_ConsumesExactHidesAndGrantsProductAndExperienceOnce()
    {
        var (inventory, performer, itemBuilder, contextAccessor) = CreateScenario(Needle, Thread, Hide, Hide, Hide, Hide);
        var task = CreateLeatherTask(contextAccessor, itemBuilder, requiredHides: 3);

        task.Tick();
        task.Tick();
        task.Tick();

        Assert.AreEqual(1, inventory.Items.GetCountById(Hide));
        Assert.AreEqual(1, inventory.Items.GetCountById(Body));
        Assert.AreEqual(1, inventory.Items.GetCountById(Thread));
        performer.Statistics.Received(1).AddExperience(Arg.Any<int>(), 25.0);
    }

    [TestMethod]
    public void Leather_WhenInterruptedBeforeConsumeTick_DoesNotMutateInventory()
    {
        var (inventory, performer, itemBuilder, contextAccessor) = CreateScenario(Needle, Thread, Hide, Hide, Hide);
        var task = CreateLeatherTask(contextAccessor, itemBuilder, requiredHides: 3);

        task.Tick();
        task.Cancel();
        task.Tick();
        task.Tick();

        Assert.AreEqual(3, inventory.Items.GetCountById(Hide));
        Assert.AreEqual(1, inventory.Items.GetCountById(Thread));
        Assert.AreEqual(1, inventory.Items.GetCountById(Needle));
        Assert.AreEqual(0, inventory.Items.GetCountById(Body));
        performer.Statistics.DidNotReceive().AddExperience(Arg.Any<int>(), Arg.Any<double>());
    }

    [TestMethod]
    public void Leather_OnFifthCraft_ConsumesExactlyOneThread()
    {
        var (inventory, _, itemBuilder, contextAccessor) = CreateScenario(
            [Needle, Needle, Needle, Needle, Needle, Thread, Thread, .. Enumerable.Repeat(Hide, 15)]);
        var task = CreateLeatherTask(contextAccessor, itemBuilder, requiredHides: 3, totalMakeCount: 5);

        for (var i = 0; i < 27; i++) task.Tick();

        Assert.AreEqual(0, inventory.Items.GetCountById(Hide));
        Assert.AreEqual(5, inventory.Items.GetCountById(Body));
        Assert.AreEqual(1, inventory.Items.GetCountById(Thread));
    }

    [TestMethod]
    public void Leather_WhenThreadMissingAtFifthCraft_RestoresHidesAndGrantsNothingForThatCraft()
    {
        var (inventory, performer, itemBuilder, contextAccessor) = CreateScenario(
            [Needle, Needle, Needle, Needle, Needle, Thread, .. Enumerable.Repeat(Hide, 15)]);
        var task = CreateLeatherTask(contextAccessor, itemBuilder, requiredHides: 3, totalMakeCount: 5);

        for (var i = 0; i < 24; i++) task.Tick();
        RemoveAll(inventory, Thread);
        task.Tick();
        task.Tick();
        task.Tick();

        Assert.IsTrue(task.IsCancelled);
        Assert.AreEqual(3, inventory.Items.GetCountById(Hide));
        Assert.AreEqual(4, inventory.Items.GetCountById(Body));
        performer.Statistics.Received(4).AddExperience(Arg.Any<int>(), 25.0);
    }

    private static JewelryTask CreateJewelryTask(ICharacterContextAccessor contextAccessor, TestItemBuilder itemBuilder,
        int resourceId, int productId)
    {
        return new JewelryTask(contextAccessor, Substitute.For<IItemService>(), itemBuilder)
        {
            Definition = new JewelryDto { ResourceID = resourceId, ProductID = productId, Experience = 15.0 },
            TotalMakeCount = 1
        };
    }

    private static LeatherTask CreateLeatherTask(ICharacterContextAccessor contextAccessor, TestItemBuilder itemBuilder,
        int requiredHides, int totalMakeCount = 1)
    {
        return new LeatherTask(contextAccessor, Substitute.For<IItemService>(), itemBuilder)
        {
            Definition = new LeatherDto
            {
                ProductId = Body, ResourceID = Hide, RequiredResourceCount = requiredHides, RequiredLevel = 1, Experience = 25.0
            },
            TotalMakeCount = totalMakeCount
        };
    }
}
