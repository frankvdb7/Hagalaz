using System.Collections.Generic;
using System.Threading;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Common.Events.Character;
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
    public void Contains_WhenCalledThroughMoneyPouchInterface_IncludesInventoryCoins()
    {
        var scenario = CreateScenario(pouchCoins: 25, inventoryCoins: 75);
        IMoneyPouchContainer pouch = scenario.MoneyPouch;
        IContainer<IItem?> itemContainer = scenario.MoneyPouch.Items;

        Assert.IsTrue(pouch.Contains(CoinId, 100));
        Assert.AreEqual(25, itemContainer[0]!.Count);
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

    [TestMethod]
    public void TryAddExact_PublishesOnlyAfterPouchAndInventoryReachFinalState()
    {
        var scenario = CreateScenario(pouchCoins: int.MaxValue - 1, inventoryCoins: 0);
        AssertTradeStoragePublishesAfterFinalState(scenario, 2, int.MaxValue - 1, int.MaxValue, 1,
            static (pouch, count) => pouch.TryAddExact(count));
    }

    [TestMethod]
    public void TryRemoveExact_PublishesOnlyAfterPouchAndInventoryReachFinalState()
    {
        var scenario = CreateScenario(pouchCoins: 10, inventoryCoins: 5);
        AssertTradeStoragePublishesAfterFinalState(scenario, 12, 10, 0, 3,
            static (pouch, count) => pouch.TryRemoveExact(count));
    }

    private static void AssertTradeStoragePublishesAfterFinalState(MoneyPouchScenario scenario, int movedCoins,
        int previousPouchCount, int expectedPouchCount, int expectedInventoryCoins,
        Func<IMoneyPouchContainer, int, bool> operation)
    {
        var eventManager = Substitute.For<IEventManager>();
        scenario.Owner.EventManager.Returns(eventManager);
        scenario.Inventory.OnUpdateAction = () =>
        {
            Assert.IsFalse(Monitor.IsEntered(((IItemContainerStorageOwner)scenario.Inventory.Items).Storage.MutationLock));
            Assert.IsFalse(Monitor.IsEntered(((IItemContainerStorageOwner)scenario.MoneyPouch).Storage.MutationLock));
            Assert.AreEqual(expectedPouchCount, scenario.MoneyPouch.Count);
            Assert.AreEqual(expectedInventoryCoins, scenario.Inventory.Items.GetCountById(CoinId));
        };
        var moneyPouchEventObservedFinalState = false;
        eventManager
            .When(manager => manager.SendEvent(Arg.Any<MoneyPouchChangedEvent>()))
            .Do(call =>
            {
                var changedEvent = call.Arg<MoneyPouchChangedEvent>();
                Assert.IsFalse(Monitor.IsEntered(((IItemContainerStorageOwner)scenario.Inventory.Items).Storage.MutationLock));
                Assert.IsFalse(Monitor.IsEntered(((IItemContainerStorageOwner)scenario.MoneyPouch).Storage.MutationLock));
                Assert.AreEqual(previousPouchCount, changedEvent.PreviousCount);
                Assert.AreEqual(expectedPouchCount, changedEvent.Count);
                Assert.AreEqual(expectedInventoryCoins, scenario.Inventory.Items.GetCountById(CoinId));
                moneyPouchEventObservedFinalState = true;
            });

        Assert.IsTrue(operation(scenario.MoneyPouch, movedCoins));
        Assert.IsTrue(moneyPouchEventObservedFinalState);
    }

    private static MoneyPouchScenario CreateScenario(int pouchCoins, int inventoryCoins)
    {
        var inventory = new ComposedTestInventory(10);
        if (inventoryCoins > 0)
        {
            Assert.IsTrue(inventory.Items.Add(new ComposedTestItem(CoinId, inventoryCoins, stackable: true)));
        }

        var character = Substitute.For<ICharacter>();
        character.Inventory.Returns(inventory);

        var moneyPouch = new MoneyPouchContainer(character, new ComposedTestItemBuilder());
        if (pouchCoins > 0)
        {
            Assert.IsTrue(moneyPouch.Add(pouchCoins));
        }

        return new MoneyPouchScenario(moneyPouch, inventory, character);
    }

    private sealed record MoneyPouchScenario(MoneyPouchContainer MoneyPouch, ComposedTestInventory Inventory,
        ICharacter Owner);


}
