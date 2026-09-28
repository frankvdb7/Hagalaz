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
    public void AddForTrade_PublishesOnlyAfterPouchAndInventoryReachFinalState()
    {
        var scenario = CreateScenario(pouchCoins: int.MaxValue - 1, inventoryCoins: 0);
        AssertTradeStoragePublishesAfterFinalState(scenario, 2, int.MaxValue - 1, int.MaxValue, 1,
            static (pouch, count) => pouch.AddForTrade(count));
    }

    [TestMethod]
    public void RemoveForTrade_PublishesOnlyAfterPouchAndInventoryReachFinalState()
    {
        var scenario = CreateScenario(pouchCoins: 10, inventoryCoins: 5);
        AssertTradeStoragePublishesAfterFinalState(scenario, 12, 10, 0, 3,
            static (pouch, count) => pouch.RemoveForTrade(count));
    }

    private static void AssertTradeStoragePublishesAfterFinalState(MoneyPouchScenario scenario, int movedCoins,
        int previousPouchCount, int expectedPouchCount, int expectedInventoryCoins,
        Func<IMoneyPouchContainer, int, bool> operation)
    {
        var eventManager = Substitute.For<IEventManager>();
        scenario.Owner.EventManager.Returns(eventManager);
        scenario.Inventory.OnUpdateAction = () =>
        {
            Assert.IsFalse(Monitor.IsEntered(((IItemContainerStorageProvider)scenario.Inventory).Storage.MutationLock));
            Assert.IsFalse(Monitor.IsEntered(((IItemContainerStorageProvider)scenario.MoneyPouch).Storage.MutationLock));
            Assert.AreEqual(expectedPouchCount, scenario.MoneyPouch.Count);
            Assert.AreEqual(expectedInventoryCoins, scenario.Inventory.GetCountById(CoinId));
        };
        var moneyPouchEventObservedFinalState = false;
        eventManager
            .When(manager => manager.SendEvent(Arg.Any<MoneyPouchChangedEvent>()))
            .Do(call =>
            {
                var changedEvent = call.Arg<MoneyPouchChangedEvent>();
                Assert.IsFalse(Monitor.IsEntered(((IItemContainerStorageProvider)scenario.Inventory).Storage.MutationLock));
                Assert.IsFalse(Monitor.IsEntered(((IItemContainerStorageProvider)scenario.MoneyPouch).Storage.MutationLock));
                Assert.AreEqual(previousPouchCount, changedEvent.PreviousCount);
                Assert.AreEqual(expectedPouchCount, changedEvent.Count);
                Assert.AreEqual(expectedInventoryCoins, scenario.Inventory.GetCountById(CoinId));
                moneyPouchEventObservedFinalState = true;
            });

        Assert.IsTrue(operation(scenario.MoneyPouch, movedCoins));
        Assert.IsTrue(moneyPouchEventObservedFinalState);
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

        return new MoneyPouchScenario(moneyPouch, inventory, character);
    }

    private sealed record MoneyPouchScenario(MoneyPouchContainer MoneyPouch, TestInventory Inventory,
        ICharacter Owner);

    private sealed class TestInventory : IInventoryContainer, ITradeItemContainer, IItemContainerStorageProvider
    {
        private readonly ItemContainerStorage _storage;
        ItemContainerStorage IItemContainerStorageProvider.Storage => _storage;
        public Action? OnUpdateAction { get; set; }
        public TestInventory(int capacity) => _storage = new ItemContainerStorage(StorageType.Normal, capacity);
        public IItem? this[int index] => _storage[index];
        public int Capacity => _storage.Capacity;
        public StorageType Type => _storage.Type;
        public int FreeSlots => _storage.FreeSlots;
        public int TakenSlots => _storage.TakenSlots;
        public bool Add(IItem item) { if (!_storage.TryAdd(item, out var s)) return false; OnUpdate(s); return true; }
        public bool Add(int slot, IItem item) { if (!_storage.TryAdd(slot, item, out var s)) return false; OnUpdate(s); return true; }
        public void AddAndRemoveFrom(IItemContainer container) => ItemContainerTransfer.AddAndRemoveFrom(this, container);
        public IItem? GetById(int id) => _storage.GetById(id);
        public int Remove(IItem item, int preferredSlot = -1, bool update = true) { var n = _storage.Remove(item, preferredSlot, out var s); if (n > 0 && update) OnUpdate(s); return n; }
        public void Replace(int slot, IItem item) { _storage.Replace(slot, item); OnUpdate([slot]); }
        public void Swap(int fromSlot, int toSlot) { if (_storage.Swap(fromSlot, toSlot)) OnUpdate([fromSlot, toSlot]); }
        public void Move(int fromSlot, int toSlot) { if (_storage.Move(fromSlot, toSlot)) OnUpdate((HashSet<int>?)null); }
        public bool AddRange(IEnumerable<IItem?> items) { if (!_storage.TryAddRange(items, out var s)) return false; OnUpdate(s); return true; }
        public bool Contains(int id, int count) => _storage.Contains(id, count);
        public bool Contains(int id) => _storage.Contains(id);
        public int GetCount(IItem item) => _storage.GetCount(item);
        public int GetCountById(int id) => _storage.GetCountById(id);
        public int GetInstanceSlot(IItem item) => _storage.GetInstanceSlot(item);
        public void Sort() { _storage.Sort(); OnUpdate((HashSet<int>?)null); }
        public int GetSlotByItem(IItem item, bool ignoreCount = true) => _storage.GetSlotByItem(item, ignoreCount);
        public bool HasSpaceFor(IItem item) => _storage.HasSpaceFor(item);
        public bool HasSpaceForRange(IEnumerable<IItem?> items) => _storage.HasSpaceForRange(items);
        public void Clear(bool update) { if (_storage.Clear() && update) OnUpdate(); }
        public IEnumerator<IItem?> GetEnumerator() => _storage.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        public bool AddRangeForTrade(IEnumerable<IItem?> items) { if (!TryAddRangeForTradeStorage(items, out var s)) return false; OnUpdate(s); return true; }
        public bool TryAddRangeForTradeStorage(IEnumerable<IItem?> items, out HashSet<int> changedSlots) => _storage.TryAddRange(items, out changedSlots);
        public bool RemoveForTrade(IItem item, int preferredSlot = -1) { if (!TryRemoveForTradeStorage(item, preferredSlot, out var s)) return false; OnUpdate(s); return true; }
        public bool TryRemoveForTradeStorage(IItem item, int preferredSlot, out HashSet<int> changedSlots) => _storage.TryRemoveExact(item, preferredSlot, out changedSlots);
        public bool DropItem(IItem item) => false;
        public void OnUpdate(HashSet<int>? slots = null) => OnUpdateAction?.Invoke();
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
