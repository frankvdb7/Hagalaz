using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class MoneyPouchContainerTests
{
    private const int CoinId = 995;

    [TestMethod]
    public void Contains_WhenPouchCoinsSatisfyRequest_ReturnsTrue()
    {
        var scenario = CreateScenario(pouchCoins: 100, inventoryCoins: 0);

        var result = scenario.MoneyPouch.Contains(CoinId, 100);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Contains_WhenInventoryCoinsSatisfyRequestWithEmptyPouch_ReturnsTrue()
    {
        var scenario = CreateScenario(pouchCoins: 0, inventoryCoins: 100);

        var result = scenario.MoneyPouch.Contains(CoinId, 100);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Contains_WhenPouchAndInventoryCoinsTogetherSatisfyRequest_ReturnsTrue()
    {
        var scenario = CreateScenario(pouchCoins: 40, inventoryCoins: 60);

        var result = scenario.MoneyPouch.Contains(CoinId, 100);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Contains_WhenCombinedCoinBalanceIsInsufficient_ReturnsFalse()
    {
        var scenario = CreateScenario(pouchCoins: 40, inventoryCoins: 59);

        var result = scenario.MoneyPouch.Contains(CoinId, 100);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Contains_WhenRequestEqualsCombinedCoinBalance_ReturnsTrue()
    {
        var scenario = CreateScenario(pouchCoins: 40, inventoryCoins: 60);

        var result = scenario.MoneyPouch.Contains(CoinId, 100);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Contains_WhenCombinedCoinBalanceExceedsIntMaxValue_DoesNotOverflow()
    {
        var scenario = CreateScenario(pouchCoins: 1_500_000_000, inventoryCoins: 1_500_000_000);

        var result = scenario.MoneyPouch.Contains(CoinId, int.MaxValue);

        Assert.IsTrue(result);
    }

    private static MoneyPouchScenario CreateScenario(int pouchCoins, int inventoryCoins)
    {
        var inventory = new TestInventory(10);
        if (inventoryCoins > 0)
        {
            Assert.IsTrue(inventory.Add(new TestItem(CoinId, inventoryCoins, stackable: true)));
        }

        var character = Substitute.For<ICharacter>();
        character.Inventory.Returns(inventory);

        var moneyPouch = new MoneyPouchContainer(character, new TestItemBuilder());
        if (pouchCoins > 0)
        {
            Assert.IsTrue(moneyPouch.Add(pouchCoins));
        }

        return new MoneyPouchScenario(moneyPouch, inventory);
    }

    private sealed record MoneyPouchScenario(IMoneyPouchContainer MoneyPouch, IInventoryContainer Inventory);

    private sealed class TestInventory : TestItemContainer, IInventoryContainer
    {
        public TestInventory(int capacity) : base(StorageType.Normal, capacity) { }

        public bool DropItem(IItem item) => false;
    }

    private abstract class TestItemContainer : TradeItemContainer
    {
        protected TestItemContainer(StorageType type, int capacity) : base(type, capacity) { }

        public override void OnUpdate(HashSet<int>? slots = null) { }
    }

    private sealed class TestItemBuilder : IItemBuilder, IItemId, IItemOptional
    {
        private int _id;
        private int _count = 1;

        public IItemId Create() => this;

        public IItemOptional WithId(int id)
        {
            _id = id;
            return this;
        }

        public IItemOptional WithCount(int count)
        {
            _count = count;
            return this;
        }

        public IItemOptional WithExtraData(string data) => this;

        public IItem Build() => new TestItem(_id, _count, stackable: true);
    }

    private sealed class TestItem : IItem
    {
        public TestItem(int id, int count, bool stackable)
        {
            Id = id;
            Count = count;
            ItemDefinition = Substitute.For<IItemDefinition>();
            ItemDefinition.Stackable.Returns(stackable);
            ItemDefinition.Noted.Returns(false);
            ItemScript = Substitute.For<IItemScript>();
            ItemScript.CanStackItem(Arg.Any<IItem>(), Arg.Any<IItem>(), Arg.Any<bool>()).Returns(callInfo =>
            {
                var left = callInfo.ArgAt<IItem>(0);
                var right = callInfo.ArgAt<IItem>(1);
                return callInfo.ArgAt<bool>(2) || left.Id == right.Id && left.ItemDefinition.Stackable;
            });
        }

        private TestItem(int id, int count, IItemDefinition definition, IItemScript script)
        {
            Id = id;
            Count = count;
            ItemDefinition = definition;
            ItemScript = script;
        }

        public int Id { get; }
        public int Count { get; set; }
        public string Name => $"Test item {Id}";
        public IItemDefinition ItemDefinition { get; }
        public IEquipmentDefinition EquipmentDefinition { get; } = Substitute.For<IEquipmentDefinition>();
        public IItemScript ItemScript { get; }
        public IEquipmentScript EquipmentScript { get; } = Substitute.For<IEquipmentScript>();
        public long[] ExtraData => [];

        public IItem Clone() => new TestItem(Id, Count, ItemDefinition, ItemScript);

        public IItem Clone(int newCount) => new TestItem(Id, newCount, ItemDefinition, ItemScript);

        public bool Equals(IItem otherItem, bool ignoreCount = true) =>
            otherItem != null && Id == otherItem.Id && (ignoreCount || Count == otherItem.Count);

        public string? SerializeExtraData() => null;
    }
}
