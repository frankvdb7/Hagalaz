using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Scripts.Skills.Crafting;
using NSubstitute;

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
        Assert.IsTrue(inventory.Items.GetCountById(Body) > 0);
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
        int requiredHides)
    {
        return new LeatherTask(contextAccessor, Substitute.For<IItemService>(), itemBuilder)
        {
            Definition = new LeatherDto
            {
                ProductId = Body, ResourceID = Hide, RequiredResourceCount = requiredHides, RequiredLevel = 1, Experience = 25.0
            },
            TotalMakeCount = 1
        };
    }

    private static void RemoveAll(ComposedTestContainer inventory, int id, int limit = int.MaxValue)
    {
        for (var slot = 0; slot < 28 && limit > 0; slot++)
        {
            if (inventory.Items[slot]?.Id != id) continue;
            inventory.Items.Remove(inventory.Items[slot]!, slot);
            limit--;
        }
    }

    private static (ComposedTestContainer Inventory, ICharacter Performer, TestItemBuilder ItemBuilder,
        ICharacterContextAccessor ContextAccessor) CreateScenario(params int[] itemIds)
    {
        var inventory = new ComposedTestContainer(28);
        foreach (var id in itemIds) inventory.Items.Add(CreateItem(id, 1));

        var performer = Substitute.For<ICharacter>();
        performer.Inventory.Returns(inventory);
        var context = Substitute.For<ICharacterContext>();
        context.Character.Returns(performer);
        var contextAccessor = Substitute.For<ICharacterContextAccessor>();
        contextAccessor.Context.Returns(context);

        return (inventory, performer, new TestItemBuilder(CreateItem), contextAccessor);
    }

    private static IItem CreateItem(int id, int count)
    {
        var item = Substitute.For<IItem>();
        item.Id.Returns(id);
        item.Count.Returns(count);
        var definition = Substitute.For<IItemDefinition>();
        definition.Stackable.Returns(false);
        item.ItemDefinition.Returns(definition);
        item.Equals(Arg.Any<IItem>(), Arg.Any<bool>()).Returns(call => call.ArgAt<IItem>(0).Id == id);
        item.Clone().Returns(_ => CreateItem(id, count));
        item.Clone(Arg.Any<int>()).Returns(call => CreateItem(id, call.Arg<int>()));
        return item;
    }
}
