using System.Reflection;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Widgets.PriceCheck;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Widgets.PriceCheck;

[TestClass]
public sealed class PriceCheckerTransferTests
{
    [TestMethod]
    public void AddItemToPriceChecker_WhenFull_LeavesInventoryItemUnchanged()
    {
        var item = CreateItem(100, 2);
        var (inventory, priceCheckerItems) = CreateFullContainers(item, CreateItem(200, 1));
        var script = CreateScript(inventory, priceCheckerItems);

        Assert.IsFalse(InvokeTransfer(script, "AddItemToPriceChecker", item, 1));

        Assert.AreSame(item, inventory[0]);
        Assert.AreEqual(2, item.Count);
        Assert.AreEqual(1, priceCheckerItems.GetCountById(200));
        Assert.AreEqual(0, priceCheckerItems.GetCountById(100));
    }

    [TestMethod]
    public void RemoveItemToInventory_WhenFull_LeavesPriceCheckerItemUnchanged()
    {
        var item = CreateItem(100, 3);
        var (inventory, priceCheckerItems) = CreateFullContainers(CreateItem(200, 1), item);
        var script = CreateScript(inventory, priceCheckerItems);

        Assert.IsFalse(InvokeTransfer(script, "RemoveItemToInventory", item, 2));
        AssertItemsRemain(inventory, priceCheckerItems, item);
    }

    [TestMethod]
    public void TryClose_WhenInventoryCannotAcceptItems_MovesRemainderToRewards()
    {
        var item = CreateItem(100, 3);
        var (inventory, priceCheckerItems) = CreateFullContainers(CreateItem(200, 1), item);
        var rewards = new TestRewardContainer();
        var script = CreateScript(inventory, priceCheckerItems, rewards);

        Assert.IsTrue(script.TryClose());

        Assert.AreEqual(0, priceCheckerItems.TakenSlots);
        Assert.AreEqual(3, rewards.GetCountById(100));
        Assert.AreEqual(1, inventory.GetCountById(200));
    }

    private static (TestInventory Inventory, TestContainer PriceCheckerItems) CreateFullContainers(
        IItem inventoryItem,
        IItem priceCheckerItem)
    {
        var inventory = new TestInventory(1);
        Assert.IsTrue(inventory.Add(inventoryItem));
        var priceCheckerItems = new TestContainer(StorageType.AlwaysStack, 1);
        Assert.IsTrue(priceCheckerItems.Add(priceCheckerItem));
        return (inventory, priceCheckerItems);
    }

    private static void AssertItemsRemain(TestInventory inventory, TestContainer priceCheckerItems, IItem item)
    {
        Assert.AreSame(item, priceCheckerItems[0]);
        Assert.AreEqual(3, item.Count);
        Assert.AreEqual(1, inventory.GetCountById(200));
        Assert.AreEqual(0, inventory.GetCountById(100));
    }

    private static IItem CreateItem(int id, int count)
    {
        var definition = Substitute.For<IItemDefinition>();
        definition.Stackable.Returns(true);
        var script = Substitute.For<IItemScript>();
        script.CanStackItem(Arg.Any<IItem>(), Arg.Any<IItem>(), Arg.Any<bool>()).Returns(call =>
        {
            var left = call.ArgAt<IItem>(0);
            var right = call.ArgAt<IItem>(1);
            return call.ArgAt<bool>(2) || left.ItemDefinition.Stackable && left.Id == right.Id;
        });
        return CreateItem(id, count, definition, script);
    }

    private static IItem CreateItem(int id, int count, IItemDefinition definition, IItemScript script)
    {
        var item = Substitute.For<IItem>();
        item.Id.Returns(id);
        item.Count.Returns(count);
        item.ItemDefinition.Returns(definition);
        item.ItemScript.Returns(script);
        item.Clone().Returns(_ => CreateItem(id, count, definition, script));
        item.Clone(Arg.Any<int>()).Returns(call => CreateItem(id, call.ArgAt<int>(0), definition, script));
        item.Equals(Arg.Any<IItem>(), Arg.Any<bool>()).Returns(call =>
        {
            var other = call.ArgAt<IItem>(0);
            return other != null && other.Id == id && (call.ArgAt<bool>(1) || other.Count == count);
        });
        return item;
    }

    private static PriceChecker CreateScript(
        IInventoryContainer inventory,
        IItemContainer priceCheckerItems,
        IRewardContainer? rewards = null)
    {
        var character = Substitute.For<ICharacter>();
        character.Inventory.Returns(inventory);
        character.Rewards.Returns(rewards ?? new TestRewardContainer());
        var context = Substitute.For<ICharacterContext>();
        context.Character.Returns(character);
        var accessor = Substitute.For<ICharacterContextAccessor>();
        accessor.Context.Returns(context);

        var script = new PriceChecker(accessor);
        typeof(PriceChecker).GetField("_priceCheckInterface", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(script, priceCheckerItems);
        return script;
    }

    private static bool InvokeTransfer(PriceChecker script, string methodName, IItem item, int amount)
    {
        var method = typeof(PriceChecker).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (bool)method.Invoke(script, [item, amount])!;
    }

    private sealed class TestInventory(int capacity) : TradeItemContainer(StorageType.Normal, capacity), IInventoryContainer
    {
        public bool DropItem(IItem item) => false;

        public override void OnUpdate(HashSet<int>? slots = null) { }
    }

    private sealed class TestContainer(StorageType type, int capacity) : BaseItemContainer(type, capacity)
    {
        public override void OnUpdate(HashSet<int>? slots = null) { }
    }

    private sealed class TestRewardContainer() : TradeItemContainer(StorageType.AlwaysStack, byte.MaxValue), IRewardContainer
    {
        public int Claim(IItem item, int count) => -1;

        public override void OnUpdate(HashSet<int>? slots = null) { }
    }

}
