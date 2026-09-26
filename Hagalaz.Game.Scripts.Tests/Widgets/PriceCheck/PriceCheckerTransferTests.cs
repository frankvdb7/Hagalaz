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
        var inventory = new TestInventory(1);
        var item = new TestItem(100, 2, stackable: true);
        Assert.IsTrue(inventory.Add(item));

        var priceCheckerItems = new TestContainer(StorageType.AlwaysStack, 1);
        Assert.IsTrue(priceCheckerItems.Add(new TestItem(200, 1, stackable: true)));
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
        var inventory = new TestInventory(1);
        Assert.IsTrue(inventory.Add(new TestItem(200, 1, stackable: true)));

        var priceCheckerItems = new TestContainer(StorageType.AlwaysStack, 1);
        var item = new TestItem(100, 3, stackable: true);
        Assert.IsTrue(priceCheckerItems.Add(item));
        var script = CreateScript(inventory, priceCheckerItems);

        Assert.IsFalse(InvokeTransfer(script, "RemoveItemToInventory", item, 2));

        Assert.AreSame(item, priceCheckerItems[0]);
        Assert.AreEqual(3, item.Count);
        Assert.AreEqual(1, inventory.GetCountById(200));
        Assert.AreEqual(0, inventory.GetCountById(100));
    }

    [TestMethod]
    public void TryClose_WhenInventoryCannotAcceptItems_LeavesItemsInTheOpenContainer()
    {
        var inventory = new TestInventory(1);
        Assert.IsTrue(inventory.Add(new TestItem(200, 1, stackable: true)));

        var priceCheckerItems = new TestContainer(StorageType.AlwaysStack, 1);
        var item = new TestItem(100, 3, stackable: true);
        Assert.IsTrue(priceCheckerItems.Add(item));
        var script = CreateScript(inventory, priceCheckerItems);

        Assert.IsFalse(script.TryClose());

        Assert.AreSame(item, priceCheckerItems[0]);
        Assert.AreEqual(3, item.Count);
        Assert.AreEqual(1, inventory.GetCountById(200));
        Assert.AreEqual(0, inventory.GetCountById(100));
    }

    private static PriceChecker CreateScript(IInventoryContainer inventory, IItemContainer priceCheckerItems)
    {
        var character = Substitute.For<ICharacter>();
        character.Inventory.Returns(inventory);
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

    private sealed class TestItem : IItem
    {
        public int Id { get; }
        public int Count { get; set; }
        public string Name => $"Test item {Id}";
        public IItemDefinition ItemDefinition { get; }
        public IEquipmentDefinition EquipmentDefinition { get; } = Substitute.For<IEquipmentDefinition>();
        public IItemScript ItemScript { get; }
        public IEquipmentScript EquipmentScript { get; } = Substitute.For<IEquipmentScript>();
        public long[] ExtraData => [];

        public TestItem(int id, int count, bool stackable)
        {
            Id = id;
            Count = count;
            var definition = Substitute.For<IItemDefinition>();
            definition.Stackable.Returns(stackable);
            ItemDefinition = definition;

            var script = Substitute.For<IItemScript>();
            script.CanStackItem(Arg.Any<IItem>(), Arg.Any<IItem>(), Arg.Any<bool>()).Returns(call =>
            {
                var left = call.ArgAt<IItem>(0);
                var right = call.ArgAt<IItem>(1);
                return call.ArgAt<bool>(2) || left.ItemDefinition.Stackable && left.Id == right.Id;
            });
            ItemScript = script;
        }

        public IItem Clone() => Clone(Count);

        public IItem Clone(int newCount) => new TestItem(Id, newCount, ItemDefinition.Stackable);

        public bool Equals(IItem otherItem, bool ignoreCount = true) =>
            otherItem != null && Id == otherItem.Id && (ignoreCount || Count == otherItem.Count);

        public string? SerializeExtraData() => null;
    }
}
