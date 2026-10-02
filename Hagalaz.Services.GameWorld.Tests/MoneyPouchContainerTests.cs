using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    public void ContainerInterfaces_ExposeOnlyTheirIntendedItemCapabilities()
    {
        var ordinaryInterfaces = new[]
        {
            typeof(IInventoryContainer), typeof(IBankContainer), typeof(IRewardContainer),
            typeof(IFamiliarInventoryContainer), typeof(IShopStockContainer)
        };

        foreach (var containerInterface in ordinaryInterfaces)
        {
            Assert.AreEqual(typeof(IItemContainer), containerInterface.GetProperty("Items")?.PropertyType,
                containerInterface.Name);
        }

        Assert.IsNull(typeof(IEquipmentContainer).GetProperty("Items"));
        Assert.IsNull(typeof(IEquipmentContainer).GetMethod("OnUpdate"));
        Assert.IsNull(typeof(IMoneyPouchContainer).GetProperty("Items"));
        Assert.IsNotNull(typeof(IMoneyPouchContainer).GetProperty("Mutations"));
        Assert.IsNull(typeof(IMoneyPouchContainer).GetMethod("Contains"));
        Assert.IsNull(typeof(IMoneyPouchContainer).GetMethod("EnlistIn"));
        Assert.IsNull(typeof(IMoneyPouchContainer).GetMethod("TryStageAddExact"));
        Assert.IsNull(typeof(IMoneyPouchContainer).GetMethod("TryStageRemoveExact"));
        Assert.AreEqual(typeof(IMoneyPouchMutationBoundary), typeof(IMoneyPouchContainer).GetProperty("Mutations")!.PropertyType);
        var boundaryMethods = typeof(IMoneyPouchMutationBoundary).GetMethods();
        Assert.AreEqual(3, boundaryMethods.Length);
        CollectionAssert.AreEquivalent(new[] { "EnlistIn", "TryStageAddExact", "TryStageRemoveExact" },
            boundaryMethods.Select(method => method.Name).ToArray());
        Assert.IsNull(typeof(MoneyPouchContainer).GetMethod("EnlistIn", BindingFlags.Instance | BindingFlags.Public));
        Assert.IsNull(typeof(MoneyPouchContainer).GetMethod("TryStageAddExact", BindingFlags.Instance | BindingFlags.Public));
        Assert.IsNull(typeof(MoneyPouchContainer).GetMethod("TryStageRemoveExact", BindingFlags.Instance | BindingFlags.Public));
        Assert.IsNull(typeof(IEquipmentContainer).GetMethod("PublishCurrentState"));
        Assert.IsFalse(typeof(IMoneyPouchContainer).GetMethods().Any(method =>
            method.Name.Contains("Storage") || method.Name == "PublishChanges" ||
            method.GetParameters().Any(parameter => parameter.ParameterType == typeof(ItemContainerTransaction))));
        Assert.IsFalse(typeof(IItemContainer).GetMethods().SelectMany(method => method.GetParameters())
            .Any(parameter => parameter.ParameterType == typeof(ItemContainer)));
    }

    [TestMethod]
    public void HasCoins_WhenPouchCoinsSatisfyRequest_ReturnsTrue()
    {
        var scenario = CreateScenario(pouchCoins: 100, inventoryCoins: 0);

        var result = scenario.MoneyPouch.HasCoins(100);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void HasCoins_WhenInventoryCoinsSatisfyRequestWithEmptyPouch_ReturnsTrue()
    {
        var scenario = CreateScenario(pouchCoins: 0, inventoryCoins: 100);

        var result = scenario.MoneyPouch.HasCoins(100);

        Assert.IsTrue(result);
        Assert.IsFalse(scenario.MoneyPouch.HasCoins(101));
    }

    [TestMethod]
    public void HasCoins_WhenPouchAndInventoryCoinsTogetherSatisfyRequest_ReturnsTrue()
    {
        var scenario = CreateScenario(pouchCoins: 60, inventoryCoins: 40);

        var result = scenario.MoneyPouch.HasCoins(100);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void HasCoins_WhenCalledThroughMoneyPouchInterface_IncludesInventoryCoins()
    {
        var scenario = CreateScenario(pouchCoins: 25, inventoryCoins: 75);
        IMoneyPouchContainer pouch = scenario.MoneyPouch;

        Assert.IsTrue(pouch.HasCoins(100));
        Assert.AreEqual(25, pouch.Count);
        Assert.IsNull(typeof(IMoneyPouchContainer).GetProperty("Items"));
    }

    [TestMethod]
    public void HasCoins_WhenCombinedCoinBalanceIsInsufficient_ReturnsFalse()
    {
        var scenario = CreateScenario(pouchCoins: 60, inventoryCoins: 39);

        var result = scenario.MoneyPouch.HasCoins(100);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void HasCoins_WhenRequestEqualsCombinedCoinBalance_ReturnsTrue()
    {
        var scenario = CreateScenario(pouchCoins: 60, inventoryCoins: 40);

        var result = scenario.MoneyPouch.HasCoins(100);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void HasCoins_WhenCombinedCoinBalanceExceedsIntMaxValue_DoesNotOverflow()
    {
        var scenario = CreateScenario(pouchCoins: 1_500_000_000, inventoryCoins: 1_500_000_000);

        var result = scenario.MoneyPouch.HasCoins(int.MaxValue);

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

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void HasCoins_NonPositiveCount_ReturnsFalse(int count)
    {
        var scenario = CreateScenario(pouchCoins: 100, inventoryCoins: 100);

        Assert.IsFalse(scenario.MoneyPouch.HasCoins(count));
    }

    [TestMethod]
    public void TryAddExact_RollsBackPouchAndInventoryWhenLaterParticipantRejects()
    {
        var scenario = CreateScenario(pouchCoins: int.MaxValue - 2, inventoryCoins: 10);
        var eventManager = Substitute.For<IEventManager>();
        scenario.Owner.EventManager.Returns(eventManager);
        scenario.Owner.ClearReceivedCalls();
        var inventoryUpdates = 0;
        scenario.Inventory.OnUpdateAction = () => inventoryUpdates++;

        var fullContainer = new ItemContainer(StorageType.Normal, 1);
        Assert.IsTrue(fullContainer.Add(new ComposedTestItem(123, 1, stackable: false)));
        var transaction = new ItemContainerTransaction();
        scenario.MoneyPouch.Mutations.EnlistIn(transaction);
        transaction.Include(fullContainer.Mutations);

        Assert.IsFalse(transaction.TryExecute(tx =>
        {
            Assert.IsTrue(scenario.MoneyPouch.Mutations.TryStageAddExact(tx, 4));
            return tx.TryAddRange(fullContainer.Mutations, [new ComposedTestItem(124, 1, stackable: false)]);
        }));

        Assert.AreEqual(int.MaxValue - 2, scenario.MoneyPouch.Count);
        Assert.AreEqual(10, scenario.Inventory.Items.GetCountById(CoinId));
        Assert.AreEqual(1, fullContainer.TakenSlots);
        Assert.AreEqual(0, inventoryUpdates);
        eventManager.DidNotReceive().SendEvent(Arg.Any<MoneyPouchChangedEvent>());
        scenario.Owner.DidNotReceive().SendChatMessage(Arg.Any<string>());
    }

    [TestMethod]
    public void Add_WhenOverflowCannotFitInFullInventory_LeavesPouchAndInventoryUnchanged()
    {
        var scenario = CreateScenario(pouchCoins: int.MaxValue - 1, inventoryCoins: 0, inventoryCapacity: 1);
        Assert.IsTrue(scenario.Inventory.Items.Add(new ComposedTestItem(123, 1, stackable: false)));
        var eventManager = Substitute.For<IEventManager>();
        scenario.Owner.EventManager.Returns(eventManager);

        Assert.IsFalse(scenario.MoneyPouch.Add(2));

        Assert.AreEqual(int.MaxValue - 1, scenario.MoneyPouch.Count);
        Assert.AreEqual(1, scenario.Inventory.Items.GetCountById(123));
        eventManager.DidNotReceive().SendEvent(Arg.Any<MoneyPouchChangedEvent>());
    }

    [TestMethod]
    public void Remove_WhenRequestExceedsCombinedBalance_RemovesAvailableAmountAtomically()
    {
        var scenario = CreateScenario(pouchCoins: 5, inventoryCoins: 3);

        var removed = scenario.MoneyPouch.Remove(20);

        Assert.AreEqual(8, removed);
        Assert.AreEqual(0, scenario.MoneyPouch.Count);
        Assert.AreEqual(0, scenario.Inventory.Items.GetCountById(CoinId));
    }

    [TestMethod]
    public void Remove_WhenInventoryRemovalFailsAfterPouchStaging_RestoresBothStores()
    {
        var inventoryStorage = new ItemContainer(StorageType.Normal, 1);
        var inventoryItems = new AdvertisedCoinCountContainer(inventoryStorage);
        var inventory = Substitute.For<IInventoryContainer>();
        inventory.Items.Returns(inventoryItems);
        var owner = Substitute.For<ICharacter>();
        owner.Inventory.Returns(inventory);
        var eventManager = Substitute.For<IEventManager>();
        owner.EventManager.Returns(eventManager);
        var moneyPouch = new MoneyPouchContainer(owner, new ComposedTestItemBuilder());
        Assert.IsTrue(moneyPouch.Add(5));
        eventManager.ClearReceivedCalls();
        owner.ClearReceivedCalls();

        Assert.AreEqual(0, moneyPouch.Remove(10));

        Assert.AreEqual(5, moneyPouch.Count);
        Assert.AreEqual(0, inventoryStorage.GetCountById(CoinId));
        eventManager.DidNotReceive().SendEvent(Arg.Any<MoneyPouchChangedEvent>());
        owner.DidNotReceive().SendChatMessage(Arg.Any<string>());
    }

    [TestMethod]
    public void AddFromInventory_ClampsToRemainingPouchSpaceAndTransfersOneTransaction()
    {
        var scenario = CreateScenario(pouchCoins: int.MaxValue - 2, inventoryCoins: 5);

        Assert.IsTrue(scenario.MoneyPouch.AddFromInventory(10));

        Assert.AreEqual(int.MaxValue, scenario.MoneyPouch.Count);
        Assert.AreEqual(3, scenario.Inventory.Items.GetCountById(CoinId));
    }

    [TestMethod]
    public void AddFromInventory_WhenPouchStagingFails_RestoresRemovedInventoryCoins()
    {
        var inventory = new ComposedTestInventory(4);
        var owner = Substitute.For<ICharacter>();
        owner.Inventory.Returns(inventory);
        var eventManager = Substitute.For<IEventManager>();
        owner.EventManager.Returns(eventManager);
        var builder = new FailPouchStageItemBuilder();
        var pouch = new MoneyPouchContainer(owner, builder);
        Assert.IsTrue(pouch.Add(1));
        Assert.IsTrue(inventory.Items.Add(new ComposedTestItem(CoinId, 2, stackable: true)));
        eventManager.ClearReceivedCalls();
        owner.ClearReceivedCalls();
        var inventoryUpdates = 0;
        inventory.OnUpdateAction = () => inventoryUpdates++;

        Assert.IsFalse(pouch.AddFromInventory(2));

        Assert.AreEqual(1, pouch.Count);
        Assert.AreEqual(2, inventory.Items.GetCountById(CoinId));
        Assert.AreEqual(0, inventoryUpdates);
        eventManager.DidNotReceive().SendEvent(Arg.Any<MoneyPouchChangedEvent>());
        owner.DidNotReceive().SendChatMessage(Arg.Any<string>());
    }

    [TestMethod]
    public void MoveToInventory_WhenInventoryIsFull_LeavesPouchUnchanged()
    {
        var scenario = CreateScenario(pouchCoins: 5, inventoryCoins: 0, inventoryCapacity: 1);
        Assert.IsTrue(scenario.Inventory.Items.Add(new ComposedTestItem(123, 1, stackable: false)));

        Assert.IsFalse(scenario.MoneyPouch.MoveToInventory(5));

        Assert.AreEqual(5, scenario.MoneyPouch.Count);
        Assert.AreEqual(1, scenario.Inventory.Items.GetCountById(123));
        Assert.AreEqual(0, scenario.Inventory.Items.GetCountById(CoinId));
    }

    private sealed class FailPouchStageItemBuilder : IItemBuilder, IItemId, IItemOptional
    {
        private int _id;
        private int _count = 1;
        private int _buildCount;
        public IItemId Create() => this;
        public IItemOptional WithId(int id) { _id = id; return this; }
        public IItemOptional WithCount(int count) { _count = count; return this; }
        public IItemOptional WithExtraData(string data) => this;
        public IItem Build()
        {
            _buildCount++;
            var count = _buildCount == 4 ? int.MaxValue : _count;
            return new ComposedTestItem(_id, count, stackable: _id == CoinId);
        }
    }

    private sealed class AdvertisedCoinCountContainer(ItemContainer inner) : IItemContainer
    {
        public IItemContainerMutationBoundary Mutations => inner.Mutations;
        public StorageType Type => inner.Type;
        public int Capacity => inner.Capacity;
        public int FreeSlots => inner.FreeSlots;
        public int TakenSlots => inner.TakenSlots;
        public IItem? this[int index] => inner[index];
        public IEnumerator<IItem?> GetEnumerator() => inner.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        public bool Add(IItem item) => inner.Add(item);
        public bool Add(int slot, IItem item) => inner.Add(slot, item);
        public IItem? GetById(int id) => inner.GetById(id);
        public int Remove(IItem item, int preferredSlot = -1, bool update = true) => inner.Remove(item, preferredSlot, update);
        public bool TryRemoveExact(IItem item, int preferredSlot = -1) => inner.TryRemoveExact(item, preferredSlot);
        public void Replace(int slot, IItem item) => inner.Replace(slot, item);
        public void Swap(int fromSlot, int toSlot) => inner.Swap(fromSlot, toSlot);
        public void Move(int fromSlot, int toSlot) => inner.Move(fromSlot, toSlot);
        public bool AddRange(IEnumerable<IItem?> items) => inner.AddRange(items);
        public bool Contains(int id, int count) => inner.Contains(id, count);
        public bool Contains(int id) => inner.Contains(id);
        public int GetCount(IItem item) => inner.GetCount(item);
        public int GetCountById(int id) => id == CoinId ? 10 : inner.GetCountById(id);
        public int GetInstanceSlot(IItem instance) => inner.GetInstanceSlot(instance);
        public void Sort() => inner.Sort();
        public int GetSlotByItem(IItem item, bool ignoreCount = true) => inner.GetSlotByItem(item, ignoreCount);
        public bool HasSpaceFor(IItem item) => inner.HasSpaceFor(item);
        public bool HasSpaceForRange(IEnumerable<IItem?> items) => inner.HasSpaceForRange(items);
        public void Clear(bool update) => inner.Clear(update);
    }

    private static void AssertTradeStoragePublishesAfterFinalState(MoneyPouchScenario scenario, int movedCoins,
        int previousPouchCount, int expectedPouchCount, int expectedInventoryCoins,
        Func<IMoneyPouchContainer, int, bool> operation)
    {
        var eventManager = Substitute.For<IEventManager>();
        scenario.Owner.EventManager.Returns(eventManager);
        scenario.Inventory.OnUpdateAction = () =>
        {
            Assert.AreEqual(expectedPouchCount, scenario.MoneyPouch.Count);
            Assert.AreEqual(expectedInventoryCoins, scenario.Inventory.Items.GetCountById(CoinId));
        };
        var moneyPouchEventObservedFinalState = false;
        eventManager
            .When(manager => manager.SendEvent(Arg.Any<MoneyPouchChangedEvent>()))
            .Do(call =>
            {
                var changedEvent = call.Arg<MoneyPouchChangedEvent>();
                Assert.AreEqual(previousPouchCount, changedEvent.PreviousCount);
                Assert.AreEqual(expectedPouchCount, changedEvent.Count);
                Assert.AreEqual(expectedInventoryCoins, scenario.Inventory.Items.GetCountById(CoinId));
                moneyPouchEventObservedFinalState = true;
            });

        Assert.IsTrue(operation(scenario.MoneyPouch, movedCoins));
        Assert.IsTrue(moneyPouchEventObservedFinalState);
    }

    private static MoneyPouchScenario CreateScenario(int pouchCoins, int inventoryCoins, int inventoryCapacity = 10)
    {
        var inventory = new ComposedTestInventory(inventoryCapacity);
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
