using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Minigames.DuelArena;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Minigames.DuelArena;

[TestClass]
public sealed class DuelStakeExchangeTests
{
    [TestMethod]
    public void TryStakeInventoryItem_TransfersExactItem()
    {
        var scenario = CreateScenario();
        var item = new TestItem(1000, 2);
        Assert.IsTrue(scenario.InventoryItems.Add(item));

        Assert.IsTrue(scenario.Exchange.TryStakeInventoryItem(scenario.Character, scenario.Stake, item, 2, 0));

        Assert.AreEqual(0, scenario.InventoryItems.GetCountById(1000));
        Assert.AreEqual(2, scenario.Stake.GetCountById(1000));
    }

    [TestMethod]
    public void TryReturnItemToInventory_WhenInventoryCannotAcceptItem_LeavesStakeUnchanged()
    {
        var scenario = CreateScenario(inventoryCapacity: 1);
        var staked = new TestItem(1000, 1);
        Assert.IsTrue(scenario.Stake.Add(staked));
        Assert.IsTrue(scenario.InventoryItems.Add(new TestItem(2000, 1)));

        Assert.IsFalse(scenario.Exchange.TryReturnItemToInventory(scenario.Character, scenario.Stake, staked, 1, 0));

        Assert.AreEqual(1, scenario.Stake.GetCountById(1000));
        Assert.AreEqual(1, scenario.InventoryItems.GetCountById(2000));
    }

    [TestMethod]
    public void TryStakePouchCoins_WhenStakeIsFull_LeavesPouchUnchanged()
    {
        var scenario = CreateScenario(stakeCapacity: 1, pouchCoins: 50);
        Assert.IsTrue(scenario.Stake.Add(new TestItem(1000, 1)));

        Assert.IsFalse(scenario.Exchange.TryStakePouchCoins(scenario.Character, scenario.Stake, 20));

        Assert.AreEqual(50, scenario.Pouch.Count);
        Assert.AreEqual(1, scenario.Stake.GetCountById(1000));
    }

    [TestMethod]
    public void TryStakePouchCoins_WhenBalanceIsBelowRequest_StakesAvailableBalance()
    {
        var scenario = CreateScenario(pouchCoins: 5);
        Assert.IsTrue(scenario.InventoryItems.Add(new TestItem(995, 3, stackable: true)));

        Assert.IsTrue(scenario.Exchange.TryStakePouchCoins(scenario.Character, scenario.Stake, 20));

        Assert.AreEqual(0, scenario.Pouch.Count);
        Assert.AreEqual(0, scenario.InventoryItems.GetCountById(995));
        Assert.AreEqual(8, scenario.Stake.GetCountById(995));
    }

    [TestMethod]
    public void TryReturnCoinsToPouch_WhenPouchAndInventoryAreFull_LeavesStakeUnchanged()
    {
        var scenario = CreateScenario(pouchCoins: int.MaxValue, inventoryCapacity: 1);
        var stakeCoins = new TestItem(995, 1, stackable: true);
        Assert.IsTrue(scenario.Stake.Add(stakeCoins));
        Assert.IsTrue(scenario.InventoryItems.Add(new TestItem(1000, 1)));

        Assert.IsFalse(scenario.Exchange.TryReturnCoinsToPouch(scenario.Character, scenario.Stake, stakeCoins, 0));

        Assert.AreEqual(1, scenario.Stake.GetCountById(995));
        Assert.AreEqual(int.MaxValue, scenario.Pouch.Count);
        Assert.AreEqual(1, scenario.InventoryItems.GetCountById(1000));
    }

    [TestMethod]
    public void TryRefundBoth_WhenSecondInventoryRejects_LeavesBothStakesAndDestinationsUnchanged()
    {
        var first = CreateScenario(pouchCoins: 10);
        var second = CreateScenario(inventoryCapacity: 1, pouchCoins: 20);
        Assert.IsTrue(first.Stake.Add(new TestItem(995, 3, stackable: true)));
        Assert.IsTrue(first.Stake.Add(new TestItem(1001, 1)));
        Assert.IsTrue(second.Stake.Add(new TestItem(1002, 1)));
        Assert.IsTrue(second.InventoryItems.Add(new TestItem(2000, 1)));

        Assert.IsFalse(first.Exchange.TryRefundBoth(first.Character, first.Stake, second.Character, second.Stake));

        Assert.AreEqual(3, first.Stake.GetCountById(995));
        Assert.AreEqual(1, first.Stake.GetCountById(1001));
        Assert.AreEqual(10, first.Pouch.Count);
        Assert.AreEqual(0, first.InventoryItems.GetCountById(1001));
        Assert.AreEqual(1, second.Stake.GetCountById(1002));
        Assert.AreEqual(20, second.Pouch.Count);
        Assert.AreEqual(1, second.InventoryItems.GetCountById(2000));
    }

    [TestMethod]
    public void TryRefundBoth_WhenEveryDestinationAccepts_RefundsItemsAndCoinsTogether()
    {
        var first = CreateScenario(pouchCoins: 10);
        var second = CreateScenario(pouchCoins: 20);
        Assert.IsTrue(first.Stake.Add(new TestItem(995, 3, stackable: true)));
        Assert.IsTrue(first.Stake.Add(new TestItem(1001, 1)));
        Assert.IsTrue(second.Stake.Add(new TestItem(1002, 2)));

        Assert.IsTrue(first.Exchange.TryRefundBoth(first.Character, first.Stake, second.Character, second.Stake));

        Assert.AreEqual(0, first.Stake.TakenSlots);
        Assert.AreEqual(13, first.Pouch.Count);
        Assert.AreEqual(1, first.InventoryItems.GetCountById(1001));
        Assert.AreEqual(0, second.Stake.TakenSlots);
        Assert.AreEqual(20, second.Pouch.Count);
        Assert.AreEqual(2, second.InventoryItems.GetCountById(1002));
    }

    private static Scenario CreateScenario(int inventoryCapacity = 4, int stakeCapacity = 4, int pouchCoins = 0)
    {
        var inventoryItems = new ItemContainer(StorageType.Normal, (short)inventoryCapacity);
        var inventory = Substitute.For<IInventoryContainer>();
        inventory.Items.Returns(inventoryItems);
        var character = Substitute.For<ICharacter>();
        character.Inventory.Returns(inventory);
        character.EventManager.Returns(Substitute.For<IEventManager>());
        var itemBuilder = new TestItemBuilder((id, count) => new TestItem(id, count, stackable: id == 995));
        var pouch = new MoneyPouchContainer(character, itemBuilder);
        if (pouchCoins > 0) Assert.IsTrue(pouch.Add(pouchCoins));
        character.MoneyPouch.Returns(pouch);
        return new Scenario(character, inventoryItems, pouch, new ItemContainer(StorageType.Normal, (short)stakeCapacity),
            new DuelStakeExchange(itemBuilder));
    }

    private sealed record Scenario(ICharacter Character, ItemContainer InventoryItems, MoneyPouchContainer Pouch,
        ItemContainer Stake, DuelStakeExchange Exchange);

    private sealed class TestItem : IItem
    {
        public TestItem(int id, int count, bool stackable = false)
        {
            Id = id;
            Count = count;
            ItemDefinition = Substitute.For<IItemDefinition>();
            ItemDefinition.Stackable.Returns(stackable);
            ItemScript = Substitute.For<IItemScript>();
            ItemScript.CanStackItem(Arg.Any<IItem>(), Arg.Any<IItem>(), Arg.Any<bool>()).Returns(call =>
            {
                var first = call.ArgAt<IItem>(0);
                var second = call.ArgAt<IItem>(1);
                return first.Id == second.Id && (call.ArgAt<bool>(2) || first.ItemDefinition.Stackable);
            });
            EquipmentDefinition = Substitute.For<IEquipmentDefinition>();
            EquipmentScript = Substitute.For<IEquipmentScript>();
        }

        public int Id { get; }
        public int Count { get; set; }
        public long[] ExtraData => [];
        public string Name => $"Item {Id}";
        public IItemDefinition ItemDefinition { get; }
        public IEquipmentDefinition EquipmentDefinition { get; }
        public IItemScript ItemScript { get; }
        public IEquipmentScript EquipmentScript { get; }
        public IItem Clone() => new TestItem(Id, Count, ItemDefinition.Stackable);
        public IItem Clone(int newCount) => new TestItem(Id, newCount, ItemDefinition.Stackable);
        public bool Equals(IItem otherItem, bool ignoreCount = true) => otherItem != null && Id == otherItem.Id &&
            (ignoreCount || Count == otherItem.Count) && ExtraData.SequenceEqual(otherItem.ExtraData);
        public string? SerializeExtraData() => null;
    }
}
