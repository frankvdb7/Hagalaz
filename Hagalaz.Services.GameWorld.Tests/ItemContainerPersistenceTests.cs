using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Features.Shops;
using Hagalaz.Game.Abstractions.Logic.Characters.Model;
using Hagalaz.Game.Abstractions.Logic.Dehydrations;
using Hagalaz.Game.Abstractions.Logic.Hydrations;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Builders;
using Hagalaz.Services.GameWorld.Logic.Shops;
using Hagalaz.Services.GameWorld.Logic.Characters.Model;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class ItemContainerPersistenceTests
{
    [TestMethod]
    public void InventoryRoundTrip_PreservesBeginningMiddleAndEndGaps()
    {
        using var scenario = new Scenario();
        AssertRoundTrip(() => new InventoryContainer(scenario.Owner, 7, Substitute.For<IMapRegionService>(),
                Substitute.For<IGroundItemBuilder>(), scenario.Builder),
            [new(101, 1, 1, null), new(102, 2, 3, null), new(103, 1, 5, null)]);
    }

    [TestMethod]
    public void InventoryDehydrate_CapturesSlotsBeforeMappingItems()
    {
        using var scenario = new Scenario();
        var container = new InventoryContainer(scenario.Owner, 7, Substitute.For<IMapRegionService>(),
            Substitute.For<IGroundItemBuilder>(), scenario.Builder);
        var first = Substitute.For<IItem>();
        first.Id.Returns(101);
        first.Count.Returns(1);
        var second = scenario.Builder.Create().WithId(102).WithCount(2).Build();
        var items = new IItem[container.Capacity];
        items[0] = first;
        items[4] = second;
        container.SetItems(items, false);
        first.SerializeExtraData().Returns(_ =>
        {
            container.Clear(false);
            return null;
        });

        var saved = container.Dehydrate();

        CollectionAssert.AreEqual(new[] { 0, 4 }, saved.Select(item => item.SlotId).ToArray());
        Assert.AreEqual(102, saved[1].ItemId);
        Assert.AreEqual(0, container.TakenSlots);
    }

    [TestMethod]
    public void BankRoundTrip_PreservesSparseSlots()
    {
        using var scenario = new Scenario();
        AssertRoundTrip(() => new BankContainer(scenario.Owner, 12, scenario.Builder),
            [new(101, 2, 2, null), new(102, 3, 9, null)]);
    }

    [TestMethod]
    public void BankDepositFromInventory_TransfersExactCountAndPreservesExtraData()
    {
        using var scenario = new Scenario();
        var inventory = CreateInventory(scenario, 4);
        scenario.Owner.Inventory.Returns(inventory);
        var bank = new BankContainer(scenario.Owner, 4, scenario.Builder);
        var item = scenario.Builder.Create().WithId(101).WithCount(5).WithExtraData("11,22").Build();
        inventory.Add(item);

        Assert.IsTrue(bank.DepositFromInventory(item, 3, out var deposited));

        Assert.AreEqual(3, deposited!.Count);
        Assert.AreEqual(2, inventory[0]!.Count);
        Assert.AreEqual(3, bank.GetCountById(101));
        CollectionAssert.AreEqual(new long[] { 11, 22 }, bank[0]!.ExtraData);
    }

    [TestMethod]
    public void BankDepositFromInventory_UnnotesIntoBankAndRetainsExtraData()
    {
        using var scenario = new Scenario();
        scenario.DefineItem(201, stackable: true, noted: true, noteId: 101);
        var inventory = CreateInventory(scenario, 4);
        scenario.Owner.Inventory.Returns(inventory);
        var bank = new BankContainer(scenario.Owner, 4, scenario.Builder);
        var note = scenario.Builder.Create().WithId(201).WithCount(4).WithExtraData("7,9").Build();
        inventory.Add(note);

        Assert.IsTrue(bank.DepositFromInventory(note, 2, out var deposited));

        Assert.AreEqual(101, deposited!.Id);
        Assert.AreEqual(2, deposited.Count);
        Assert.AreEqual(2, inventory.GetCountById(201));
        Assert.AreEqual(2, bank.GetCountById(101));
        CollectionAssert.AreEqual(new long[] { 7, 9 }, bank[0]!.ExtraData);
    }

    [TestMethod]
    public void BankWithdrawFromBank_TransfersExactCountAsNoteAndPreservesExtraData()
    {
        using var scenario = new Scenario();
        scenario.DefineItem(101, stackable: true, noteId: 201);
        scenario.DefineItem(201, stackable: true, noted: true, noteId: 101);
        var inventory = CreateInventory(scenario, 4);
        scenario.Owner.Inventory.Returns(inventory);
        var bank = new BankContainer(scenario.Owner, 4, scenario.Builder);
        var item = scenario.Builder.Create().WithId(101).WithCount(5).WithExtraData("11,22").Build();
        bank.Add(item);

        Assert.IsTrue(bank.WithdrawFromBank(item, 3, notingEnabled: true, out var withdrawn));

        Assert.AreEqual(201, withdrawn!.Id);
        Assert.AreEqual(3, inventory.GetCountById(201));
        Assert.AreEqual(2, bank.GetCountById(101));
        CollectionAssert.AreEqual(new long[] { 11, 22 }, inventory[0]!.ExtraData);
    }

    [TestMethod]
    public void BankWithdrawFromBank_TransfersOnlyTheNonStackableQuantityThatFits()
    {
        using var scenario = new Scenario();
        scenario.DefineItem(101, stackable: false);
        scenario.DefineItem(102, stackable: false);
        var inventory = CreateInventory(scenario, 2);
        scenario.Owner.Inventory.Returns(inventory);
        var bank = new BankContainer(scenario.Owner, 4, scenario.Builder);
        inventory.Add(scenario.Builder.Create().WithId(102).WithCount(1).Build());
        var item = scenario.Builder.Create().WithId(101).WithCount(5).Build();
        bank.Add(item);

        Assert.IsTrue(bank.WithdrawFromBank(item, 3, notingEnabled: false, out var withdrawn));

        Assert.AreEqual(1, withdrawn!.Count);
        Assert.AreEqual(4, bank.GetCountById(101));
        Assert.AreEqual(1, inventory.GetCountById(101));
        Assert.AreEqual(2, inventory.TakenSlots);
    }

    [TestMethod]
    public void BankDepositFromEquipment_TransfersBeforeCallingUnequipped()
    {
        using var scenario = new Scenario();
        var inventory = CreateInventory(scenario, 2);
        scenario.Owner.Inventory.Returns(inventory);
        var equipment = new EquipmentContainer(scenario.Owner, 15, scenario.Builder);
        scenario.Owner.Equipment.Returns(equipment);
        var bank = new BankContainer(scenario.Owner, 4, scenario.Builder);
        var item = scenario.Builder.Create().WithId(101).WithCount(1).Build();
        equipment.Add(EquipmentSlot.Hat, item);
        var callbackSawCommittedStorage = false;
        item.EquipmentScript.When(script => script.OnUnequipped(item, scenario.Owner)).Do(_ =>
        {
            Assert.IsNull(equipment[EquipmentSlot.Hat]);
            Assert.AreSame(item, bank[0]);
            callbackSawCommittedStorage = true;
        });

        Assert.IsTrue(bank.DepositFromEquipment(item, 1, out _));

        Assert.IsTrue(callbackSawCommittedStorage);
    }

    [TestMethod]
    public void ShopSell_TransfersTheExactItemQuantityToStock()
    {
        using var scenario = new Scenario();
        scenario.DefineItem(101, stackable: true);
        var inventory = CreateInventory(scenario, 4);
        scenario.Owner.Inventory.Returns(inventory);
        var moneyPouch = Substitute.For<IMoneyPouchContainer>();
        moneyPouch.Contains(995).Returns(true);
        moneyPouch.Add(Arg.Any<int>()).Returns(true);
        scenario.Owner.MoneyPouch.Returns(moneyPouch);
        var shop = Substitute.For<IShop>();
        shop.GeneralStore.Returns(true);
        shop.CurrencyId.Returns(995);
        shop.GetSellValue(Arg.Any<IItem>()).Returns(2);
        var stock = new ShopStockContainer(shop, Substitute.For<IItemService>(), scenario.Builder, false,
            StorageType.Normal, 4, new List<IItem>(), scenario.Owner.EventManager);
        var item = scenario.Builder.Create().WithId(101).WithCount(5).WithExtraData("11,22").Build();
        item.ItemScript.CanSellItem(Arg.Any<IItem>(), scenario.Owner).Returns(true);
        inventory.Add(item);

        Assert.IsTrue(stock.SellFromInventory(scenario.Owner, item, 3));

        Assert.AreEqual(2, inventory.GetCountById(101));
        Assert.AreEqual(3, stock.GetCountById(101));
        CollectionAssert.AreEqual(new long[] { 11, 22 }, stock[0]!.ExtraData);
        moneyPouch.Received(1).Add(6);
    }

    [TestMethod]
    public void ShopSell_NotedItemCapsToAvailableQuantityAndPreservesExtraData()
    {
        using var scenario = new Scenario();
        scenario.DefineItem(101, stackable: true);
        scenario.DefineItem(201, stackable: true, noted: true, noteId: 101);
        var inventory = CreateInventory(scenario, 4);
        scenario.Owner.Inventory.Returns(inventory);
        var moneyPouch = Substitute.For<IMoneyPouchContainer>();
        moneyPouch.Contains(995).Returns(true);
        moneyPouch.Add(Arg.Any<int>()).Returns(true);
        scenario.Owner.MoneyPouch.Returns(moneyPouch);
        var shop = Substitute.For<IShop>();
        shop.GeneralStore.Returns(true);
        shop.CurrencyId.Returns(995);
        shop.GetSellValue(Arg.Any<IItem>()).Returns(2);
        var stock = new ShopStockContainer(shop, Substitute.For<IItemService>(), scenario.Builder, false,
            StorageType.Normal, 4, new List<IItem>(), scenario.Owner.EventManager);
        var item = scenario.Builder.Create().WithId(201).WithCount(5).WithExtraData("11,22").Build();
        item.ItemScript.CanSellItem(Arg.Any<IItem>(), scenario.Owner).Returns(true);
        inventory.Add(item);

        Assert.IsTrue(stock.SellFromInventory(scenario.Owner, item, 10));

        Assert.AreEqual(0, inventory.GetCountById(201));
        Assert.AreEqual(5, stock.GetCountById(101));
        CollectionAssert.AreEqual(new long[] { 11, 22 }, stock[0]!.ExtraData);
        moneyPouch.Received(1).Add(10);
    }

    [TestMethod]
    public void FamiliarInventory_TransfersBothDirectionsWithExactCounts()
    {
        using var scenario = new Scenario();
        var inventory = CreateInventory(scenario, 4);
        scenario.Owner.Inventory.Returns(inventory);
        var familiar = new FamiliarInventoryContainer(scenario.Owner, StorageType.Normal, 4, scenario.Builder);
        var item = scenario.Builder.Create().WithId(101).WithCount(5).Build();
        inventory.Add(item);

        Assert.IsTrue(familiar.DepositFromInventory(item, 3));
        Assert.AreEqual(2, inventory.GetCountById(101));
        Assert.AreEqual(3, familiar.GetCountById(101));

        var familiarItem = familiar[0]!;
        Assert.IsTrue(familiar.WithdrawFromFamiliarInventory(familiarItem, 2));
        Assert.AreEqual(4, inventory.GetCountById(101));
        Assert.AreEqual(1, familiar.GetCountById(101));
    }

    [TestMethod]
    public void RewardClaim_TransfersOnlyTheNonStackableQuantityThatFits()
    {
        using var scenario = new Scenario();
        scenario.DefineItem(101, stackable: false);
        scenario.DefineItem(102, stackable: false);
        var inventory = CreateInventory(scenario, 2);
        scenario.Owner.Inventory.Returns(inventory);
        var rewards = new RewardContainer(scenario.Owner, scenario.Builder);
        inventory.Add(scenario.Builder.Create().WithId(102).WithCount(1).Build());
        var rewardItem = scenario.Builder.Create().WithId(101).WithCount(4).Build();
        rewards.Add(rewardItem);

        Assert.AreEqual(1, rewards.Claim(rewardItem, 3));

        Assert.AreEqual(3, rewards.GetCountById(101));
        Assert.AreEqual(1, inventory.GetCountById(101));
        Assert.AreEqual(2, inventory.TakenSlots);
    }

    [TestMethod]
    public void EquipItem_EmptySlotCallsEquippedAfterStorageCommit()
    {
        using var scenario = new Scenario();
        var inventory = CreateInventory(scenario, 2);
        scenario.Owner.Inventory.Returns(inventory);
        var equipment = new EquipmentContainer(scenario.Owner, 15, scenario.Builder);
        scenario.Owner.Equipment.Returns(equipment);
        scenario.DefaultEquipmentDefinition.Slot.Returns(EquipmentSlot.Hat);
        var item = scenario.Builder.Create().WithId(101).WithCount(1).Build();
        inventory.Add(item);
        item.EquipmentScript.CanEquipItem(item, scenario.Owner).Returns(true);
        var callbackSawCommittedStorage = false;
        item.EquipmentScript.When(script => script.OnEquipped(item, scenario.Owner)).Do(_ =>
        {
            Assert.IsNull(inventory[0]);
            Assert.AreSame(item, equipment[EquipmentSlot.Hat]);
            callbackSawCommittedStorage = true;
        });

        Assert.IsTrue(equipment.EquipItem(item));

        Assert.IsTrue(callbackSawCommittedStorage);
    }

    [TestMethod]
    public void UnEquipItem_InventoryFullLeavesEquipmentWithoutUnequippedCallback()
    {
        using var scenario = new Scenario();
        var inventory = CreateInventory(scenario, 1);
        scenario.Owner.Inventory.Returns(inventory);
        var equipment = new EquipmentContainer(scenario.Owner, 15, scenario.Builder);
        scenario.Owner.Equipment.Returns(equipment);
        scenario.DefaultEquipmentDefinition.Slot.Returns(EquipmentSlot.Hat);
        var item = scenario.Builder.Create().WithId(101).WithCount(1).Build();
        equipment.Add(EquipmentSlot.Hat, item);
        inventory.Add(scenario.Builder.Create().WithId(102).WithCount(1).Build());
        item.EquipmentScript.CanUnEquipItem(item, scenario.Owner).Returns(true);

        Assert.IsFalse(equipment.UnEquipItem(item));

        Assert.AreSame(item, equipment[EquipmentSlot.Hat]);
        Assert.AreEqual(1, inventory.GetCountById(102));
        item.EquipmentScript.DidNotReceive().OnUnequipped(item, scenario.Owner);
    }

    [TestMethod]
    public void UnEquipItem_CallsUnequippedAfterStorageCommit()
    {
        using var scenario = new Scenario();
        var inventory = CreateInventory(scenario, 2);
        scenario.Owner.Inventory.Returns(inventory);
        var equipment = new EquipmentContainer(scenario.Owner, 15, scenario.Builder);
        scenario.Owner.Equipment.Returns(equipment);
        var item = scenario.Builder.Create().WithId(101).WithCount(1).Build();
        equipment.Add(EquipmentSlot.Hat, item);
        item.EquipmentScript.CanUnEquipItem(item, scenario.Owner).Returns(true);
        var callbackSawCommittedStorage = false;
        item.EquipmentScript.When(script => script.OnUnequipped(item, scenario.Owner)).Do(_ =>
        {
            Assert.IsNull(equipment[EquipmentSlot.Hat]);
            Assert.AreSame(item, inventory[0]);
            callbackSawCommittedStorage = true;
        });

        Assert.IsTrue(equipment.UnEquipItem(item));

        Assert.IsTrue(callbackSawCommittedStorage);
    }

    [TestMethod]
    public void RewardClaim_WhenInventoryFullLeavesRewardUnchanged()
    {
        using var scenario = new Scenario();
        var inventory = CreateInventory(scenario, 1);
        scenario.Owner.Inventory.Returns(inventory);
        var rewards = new RewardContainer(scenario.Owner, scenario.Builder);
        inventory.Add(scenario.Builder.Create().WithId(102).WithCount(1).Build());
        var rewardItem = scenario.Builder.Create().WithId(101).WithCount(4).Build();
        rewards.Add(rewardItem);

        Assert.AreEqual(-1, rewards.Claim(rewardItem, 1));

        Assert.AreEqual(4, rewards.GetCountById(101));
        Assert.AreEqual(0, inventory.GetCountById(101));
        Assert.AreEqual(1, inventory.GetCountById(102));
    }

    [TestMethod]
    public void EquipmentRoundTrip_PreservesSemanticSlotsAndGaps()
    {
        using var scenario = new Scenario();
        AssertRoundTrip(() => new EquipmentContainer(scenario.Owner, 15, scenario.Builder),
            [new(101, 1, (int)EquipmentSlot.Hat, null), new(102, 1, (int)EquipmentSlot.Weapon, null),
                new(103, 1, (int)EquipmentSlot.Shield, null), new(104, 1, (int)EquipmentSlot.Ring, null)]);
    }

    [TestMethod]
    public void FamiliarInventoryRoundTrip_PreservesSparseSlots()
    {
        using var scenario = new Scenario();
        FamiliarInventoryContainer Create() => new(scenario.Owner, StorageType.Normal, 8, scenario.Builder);
        var source = Create();
        source.Hydrate([new HydratedItem(101, 1, 1, null), new HydratedItem(102, 2, 6, null)]);

        var saved = source.Dehydrate();
        CollectionAssert.AreEqual(new[] { 1, 6 }, saved.Select(item => item.SlotId).ToArray());

        var restored = Create();
        restored.Hydrate(saved);
        AssertSlots(restored, (1, 101), (6, 102));
    }

    [TestMethod]
    public void RewardRoundTrip_PreservesSparseSlots()
    {
        using var scenario = new Scenario();
        AssertRoundTrip(() => new RewardContainer(scenario.Owner, scenario.Builder),
            [new(101, 1, 4, null), new(102, 2, 200, null)]);
    }

    [TestMethod]
    public void MoneyPouchRoundTrip_PreservesZeroCoinSentinel()
    {
        using var scenario = new Scenario();
        var source = new MoneyPouchContainer(scenario.Owner, scenario.Builder);
        var saved = source.Dehydrate();
        Assert.HasCount(1, saved);
        Assert.AreEqual(0, saved[0].SlotId);
        Assert.AreEqual(0, saved[0].Count);

        var restored = new MoneyPouchContainer(scenario.Owner, scenario.Builder);
        restored.Hydrate(saved);
        Assert.AreEqual(0, restored.Count);
        Assert.AreEqual(995, restored[0]!.Id);
    }

    [TestMethod]
    public void MoneyPouchHydration_EmptyStateRestoresZeroCoinSentinel()
    {
        using var scenario = new Scenario();
        var container = new MoneyPouchContainer(scenario.Owner, scenario.Builder);
        container.Hydrate([new HydratedItemDto(995, 25, 0, null)]);

        container.Hydrate([]);

        Assert.AreEqual(0, container.Count);
        Assert.IsNotNull(container[0]);
        Assert.AreEqual(995, container[0]!.Id);
        var saved = container.Dehydrate();
        Assert.HasCount(1, saved);
        Assert.AreEqual(new HydratedItemDto(995, 0, 0, null), saved[0]);
    }

    [TestMethod]
    public void MoneyPouchHydration_RejectsNonCoinItemWithoutChangingState()
    {
        using var scenario = new Scenario();
        var container = new MoneyPouchContainer(scenario.Owner, scenario.Builder);
        container.Hydrate([new HydratedItemDto(995, 25, 0, null)]);

        Assert.ThrowsExactly<ArgumentException>(() => container.Hydrate([new HydratedItemDto(101, 5, 0, null)]));

        Assert.AreEqual(25, container.Count);
        Assert.AreEqual(995, container[0]!.Id);
    }

    [TestMethod]
    public void InventoryRoundTrip_PreservesItemExtraData()
    {
        using var scenario = new Scenario();
        InventoryContainer Create() => new(scenario.Owner, 7, Substitute.For<IMapRegionService>(),
            Substitute.For<IGroundItemBuilder>(), scenario.Builder);
        var source = Create();
        source.Hydrate([new HydratedItemDto(101, 1, 4, "11,22")]);

        var saved = source.Dehydrate();
        Assert.AreEqual("11,22", saved[0].ExtraData);

        var restored = Create();
        restored.Hydrate(saved);
        CollectionAssert.AreEqual(new long[] { 11, 22 }, restored[4]!.ExtraData);
    }

    [TestMethod]
    public void InventoryHydration_RejectsDuplicateSlotsWithoutChangingState()
    {
        using var scenario = new Scenario();
        var container = new InventoryContainer(scenario.Owner, 7, Substitute.For<IMapRegionService>(),
            Substitute.For<IGroundItemBuilder>(), scenario.Builder);
        container.Hydrate([new HydratedItemDto(101, 1, 4, null)]);

        Assert.ThrowsExactly<ArgumentException>(() => container.Hydrate(
            [new HydratedItemDto(102, 1, 2, null), new HydratedItemDto(103, 1, 2, null)]));
        AssertSlots(container, (4, 101));
    }

    [TestMethod]
    [DataRow(-1)]
    [DataRow(7)]
    public void InventoryHydration_RejectsOutOfBoundsSlotWithoutChangingState(int invalidSlot)
    {
        using var scenario = new Scenario();
        var container = new InventoryContainer(scenario.Owner, 7, Substitute.For<IMapRegionService>(),
            Substitute.For<IGroundItemBuilder>(), scenario.Builder);
        container.Hydrate([new HydratedItemDto(101, 1, 4, null)]);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => container.Hydrate(
            [new HydratedItemDto(102, 1, invalidSlot, null)]));
        AssertSlots(container, (4, 101));
    }

    [TestMethod]
    [DataRow(-1)]
    [DataRow(0)]
    public void InventoryHydration_RejectsInvalidCountWithoutChangingState(int invalidCount)
    {
        using var scenario = new Scenario();
        var container = new InventoryContainer(scenario.Owner, 7, Substitute.For<IMapRegionService>(),
            Substitute.For<IGroundItemBuilder>(), scenario.Builder);
        container.Hydrate([new HydratedItemDto(101, 1, 4, null)]);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => container.Hydrate(
            [new HydratedItemDto(102, invalidCount, 2, null)]));
        AssertSlots(container, (4, 101));
    }

    [TestMethod]
    public void InventoryHydration_KeepsIdenticalStackableItemsInTheirPersistedSlots()
    {
        using var scenario = new Scenario();
        var container = new InventoryContainer(scenario.Owner, 7, Substitute.For<IMapRegionService>(),
            Substitute.For<IGroundItemBuilder>(), scenario.Builder);

        container.Hydrate([new HydratedItemDto(101, 2, 2, null), new HydratedItemDto(101, 3, 5, null)]);

        AssertSlots(container, (2, 101), (5, 101));
        Assert.AreEqual(2, container[2]!.Count);
        Assert.AreEqual(3, container[5]!.Count);
    }

    private static void AssertRoundTrip<T>(Func<T> create, IReadOnlyList<HydratedItemDto> initial)
        where T : BaseItemContainer, IHydratable<IReadOnlyList<HydratedItemDto>>, IDehydratable<IReadOnlyList<HydratedItemDto>>
    {
        var source = create();
        source.Hydrate(initial);

        var saved = source.Dehydrate();
        CollectionAssert.AreEqual(initial.Select(item => item.SlotId).ToArray(), saved.Select(item => item.SlotId).ToArray());

        var restored = create();
        restored.Hydrate(saved);
        AssertSlots(restored, initial.Select(item => (item.SlotId, item.ItemId)).ToArray());
        foreach (var item in initial)
        {
            Assert.AreEqual(item.Count, restored[item.SlotId]!.Count);
        }
    }

    private static void AssertSlots(IItemContainer container, params (int Slot, int ItemId)[] occupied)
    {
        for (var slot = 0; slot < container.Capacity; slot++)
        {
            var match = occupied.FirstOrDefault(item => item.Slot == slot);
            Assert.AreEqual(match.ItemId == 0 ? null : match.ItemId, container[slot]?.Id, $"Slot {slot}");
        }
    }

    private static InventoryContainer CreateInventory(Scenario scenario, int capacity) =>
        new(scenario.Owner, capacity, Substitute.For<IMapRegionService>(),
            Substitute.For<IGroundItemBuilder>(), scenario.Builder);

    private sealed class Scenario : IDisposable
    {
        private readonly ServiceProvider _services;
        private readonly Dictionary<int, IItemDefinition> _itemDefinitions = [];

        public ICharacter Owner { get; } = Substitute.For<ICharacter>();
        public IItemBuilder Builder { get; }
        public IItemDefinition DefaultItemDefinition { get; }
        public IEquipmentDefinition DefaultEquipmentDefinition { get; }

        public Scenario()
        {
            Owner.Statistics.Returns(Substitute.For<ICharacterStatistics>());
            DefaultItemDefinition = CreateDefinition(stackable: true);
            DefaultEquipmentDefinition = Substitute.For<IEquipmentDefinition>();
            var itemScript = Substitute.For<IItemScript>();
            itemScript.CanStackItem(Arg.Any<IItem>(), Arg.Any<IItem>(), Arg.Any<bool>()).Returns(callInfo =>
            {
                var current = callInfo.ArgAt<IItem>(0);
                var incoming = callInfo.ArgAt<IItem>(1);
                return current.Id == incoming.Id && (callInfo.ArgAt<bool>(2) || current.ItemDefinition.Stackable || current.ItemDefinition.Noted);
            });
            var equipmentScript = Substitute.For<IEquipmentScript>();
            var itemService = Substitute.For<IItemService>();
            itemService.FindItemDefinitionById(Arg.Any<int>()).Returns(callInfo =>
            {
                var id = callInfo.Arg<int>();
                return _itemDefinitions.TryGetValue(id, out var definition) ? definition : DefaultItemDefinition;
            });
            var equipmentService = Substitute.For<IEquipmentService>();
            equipmentService.FindEquipmentDefinitionById(Arg.Any<int>()).Returns(DefaultEquipmentDefinition);
            var itemProvider = Substitute.For<IItemScriptProvider>();
            itemProvider.FindItemScriptById(Arg.Any<int>()).Returns(itemScript);
            var equipmentProvider = Substitute.For<IEquipmentScriptProvider>();
            equipmentProvider.FindEquipmentScriptById(Arg.Any<int>()).Returns(equipmentScript);
            _services = new ServiceCollection()
                .AddSingleton(itemService)
                .AddSingleton(equipmentService)
                .BuildServiceProvider();
            Builder = new ItemBuilder(_services, itemProvider, equipmentProvider);
        }

        public void DefineItem(int id, bool stackable, bool noted = false, int noteId = -1)
        {
            _itemDefinitions[id] = CreateDefinition(stackable, noted, noteId);
        }

        public void DefineItem(int id, IItemDefinition definition) => _itemDefinitions[id] = definition;

        private static IItemDefinition CreateDefinition(bool stackable, bool noted = false, int noteId = -1)
        {
            var definition = Substitute.For<IItemDefinition>();
            definition.Stackable.Returns(stackable);
            definition.Noted.Returns(noted);
            definition.NoteId.Returns(noteId);
            return definition;
        }

        public void Dispose() => _services.Dispose();
    }
}
