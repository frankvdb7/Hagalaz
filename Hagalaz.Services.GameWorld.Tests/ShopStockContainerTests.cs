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
        Assert.AreEqual(0, scenario.Inventory.Items.GetCountById(ItemId));
    }

    [TestMethod]
    public void BuyFromShop_WhenPlayerHasCostMinusOneCoins_RejectsAndPreservesAllCoins()
    {
        const int cost = 10_000;
        var scenario = CreateScenario(cost, pouchCoins: cost - 1);

        var result = scenario.Stock.BuyFromShop(scenario.Character, scenario.StockItem, 1);

        Assert.IsFalse(result);
        Assert.AreEqual(cost - 1, scenario.MoneyPouch.Count);
        Assert.AreEqual(0, scenario.Inventory.Items.GetCountById(ItemId));
    }

    [TestMethod]
    public void BuyFromShop_WhenPlayerHasExactCoinCost_SucceedsAndRemovesExactCost()
    {
        const int cost = 10_000;
        var scenario = CreateScenario(cost, pouchCoins: cost);

        var result = scenario.Stock.BuyFromShop(scenario.Character, scenario.StockItem, 1);

        Assert.IsTrue(result);
        Assert.AreEqual(0, GetTotalCoins(scenario));
        Assert.AreEqual(1, scenario.Inventory.Items.GetCountById(ItemId));
    }

    [TestMethod]
    public void BuyFromShop_WhenPlayerHasMoreThanCoinCost_RemovesOnlyExactCost()
    {
        const int cost = 10_000;
        var scenario = CreateScenario(cost, pouchCoins: cost + 1);

        var result = scenario.Stock.BuyFromShop(scenario.Character, scenario.StockItem, 1);

        Assert.IsTrue(result);
        Assert.AreEqual(1, GetTotalCoins(scenario));
        Assert.AreEqual(1, scenario.Inventory.Items.GetCountById(ItemId));
    }

    [TestMethod]
    public void BuyFromShop_WhenCoinsAreSplitBetweenPouchAndInventory_RemovesExactCost()
    {
        const int cost = 10_000;
        var scenario = CreateScenario(cost, pouchCoins: 4_000, inventoryCurrency: 6_000);

        var result = scenario.Stock.BuyFromShop(scenario.Character, scenario.StockItem, 1);

        Assert.IsTrue(result);
        Assert.AreEqual(0, GetTotalCoins(scenario));
        Assert.AreEqual(1, scenario.Inventory.Items.GetCountById(ItemId));
    }

    [TestMethod]
    public void BuyFromShop_WhenSplitCoinsAreUnderfunded_RejectsAndPreservesBothBalances()
    {
        const int cost = 10_000;
        var scenario = CreateScenario(cost, pouchCoins: 4_000, inventoryCurrency: 5_999);

        var result = scenario.Stock.BuyFromShop(scenario.Character, scenario.StockItem, 1);

        Assert.IsFalse(result);
        Assert.AreEqual(4_000, scenario.MoneyPouch.Count);
        Assert.AreEqual(5_999, scenario.Inventory.Items.GetCountById(CoinId));
        Assert.AreEqual(0, scenario.Inventory.Items.GetCountById(ItemId));
    }

    [TestMethod]
    public void BuyFromShop_WhenBuyingSampleStock_DoesNotChargeCurrency()
    {
        var scenario = CreateScenario(cost: 10_000, sampleStock: true);

        var result = scenario.Stock.BuyFromShop(scenario.Character, scenario.StockItem, 1);

        Assert.IsTrue(result);
        Assert.AreEqual(0, GetTotalCoins(scenario));
        Assert.AreEqual(1, scenario.Inventory.Items.GetCountById(ItemId));
    }

    [TestMethod]
    public void BuyFromShop_WhenUsingNonCoinCurrency_RemovesExactInventoryCost()
    {
        const int currencyId = 2000;
        const int cost = 5;
        var scenario = CreateScenario(cost, currencyId, inventoryCurrency: cost);

        var result = scenario.Stock.BuyFromShop(scenario.Character, scenario.StockItem, 1);

        Assert.IsTrue(result);
        Assert.AreEqual(0, scenario.Inventory.Items.GetCountById(currencyId));
        Assert.AreEqual(1, scenario.Inventory.Items.GetCountById(ItemId));
    }

    [TestMethod]
    public void BuyFromShop_WhenNonCoinCurrencyIsUnderfunded_RejectsAndPreservesCurrency()
    {
        const int currencyId = 2000;
        const int cost = 5;
        var scenario = CreateScenario(cost, currencyId, inventoryCurrency: cost - 1);

        var result = scenario.Stock.BuyFromShop(scenario.Character, scenario.StockItem, 1);

        Assert.IsFalse(result);
        Assert.AreEqual(cost - 1, scenario.Inventory.Items.GetCountById(currencyId));
        Assert.AreEqual(0, scenario.Inventory.Items.GetCountById(ItemId));
    }

    [TestMethod]
    public void NormalizeStock_RestocksDepletedOriginalItem()
    {
        var scenario = CreateScenario(cost: 0);
        var depletedOriginal = scenario.StockItem.Clone(0);
        var replacement = new IItem[scenario.Stock.Items.Capacity];
        replacement[0] = depletedOriginal;
        scenario.Stock.SetItems(replacement, update: false);

        scenario.Stock.NormalizeStock();

        Assert.AreSame(depletedOriginal, scenario.Stock.Items[0]);
        Assert.AreEqual(1, scenario.Stock.Items[0]!.Count);
    }

    [TestMethod]
    public void NormalizeStock_RetainsDepletedPlayerStockAtZeroCount()
    {
        var scenario = CreateScenario(cost: 0);
        var playerStock = new ComposedTestItem(2001, 1, stackable: true);
        var replacement = new IItem[scenario.Stock.Items.Capacity];
        replacement[0] = playerStock;
        scenario.Stock.SetItems(replacement, update: false);

        scenario.Stock.NormalizeStock();

        Assert.AreSame(playerStock, scenario.Stock.Items[0]);
        Assert.AreEqual(0, scenario.Stock.Items[0]!.Count);
    }

    private static ShopScenario CreateScenario(
        int cost,
        int currencyId = CoinId,
        int pouchCoins = 0,
        int inventoryCurrency = 0,
        bool sampleStock = false)
    {
        var inventory = new ComposedTestInventory(10);
        if (inventoryCurrency > 0)
        {
            Assert.IsTrue(inventory.Items.Add(new ComposedTestItem(currencyId, inventoryCurrency, stackable: true)));
        }

        var character = Substitute.For<ICharacter>();
        character.Inventory.Returns(inventory);
        character.EventManager.Returns(Substitute.For<IEventManager>());

        var itemBuilder = new ComposedTestItemBuilder();
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

        var stockItem = new ComposedTestItem(ItemId, 1, stackable: true);
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
        scenario.MoneyPouch.Count + scenario.Inventory.Items.GetCountById(CoinId);

    private sealed record ShopScenario(
        ICharacter Character,
        IInventoryContainer Inventory,
        IMoneyPouchContainer MoneyPouch,
        IShopStockContainer Stock,
        IItem StockItem);


}
