using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Features.Shops;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Common.Events;
using Hagalaz.Services.GameWorld.Logic.Shops;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class ShopStockContainerTests
{
    private const int CoinId = 995;
    private const int ItemId = 1000;

    [TestMethod]
    public void BuyFromShop_WhenPlayerHasOneCoinForTenThousandCoinItem_RejectsAndPreservesCoin()
    {
        var scenario = CreateScenario(cost: 10_000, pouchCoins: 1);

        var result = scenario.Stock.BuyFromShop(scenario.Character, scenario.StockItem, 1);

        Assert.IsFalse(result);
        Assert.AreEqual(1, scenario.MoneyPouch.Count);
        Assert.AreEqual(0, scenario.Inventory.GetCountById(ItemId));
    }

    [TestMethod]
    public void BuyFromShop_WhenPlayerHasCostMinusOneCoins_RejectsAndPreservesAllCoins()
    {
        const int cost = 10_000;
        var scenario = CreateScenario(cost, pouchCoins: cost - 1);

        var result = scenario.Stock.BuyFromShop(scenario.Character, scenario.StockItem, 1);

        Assert.IsFalse(result);
        Assert.AreEqual(cost - 1, scenario.MoneyPouch.Count);
        Assert.AreEqual(0, scenario.Inventory.GetCountById(ItemId));
    }

    [TestMethod]
    public void BuyFromShop_WhenPlayerHasExactCoinCost_SucceedsAndRemovesExactCost()
    {
        const int cost = 10_000;
        var scenario = CreateScenario(cost, pouchCoins: cost);

        var result = scenario.Stock.BuyFromShop(scenario.Character, scenario.StockItem, 1);

        Assert.IsTrue(result);
        Assert.AreEqual(0, GetTotalCoins(scenario));
        Assert.AreEqual(1, scenario.Inventory.GetCountById(ItemId));
    }

    [TestMethod]
    public void BuyFromShop_WhenPlayerHasMoreThanCoinCost_RemovesOnlyExactCost()
    {
        const int cost = 10_000;
        var scenario = CreateScenario(cost, pouchCoins: cost + 1);

        var result = scenario.Stock.BuyFromShop(scenario.Character, scenario.StockItem, 1);

        Assert.IsTrue(result);
        Assert.AreEqual(1, GetTotalCoins(scenario));
        Assert.AreEqual(1, scenario.Inventory.GetCountById(ItemId));
    }

    [TestMethod]
    public void BuyFromShop_WhenCoinsAreSplitBetweenPouchAndInventory_RemovesExactCost()
    {
        const int cost = 10_000;
        var scenario = CreateScenario(cost, pouchCoins: 4_000, inventoryCurrency: 6_000);

        var result = scenario.Stock.BuyFromShop(scenario.Character, scenario.StockItem, 1);

        Assert.IsTrue(result);
        Assert.AreEqual(0, GetTotalCoins(scenario));
        Assert.AreEqual(1, scenario.Inventory.GetCountById(ItemId));
    }

    [TestMethod]
    public void BuyFromShop_WhenBuyingSampleStock_DoesNotChargeCurrency()
    {
        var scenario = CreateScenario(cost: 10_000, sampleStock: true);

        var result = scenario.Stock.BuyFromShop(scenario.Character, scenario.StockItem, 1);

        Assert.IsTrue(result);
        Assert.AreEqual(0, GetTotalCoins(scenario));
        Assert.AreEqual(1, scenario.Inventory.GetCountById(ItemId));
    }

    [TestMethod]
    public void BuyFromShop_WhenUsingNonCoinCurrency_RemovesExactInventoryCost()
    {
        const int currencyId = 2000;
        const int cost = 5;
        var scenario = CreateScenario(cost, currencyId, inventoryCurrency: cost);

        var result = scenario.Stock.BuyFromShop(scenario.Character, scenario.StockItem, 1);

        Assert.IsTrue(result);
        Assert.AreEqual(0, scenario.Inventory.GetCountById(currencyId));
        Assert.AreEqual(1, scenario.Inventory.GetCountById(ItemId));
    }

    [TestMethod]
    public void BuyFromShop_WhenNonCoinCurrencyIsUnderfunded_RejectsAndPreservesCurrency()
    {
        const int currencyId = 2000;
        const int cost = 5;
        var scenario = CreateScenario(cost, currencyId, inventoryCurrency: cost - 1);

        var result = scenario.Stock.BuyFromShop(scenario.Character, scenario.StockItem, 1);

        Assert.IsFalse(result);
        Assert.AreEqual(cost - 1, scenario.Inventory.GetCountById(currencyId));
        Assert.AreEqual(0, scenario.Inventory.GetCountById(ItemId));
    }

    private static ShopScenario CreateScenario(
        int cost,
        int currencyId = CoinId,
        int pouchCoins = 0,
        int inventoryCurrency = 0,
        bool sampleStock = false)
    {
        var inventory = new TestInventory(10);
        if (inventoryCurrency > 0)
        {
            Assert.IsTrue(inventory.Add(new TestItem(currencyId, inventoryCurrency, stackable: true)));
        }

        var character = Substitute.For<ICharacter>();
        character.Inventory.Returns(inventory);
        character.EventManager.Returns(Substitute.For<IEventManager>());

        var itemBuilder = new TestItemBuilder();
        var moneyPouch = new MoneyPouchContainer(character, itemBuilder);
        if (pouchCoins > 0)
        {
            Assert.IsTrue(moneyPouch.Add(pouchCoins));
        }

        character.MoneyPouch.Returns(moneyPouch);

        var shop = Substitute.For<IShop>();
        shop.CurrencyId.Returns(currencyId);
        shop.GeneralStore.Returns(true);
        shop.GetBuyValue(Arg.Any<IItem>()).Returns(cost);

        var itemService = Substitute.For<IItemService>();
        var currencyDefinition = Substitute.For<IItemDefinition>();
        currencyDefinition.Name.Returns(currencyId == CoinId ? "Coins" : "Tokens");
        itemService.FindItemDefinitionById(currencyId).Returns(currencyDefinition);

        var stockItem = new TestItem(ItemId, 1, stackable: true);
        var stock = new ShopStockContainer(
            shop,
            itemService,
            itemBuilder,
            sampleStock,
            StorageType.AlwaysStack,
            1,
            [stockItem],
            Substitute.For<IEventManager>());

        return new ShopScenario(character, inventory, moneyPouch, stock, stockItem);
    }

    private static int GetTotalCoins(ShopScenario scenario) =>
        scenario.MoneyPouch.Count + scenario.Inventory.GetCountById(CoinId);

    private sealed record ShopScenario(
        ICharacter Character,
        IInventoryContainer Inventory,
        IMoneyPouchContainer MoneyPouch,
        IShopStockContainer Stock,
        IItem StockItem);

    private sealed class TestInventory : TestItemContainer, IInventoryContainer
    {
        public TestInventory(int capacity) : base(StorageType.Normal, capacity) { }

        public bool DropItem(IItem item) => false;
    }

    private class TestItemContainer : TradeItemContainer
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
            ItemDefinition = Substitute.For<IItemDefinition>();
            ItemDefinition.Stackable.Returns(stackable);
            ItemDefinition.Noted.Returns(false);

            ItemScript = Substitute.For<IItemScript>();
            ItemScript.CanBuyItem(Arg.Any<IItem>(), Arg.Any<ICharacter>()).Returns(true);
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

        public IItem Clone() => new TestItem(Id, Count, ItemDefinition, ItemScript);

        public IItem Clone(int newCount) => new TestItem(Id, newCount, ItemDefinition, ItemScript);

        public bool Equals(IItem otherItem, bool ignoreCount = true) =>
            otherItem != null && Id == otherItem.Id && (ignoreCount || Count == otherItem.Count);

        public string? SerializeExtraData() => null;
    }
}
