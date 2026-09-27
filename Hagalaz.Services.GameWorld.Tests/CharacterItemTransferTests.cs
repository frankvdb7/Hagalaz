using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Features.Shops;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Services.GameWorld.Builders;
using Hagalaz.Services.GameWorld.Logic.Shops;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class CharacterItemTransferTests
{
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
        var eventManager = Substitute.For<IEventManager>();
        scenario.Owner.EventManager.Returns(eventManager);
        var publicationSawEquipmentEffect = false;
        eventManager
            .When(manager => manager.SendEvent(Arg.Any<IEvent>()))
            .Do(call =>
            {
                if (call.Arg<IEvent>() is BankChangedEvent or EquipmentChangedEvent)
                {
                    publicationSawEquipmentEffect = callbackSawCommittedStorage;
                }
            });

        Assert.IsTrue(bank.DepositFromEquipment(item, 1, out _));

        Assert.IsTrue(callbackSawCommittedStorage);
        Assert.IsTrue(publicationSawEquipmentEffect);
        eventManager.Received(1).SendEvent(Arg.Is<IEvent>(gameEvent => gameEvent is EquipmentChangedEvent));
        eventManager.Received(1).SendEvent(Arg.Is<IEvent>(gameEvent => gameEvent is BankChangedEvent));
    }

    [TestMethod]
    public void ShopSell_TransfersTheExactItemQuantityToStock()
    {
        using var scenario = new Scenario();
        scenario.DefineItem(101, stackable: true);
        var (inventory, stock, moneyPouch) = CreateShopScenario(scenario);
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
        var (inventory, stock, moneyPouch) = CreateShopScenario(scenario);
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
        var (inventory, equipment, item) = CreateEquipmentSetup(scenario);
        var callbackSawCommittedStorage = ObserveEquippedState(scenario.Owner, inventory, equipment, item);

        Assert.IsTrue(equipment.EquipItem(item));

        Assert.IsTrue(callbackSawCommittedStorage());
    }

    [TestMethod]
    public void EquipItem_EquipmentPublicationThrowsAfterDomainEffectAndStorageCommit()
    {
        using var scenario = new Scenario();
        var (inventory, equipment, item) = CreateEquipmentSetup(scenario);
        var callbackSawCommittedStorage = ObserveEquippedState(scenario.Owner, inventory, equipment, item);
        var eventManager = Substitute.For<IEventManager>();
        scenario.Owner.EventManager.Returns(eventManager);
        var publicationSawEquipmentEffect = false;
        eventManager
            .When(manager => manager.SendEvent(Arg.Any<IEvent>()))
            .Do(call =>
            {
                if (call.Arg<IEvent>() is InventoryChangedEvent or EquipmentChangedEvent)
                {
                    publicationSawEquipmentEffect = callbackSawCommittedStorage();
                }

                if (call.Arg<IEvent>() is EquipmentChangedEvent)
                {
                    throw new InvalidOperationException("Controlled equipment publication failure.");
                }
            });

        Assert.ThrowsExactly<InvalidOperationException>(() => equipment.EquipItem(item));

        Assert.IsTrue(callbackSawCommittedStorage());
        Assert.IsTrue(publicationSawEquipmentEffect);
        Assert.IsNull(inventory[0]);
        Assert.AreSame(item, equipment[EquipmentSlot.Hat]);
        eventManager.Received(1).SendEvent(Arg.Is<IEvent>(gameEvent => gameEvent is InventoryChangedEvent));
        eventManager.Received(1).SendEvent(Arg.Is<IEvent>(gameEvent => gameEvent is EquipmentChangedEvent));
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
        var eventManager = Substitute.For<IEventManager>();
        scenario.Owner.EventManager.Returns(eventManager);
        var publicationSawEquipmentEffect = false;
        eventManager
            .When(manager => manager.SendEvent(Arg.Any<IEvent>()))
            .Do(call =>
            {
                if (call.Arg<IEvent>() is InventoryChangedEvent or EquipmentChangedEvent)
                {
                    publicationSawEquipmentEffect = callbackSawCommittedStorage;
                }
            });

        Assert.IsTrue(equipment.UnEquipItem(item));

        Assert.IsTrue(callbackSawCommittedStorage);
        Assert.IsTrue(publicationSawEquipmentEffect);
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
    private static InventoryContainer CreateInventory(Scenario scenario, int capacity) =>
        new(scenario.Owner, capacity, Substitute.For<IMapRegionService>(),
            Substitute.For<IGroundItemBuilder>(), scenario.Builder);

    private static (InventoryContainer Inventory, ShopStockContainer Stock, IMoneyPouchContainer MoneyPouch)
        CreateShopScenario(Scenario scenario)
    {
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
        return (inventory, stock, moneyPouch);
    }
    private static (InventoryContainer Inventory, EquipmentContainer Equipment, IItem Item) CreateEquipmentSetup(
        Scenario scenario)
    {
        var inventory = CreateInventory(scenario, 2);
        scenario.Owner.Inventory.Returns(inventory);
        var equipment = new EquipmentContainer(scenario.Owner, 15, scenario.Builder);
        scenario.Owner.Equipment.Returns(equipment);
        scenario.DefaultEquipmentDefinition.Slot.Returns(EquipmentSlot.Hat);
        var item = scenario.Builder.Create().WithId(101).WithCount(1).Build();
        Assert.IsTrue(inventory.Add(item));
        item.EquipmentScript.CanEquipItem(item, scenario.Owner).Returns(true);
        return (inventory, equipment, item);
    }
    private static Func<bool> ObserveEquippedState(
        ICharacter owner,
        IItemContainer inventory,
        EquipmentContainer equipment,
        IItem item)
    {
        var callbackSawCommittedStorage = false;
        item.EquipmentScript.When(script => script.OnEquipped(item, owner)).Do(_ =>
        {
            Assert.IsNull(inventory[0]);
            Assert.AreSame(item, equipment[EquipmentSlot.Hat]);
            callbackSawCommittedStorage = true;
        });
        return () => callbackSawCommittedStorage;
    }
}
