using System.Threading;
using System.Reflection;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Features.Shops;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions;
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
    public void TryRestoreEquippedItem_PublishesWithoutRunningOnEquipped()
    {
        using var scenario = new Scenario();
        var (equipment, eventManager) = CreateEquipmentScenario(scenario);
        var item = scenario.Builder.Create().WithId(101).WithCount(1).Build();

        Assert.IsTrue(equipment.TryRestoreEquippedItem(EquipmentSlot.Hat, item));

        Assert.AreSame(item, equipment[EquipmentSlot.Hat]);
        item.EquipmentScript.DidNotReceive().OnEquipped(item, scenario.Owner);
        eventManager.Received(1).SendEvent(Arg.Is<IEvent>(gameEvent => gameEvent is EquipmentChangedEvent));
    }

    [TestMethod]
    public void TryReplaceEquippedItem_ReplacesExpectedInstanceAndPreservesLifecycleOrder()
    {
        using var scenario = new Scenario();
        var (equipment, eventManager, current, replacement) = CreateReplacementScenario(scenario);
        var order = new List<string>();
        eventManager.When(manager => manager.SendEvent(Arg.Any<IEvent>())).Do(call =>
        {
            if (call.Arg<IEvent>() is EquipmentChangedEvent)
            {
                Assert.AreSame(replacement, equipment[EquipmentSlot.Hat]);
                CollectionAssert.AreEqual(new[] { "unequip", "equip" }, order);
                order.Add("publish");
            }
        });
        current.EquipmentScript.When(script => script.OnUnequipped(current, scenario.Owner)).Do(_ =>
        {
            Assert.AreSame(replacement, equipment[EquipmentSlot.Hat]);
            order.Add("unequip");
        });
        replacement.EquipmentScript.When(script => script.OnEquipped(replacement, scenario.Owner)).Do(_ =>
        {
            Assert.AreSame(replacement, equipment[EquipmentSlot.Hat]);
            order.Add("equip");
        });

        Assert.IsTrue(equipment.TryReplaceEquippedItem(EquipmentSlot.Hat, current, replacement));

        Assert.AreSame(replacement, equipment[EquipmentSlot.Hat]);
        CollectionAssert.AreEqual(new[] { "unequip", "equip", "publish" }, order);
    }

    [TestMethod]
    public void TryReplaceEquippedItem_WhenOldCallbackThrows_AttemptsRemainingActionsAndPreservesStorage()
    {
        using var scenario = new Scenario();
        var (equipment, eventManager, current, replacement) = CreateReplacementScenario(scenario);
        var failure = new InvalidOperationException("old unequip failed");
        var order = new List<string>();
        current.EquipmentScript.When(script => script.OnUnequipped(current, scenario.Owner)).Do(_ =>
        {
            Assert.AreSame(replacement, equipment[EquipmentSlot.Hat]);
            order.Add("unequip");
            throw failure;
        });
        replacement.EquipmentScript.When(script => script.OnEquipped(replacement, scenario.Owner)).Do(_ =>
        {
            Assert.AreSame(replacement, equipment[EquipmentSlot.Hat]);
            order.Add("equip");
        });
        eventManager.When(manager => manager.SendEvent(Arg.Any<IEvent>())).Do(call =>
        {
            if (call.Arg<IEvent>() is EquipmentChangedEvent) order.Add("publish");
        });

        var thrown = Assert.ThrowsExactly<InvalidOperationException>(() =>
            equipment.TryReplaceEquippedItem(EquipmentSlot.Hat, current, replacement));

        Assert.AreSame(failure, thrown);
        Assert.AreSame(replacement, equipment[EquipmentSlot.Hat]);
        CollectionAssert.AreEqual(new[] { "unequip", "equip", "publish" }, order);
        eventManager.Received(1).SendEvent(Arg.Is<IEvent>(gameEvent => gameEvent is EquipmentChangedEvent));
    }

    [TestMethod]
    public void TryReplaceEquippedItem_WhenBothCallbacksThrow_AggregatesFailuresAfterPublication()
    {
        using var scenario = new Scenario();
        var (equipment, eventManager, current, replacement) = CreateReplacementScenario(scenario);
        var unequipFailure = new InvalidOperationException("old unequip failed");
        var equipFailure = new InvalidOperationException("new equip failed");
        var order = ObserveFailingReplacementEffects(scenario.Owner, current, replacement, unequipFailure, equipFailure);
        eventManager.When(manager => manager.SendEvent(Arg.Any<IEvent>())).Do(call =>
        {
            if (call.Arg<IEvent>() is EquipmentChangedEvent) order.Add("publish");
        });

        var thrown = Assert.ThrowsExactly<AggregateException>(() =>
            equipment.TryReplaceEquippedItem(EquipmentSlot.Hat, current, replacement));

        Assert.AreSame(unequipFailure, thrown.InnerExceptions[0]);
        Assert.AreSame(equipFailure, thrown.InnerExceptions[1]);
        Assert.AreEqual(2, thrown.InnerExceptions.Count);
        Assert.AreSame(replacement, equipment[EquipmentSlot.Hat]);
        CollectionAssert.AreEqual(new[] { "unequip", "equip", "publish" }, order);
        eventManager.Received(1).SendEvent(Arg.Is<IEvent>(gameEvent => gameEvent is EquipmentChangedEvent));
    }

    [TestMethod]
    public void TryReplaceEquippedItem_StaleExpectedInstanceDoesNothing()
    {
        using var scenario = new Scenario();
        var eventManager = Substitute.For<IEventManager>();
        scenario.Owner.EventManager.Returns(eventManager);
        var equipment = new EquipmentContainer(scenario.Owner, 15, scenario.Builder);
        var current = scenario.Builder.Create().WithId(101).WithCount(1).Build();
        var stale = scenario.Builder.Create().WithId(102).WithCount(1).Build();
        var replacement = scenario.Builder.Create().WithId(103).WithCount(1).Build();
        Assert.IsTrue(equipment.TryRestoreEquippedItem(EquipmentSlot.Hat, current));
        eventManager.ClearReceivedCalls();

        Assert.IsFalse(equipment.TryReplaceEquippedItem(EquipmentSlot.Hat, stale, replacement));

        Assert.AreSame(current, equipment[EquipmentSlot.Hat]);
        current.EquipmentScript.DidNotReceive().OnUnequipped(current, scenario.Owner);
        replacement.EquipmentScript.DidNotReceive().OnEquipped(replacement, scenario.Owner);
        eventManager.DidNotReceive().SendEvent(Arg.Any<IEvent>());
    }

    [TestMethod]
    public void RemoveEquippedItem_FullRemovalRunsLifecycleBeforePublication()
    {
        using var scenario = new Scenario();
        var eventManager = Substitute.For<IEventManager>();
        scenario.Owner.EventManager.Returns(eventManager);
        var equipment = new EquipmentContainer(scenario.Owner, 15, scenario.Builder);
        var item = scenario.Builder.Create().WithId(101).WithCount(1).Build();
        Assert.IsTrue(equipment.TryRestoreEquippedItem(EquipmentSlot.Hat, item));
        eventManager.ClearReceivedCalls();
        var order = ObserveUnequipBeforePublication(eventManager, equipment, item, scenario.Owner);

        var removed = equipment.RemoveEquippedItem(item, EquipmentSlot.Hat);

        Assert.AreEqual(1, removed);
        Assert.IsNull(equipment[EquipmentSlot.Hat]);
        CollectionAssert.AreEqual(new[] { "unequip", "publish" }, order);
    }

    [TestMethod]
    public void RemoveEquippedItem_WhenUnequipCallbackThrows_StillPublishesAndKeepsStorageRemoved()
    {
        using var scenario = new Scenario();
        var eventManager = Substitute.For<IEventManager>();
        scenario.Owner.EventManager.Returns(eventManager);
        var equipment = new EquipmentContainer(scenario.Owner, 15, scenario.Builder);
        var item = scenario.Builder.Create().WithId(101).WithCount(1).Build();
        Assert.IsTrue(equipment.TryRestoreEquippedItem(EquipmentSlot.Hat, item));
        eventManager.ClearReceivedCalls();
        var failure = new InvalidOperationException("unequip failed");
        var order = new List<string>();
        item.EquipmentScript.When(script => script.OnUnequipped(item, scenario.Owner)).Do(_ =>
        {
            Assert.IsNull(equipment[EquipmentSlot.Hat]);
            order.Add("unequip");
            throw failure;
        });
        eventManager.When(manager => manager.SendEvent(Arg.Any<IEvent>())).Do(call =>
        {
            if (call.Arg<IEvent>() is EquipmentChangedEvent) order.Add("publish");
        });

        var thrown = Assert.ThrowsExactly<InvalidOperationException>(() =>
            equipment.RemoveEquippedItem(item, EquipmentSlot.Hat));

        Assert.AreSame(failure, thrown);
        Assert.IsNull(equipment[EquipmentSlot.Hat]);
        CollectionAssert.AreEqual(new[] { "unequip", "publish" }, order);
        eventManager.Received(1).SendEvent(Arg.Is<IEvent>(gameEvent => gameEvent is EquipmentChangedEvent));
    }

    [TestMethod]
    public void RemoveEquippedItem_PartialRemovalPublishesWithoutUnequipping()
    {
        using var scenario = new Scenario();
        scenario.DefineItem(101, stackable: true);
        var eventManager = Substitute.For<IEventManager>();
        scenario.Owner.EventManager.Returns(eventManager);
        var equipment = new EquipmentContainer(scenario.Owner, 15, scenario.Builder);
        var item = scenario.Builder.Create().WithId(101).WithCount(5).Build();
        Assert.IsTrue(equipment.TryRestoreEquippedItem(EquipmentSlot.Hat, item));
        eventManager.ClearReceivedCalls();
        var partial = item.Clone();
        partial.Count = 2;
        var published = false;
        eventManager.When(manager => manager.SendEvent(Arg.Any<IEvent>())).Do(call =>
        {
            if (call.Arg<IEvent>() is EquipmentChangedEvent)
            {
                Assert.AreSame(item, equipment[EquipmentSlot.Hat]);
                Assert.AreEqual(3, item.Count);
                published = true;
            }
        });

        var removed = equipment.RemoveEquippedItem(partial, EquipmentSlot.Hat);

        Assert.AreEqual(2, removed);
        Assert.AreSame(item, equipment[EquipmentSlot.Hat]);
        Assert.AreEqual(3, item.Count);
        Assert.IsTrue(published);
        item.EquipmentScript.DidNotReceive().OnUnequipped(item, scenario.Owner);
    }

    [TestMethod]
    public void BankDepositFromInventory_TransfersExactCountAndPreservesExtraData()
    {
        using var scenario = new Scenario();
        var inventory = CreateInventory(scenario, 4);
        scenario.Owner.Inventory.Returns(inventory);
        var bank = new BankContainer(scenario.Owner, 4, scenario.Builder);
        var item = scenario.Builder.Create().WithId(101).WithCount(5).WithExtraData("11,22").Build();
        inventory.Items.Add(item);

        Assert.IsTrue(bank.DepositFromInventory(item, 3, out var deposited));

        Assert.AreEqual(3, deposited!.Count);
        Assert.AreEqual(2, inventory.Items[0]!.Count);
        Assert.AreEqual(3, bank.Items.GetCountById(101));
        CollectionAssert.AreEqual(new long[] { 11, 22 }, bank.Items[0]!.ExtraData);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void BankDepositFromMoneyPouch_WhenBankCannotAcceptCoins_LeavesBothStoresUnchanged(bool joined)
    {
        using var scenario = new Scenario();
        var inventory = CreateInventory(scenario, 4);
        scenario.Owner.Inventory.Returns(inventory);
        var moneyPouch = new MoneyPouchContainer(scenario.Owner, scenario.Builder);
        scenario.Owner.MoneyPouch.Returns(moneyPouch);
        Assert.IsTrue(moneyPouch.Add(5));
        var bank = new BankContainer(scenario.Owner, 2, scenario.Builder);
        Assert.IsTrue(bank.Items.Add(scenario.Builder.Create().WithId(995).WithCount(int.MaxValue).Build()));
        var events = Substitute.For<IEventManager>();
        scenario.Owner.EventManager.Returns(events);
        scenario.Owner.ClearReceivedCalls();
        var bankStorage = ((ItemContainerMutationBoundary)bank.Items.Mutations).Storage;
        var pouchStorages = ((IItemContainerTransactionParticipantInternal)moneyPouch.Mutations).Boundaries;
        scenario.Owner.When(owner => owner.SendChatMessage(Arg.Any<string>())).Do(_ =>
        {
            Assert.IsNull(bankStorage.Transaction);
            Assert.IsFalse(Monitor.IsEntered(bankStorage.MutationLock));
            foreach (var boundary in pouchStorages)
            {
                Assert.IsNull(boundary.Storage.Transaction);
                Assert.IsFalse(Monitor.IsEntered(boundary.Storage.MutationLock));
            }
        });
        using var outer = joined ? ItemContainerTransaction.Begin(bank.Items.Mutations, moneyPouch.Mutations) : null;

        Assert.IsFalse(bank.DepositFromMoneyPouch(out var deposited));

        Assert.IsNull(deposited);
        if (joined)
        {
            Assert.AreSame(outer, bankStorage.Transaction);
            scenario.Owner.DidNotReceive().SendChatMessage(Arg.Any<string>());
            outer!.Dispose();
        }
        else scenario.Owner.Received(1).SendChatMessage("Not enough space in your bank.");
        Assert.AreEqual(5, moneyPouch.Count);
        Assert.AreEqual(int.MaxValue, bank.Items.GetCountById(995));
        events.DidNotReceive().SendEvent(Arg.Any<BankChangedEvent>());
        events.DidNotReceive().SendEvent(Arg.Any<MoneyPouchChangedEvent>());
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
        inventory.Items.Add(note);

        Assert.IsTrue(bank.DepositFromInventory(note, 2, out var deposited));

        Assert.AreEqual(101, deposited!.Id);
        Assert.AreEqual(2, deposited.Count);
        Assert.AreEqual(2, inventory.Items.GetCountById(201));
        Assert.AreEqual(2, bank.Items.GetCountById(101));
        CollectionAssert.AreEqual(new long[] { 7, 9 }, bank.Items[0]!.ExtraData);
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
        bank.Items.Add(item);

        Assert.IsTrue(bank.WithdrawFromBank(item, 3, notingEnabled: true, out var withdrawn));

        Assert.AreEqual(201, withdrawn!.Id);
        Assert.AreEqual(3, inventory.Items.GetCountById(201));
        Assert.AreEqual(2, bank.Items.GetCountById(101));
        CollectionAssert.AreEqual(new long[] { 11, 22 }, inventory.Items[0]!.ExtraData);
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
        inventory.Items.Add(scenario.Builder.Create().WithId(102).WithCount(1).Build());
        var item = scenario.Builder.Create().WithId(101).WithCount(5).Build();
        bank.Items.Add(item);

        Assert.IsTrue(bank.WithdrawFromBank(item, 3, notingEnabled: false, out var withdrawn));

        Assert.AreEqual(1, withdrawn!.Count);
        Assert.AreEqual(4, bank.Items.GetCountById(101));
        Assert.AreEqual(1, inventory.Items.GetCountById(101));
        Assert.AreEqual(2, inventory.Items.TakenSlots);
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
        equipment.TryRestoreEquippedItem(EquipmentSlot.Hat, item);
        var callbackSawCommittedStorage = false;
        item.EquipmentScript.When(script => script.OnUnequipped(item, scenario.Owner)).Do(_ =>
        {
            Assert.IsNull(equipment[EquipmentSlot.Hat]);
            Assert.AreSame(item, bank.Items[0]);
            callbackSawCommittedStorage = true;
        });
        var eventManager = Substitute.For<IEventManager>();
        scenario.Owner.EventManager.Returns(eventManager);
        var publicationSawEquipmentEffect = false;
        TrackPublicationAfterCallback(
            eventManager,
            gameEvent => gameEvent is BankChangedEvent or EquipmentChangedEvent,
            () => callbackSawCommittedStorage,
            observed => publicationSawEquipmentEffect = observed);

        Assert.IsTrue(bank.DepositFromEquipment(item, 1, out _));

        Assert.IsTrue(callbackSawCommittedStorage);
        Assert.IsTrue(publicationSawEquipmentEffect);
        eventManager.Received(1).SendEvent(Arg.Is<IEvent>(gameEvent => gameEvent is EquipmentChangedEvent));
        eventManager.Received(1).SendEvent(Arg.Is<IEvent>(gameEvent => gameEvent is BankChangedEvent));
    }

    [TestMethod]
    public void ClearEquipment_ClearsStorageBeforeDomainCallbacksAndPublication()
    {
        using var scenario = new Scenario();
        var eventManager = Substitute.For<IEventManager>();
        scenario.Owner.EventManager.Returns(eventManager);
        var equipment = new EquipmentContainer(scenario.Owner, 15, scenario.Builder);
        scenario.Owner.Equipment.Returns(equipment);
        var item = scenario.Builder.Create().WithId(101).WithCount(1).Build();
        equipment.TryRestoreEquippedItem(EquipmentSlot.Hat, item);
        eventManager.ClearReceivedCalls();
        var order = ObserveUnequipBeforePublication(eventManager, equipment, item, scenario.Owner);

        equipment.ClearEquipment();

        Assert.IsNull(equipment[EquipmentSlot.Hat]);
        CollectionAssert.AreEqual(new[] { "unequip", "publish" }, order);
        eventManager.Received(1).SendEvent(Arg.Is<IEvent>(gameEvent => gameEvent is EquipmentChangedEvent));
    }

    [TestMethod]
    public void ClearEquipment_WhenCallbackThrows_AttemptsEveryCallbackAndPublication()
    {
        using var scenario = new Scenario();
        var eventManager = Substitute.For<IEventManager>();
        scenario.Owner.EventManager.Returns(eventManager);
        var equipment = new EquipmentContainer(scenario.Owner, 15, scenario.Builder);
        var first = scenario.Builder.Create().WithId(101).WithCount(1).Build();
        var second = scenario.Builder.Create().WithId(102).WithCount(1).Build();
        Assert.IsTrue(equipment.TryRestoreEquippedItem(EquipmentSlot.Hat, first));
        Assert.IsTrue(equipment.TryRestoreEquippedItem(EquipmentSlot.Amulet, second));
        eventManager.ClearReceivedCalls();
        var failure = new InvalidOperationException("first unequip failed");
        var order = new List<string>();
        first.EquipmentScript.When(script => script.OnUnequipped(first, scenario.Owner)).Do(_ =>
        {
            Assert.IsNull(equipment[EquipmentSlot.Hat]);
            Assert.IsNull(equipment[EquipmentSlot.Amulet]);
            order.Add("first");
            throw failure;
        });
        second.EquipmentScript.When(script => script.OnUnequipped(second, scenario.Owner)).Do(_ =>
        {
            Assert.IsNull(equipment[EquipmentSlot.Hat]);
            Assert.IsNull(equipment[EquipmentSlot.Amulet]);
            order.Add("second");
        });
        eventManager.When(manager => manager.SendEvent(Arg.Any<IEvent>())).Do(call =>
        {
            if (call.Arg<IEvent>() is EquipmentChangedEvent)
            {
                CollectionAssert.AreEqual(new[] { "first", "second" }, order);
                order.Add("publish");
            }
        });

        var thrown = Assert.ThrowsExactly<InvalidOperationException>(() => equipment.ClearEquipment());

        Assert.AreSame(failure, thrown);
        Assert.IsTrue(equipment.All(item => item == null));
        CollectionAssert.AreEqual(new[] { "first", "second", "publish" }, order);
        eventManager.Received(1).SendEvent(Arg.Is<IEvent>(gameEvent => gameEvent is EquipmentChangedEvent));
    }

    [TestMethod]
    public void ShopSell_TransfersTheExactItemQuantityToStock()
    {
        using var scenario = new Scenario();
        scenario.DefineItem(101, stackable: true);
        var (inventory, stock, moneyPouch) = CreateShopScenario(scenario);
        var item = scenario.Builder.Create().WithId(101).WithCount(5).WithExtraData("11,22").Build();
        item.ItemScript.CanSellItem(Arg.Any<IItem>(), scenario.Owner).Returns(true);
        inventory.Items.Add(item);

        Assert.IsTrue(stock.SellFromInventory(scenario.Owner, item, 3));

        Assert.AreEqual(2, inventory.Items.GetCountById(101));
        Assert.AreEqual(3, stock.Items.GetCountById(101));
        CollectionAssert.AreEqual(new long[] { 11, 22 }, stock.Items[0]!.ExtraData);
        Assert.AreEqual(6, moneyPouch.Count);
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
        inventory.Items.Add(item);

        Assert.IsTrue(stock.SellFromInventory(scenario.Owner, item, 10));

        Assert.AreEqual(0, inventory.Items.GetCountById(201));
        Assert.AreEqual(5, stock.Items.GetCountById(101));
        CollectionAssert.AreEqual(new long[] { 11, 22 }, stock.Items[0]!.ExtraData);
        Assert.AreEqual(10, moneyPouch.Count);
    }

    [TestMethod]
    public void FamiliarInventory_TransfersBothDirectionsWithExactCounts()
    {
        using var scenario = new Scenario();
        var inventory = CreateInventory(scenario, 4);
        scenario.Owner.Inventory.Returns(inventory);
        var familiar = new FamiliarInventoryContainer(scenario.Owner, StorageType.Normal, 4, scenario.Builder);
        var item = scenario.Builder.Create().WithId(101).WithCount(5).Build();
        inventory.Items.Add(item);

        Assert.IsTrue(familiar.DepositFromInventory(item, 3));
        Assert.AreEqual(2, inventory.Items.GetCountById(101));
        Assert.AreEqual(3, familiar.Items.GetCountById(101));

        var familiarItem = familiar.Items[0]!;
        Assert.IsTrue(familiar.WithdrawFromFamiliarInventory(familiarItem, 2));
        Assert.AreEqual(4, inventory.Items.GetCountById(101));
        Assert.AreEqual(1, familiar.Items.GetCountById(101));
    }

    [TestMethod]
    public void FamiliarInventory_WithdrawAvailableToInventory_MovesAllFittingItemsInOnePublication()
    {
        using var scenario = new Scenario();
        scenario.DefineItem(101, stackable: false);
        scenario.DefineItem(102, stackable: false);
        var inventory = CreateInventory(scenario, 2);
        scenario.Owner.Inventory.Returns(inventory);
        var familiar = new FamiliarInventoryContainer(scenario.Owner, StorageType.Normal, 2, scenario.Builder);
        Assert.IsTrue(familiar.Items.Add(scenario.Builder.Create().WithId(101).WithCount(1).Build()));
        Assert.IsTrue(familiar.Items.Add(scenario.Builder.Create().WithId(102).WithCount(1).Build()));
        var eventManager = Substitute.For<IEventManager>();
        scenario.Owner.EventManager.Returns(eventManager);
        eventManager.ClearReceivedCalls();

        familiar.WithdrawAvailableToInventory();

        Assert.AreEqual(0, familiar.Items.TakenSlots);
        Assert.AreEqual(1, inventory.Items.GetCountById(101));
        Assert.AreEqual(1, inventory.Items.GetCountById(102));
        eventManager.Received(1).SendEvent(Arg.Is<IEvent>(gameEvent => gameEvent is FamiliarInventoryChangedEvent));
        eventManager.Received(1).SendEvent(Arg.Is<IEvent>(gameEvent => gameEvent is InventoryChangedEvent));
    }

    [TestMethod]
    public void FamiliarInventory_WithdrawAvailableToInventory_MovesFittingItemsAndRetainsTheRest()
    {
        using var scenario = new Scenario();
        scenario.DefineItem(101, stackable: false);
        scenario.DefineItem(102, stackable: false);
        scenario.DefineItem(103, stackable: false);
        var inventory = CreateInventory(scenario, 3);
        scenario.Owner.Inventory.Returns(inventory);
        Assert.IsTrue(inventory.Items.Add(scenario.Builder.Create().WithId(199).WithCount(1).Build()));
        var familiar = new FamiliarInventoryContainer(scenario.Owner, StorageType.Normal, 3, scenario.Builder);
        Assert.IsTrue(familiar.Items.Add(scenario.Builder.Create().WithId(101).WithCount(1).Build()));
        Assert.IsTrue(familiar.Items.Add(scenario.Builder.Create().WithId(102).WithCount(1).Build()));
        Assert.IsTrue(familiar.Items.Add(scenario.Builder.Create().WithId(103).WithCount(1).Build()));
        var eventManager = Substitute.For<IEventManager>();
        scenario.Owner.EventManager.Returns(eventManager);
        eventManager.ClearReceivedCalls();

        familiar.WithdrawAvailableToInventory();

        Assert.AreEqual(1, familiar.Items.TakenSlots);
        Assert.AreEqual(1, familiar.Items.GetCountById(103));
        Assert.AreEqual(1, inventory.Items.GetCountById(199));
        Assert.AreEqual(1, inventory.Items.GetCountById(101));
        Assert.AreEqual(1, inventory.Items.GetCountById(102));
        eventManager.Received(1).SendEvent(Arg.Is<IEvent>(gameEvent => gameEvent is FamiliarInventoryChangedEvent));
        eventManager.Received(1).SendEvent(Arg.Is<IEvent>(gameEvent => gameEvent is InventoryChangedEvent));
    }

    [TestMethod]
    public void FamiliarInventory_WithdrawAvailableToInventory_WhenNothingFitsLeavesStateAndPublishesNothing()
    {
        using var scenario = new Scenario();
        scenario.DefineItem(101, stackable: false);
        var inventory = CreateInventory(scenario, 1);
        scenario.Owner.Inventory.Returns(inventory);
        Assert.IsTrue(inventory.Items.Add(scenario.Builder.Create().WithId(199).WithCount(1).Build()));
        var familiar = new FamiliarInventoryContainer(scenario.Owner, StorageType.Normal, 1, scenario.Builder);
        var familiarItem = scenario.Builder.Create().WithId(101).WithCount(1).Build();
        Assert.IsTrue(familiar.Items.Add(familiarItem));
        var eventManager = Substitute.For<IEventManager>();
        scenario.Owner.EventManager.Returns(eventManager);
        eventManager.ClearReceivedCalls();

        familiar.WithdrawAvailableToInventory();

        Assert.AreEqual(familiarItem.Id, familiar.Items[0]?.Id);
        Assert.AreEqual(1, familiar.Items[0]?.Count);
        Assert.AreEqual(1, inventory.Items.GetCountById(199));
        eventManager.DidNotReceive().SendEvent(Arg.Any<IEvent>());
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
        inventory.Items.Add(scenario.Builder.Create().WithId(102).WithCount(1).Build());
        var rewardItem = scenario.Builder.Create().WithId(101).WithCount(4).Build();
        rewards.Items.Add(rewardItem);

        Assert.AreEqual(1, rewards.Claim(rewardItem, 3));

        Assert.AreEqual(3, rewards.Items.GetCountById(101));
        Assert.AreEqual(1, inventory.Items.GetCountById(101));
        Assert.AreEqual(2, inventory.Items.TakenSlots);
    }

    [TestMethod]
    public void EquipItem_EmptySlotCallsEquippedAfterStorageCommit()
    {
        using var scenario = new Scenario();
        var (inventory, equipment, item) = CreateEquipmentSetup(scenario);
        var callbackSawCommittedStorage = ObserveEquippedState(scenario.Owner, inventory.Items, equipment, item);

        Assert.IsTrue(equipment.EquipItem(item));

        Assert.IsTrue(callbackSawCommittedStorage());
    }

    [TestMethod]
    public void EquipItem_TwoHandedReplacementPreflightsEveryConflictingItemBeforeMutation()
    {
        using var scenario = new Scenario();
        var inventory = CreateInventory(scenario, 4);
        scenario.Owner.Inventory.Returns(inventory);
        var eventManager = Substitute.For<IEventManager>();
        scenario.Owner.EventManager.Returns(eventManager);
        var equipment = new EquipmentContainer(scenario.Owner, 15, scenario.Builder);
        scenario.Owner.Equipment.Returns(equipment);
        var weaponDefinition = Substitute.For<IEquipmentDefinition>();
        weaponDefinition.Slot.Returns(EquipmentSlot.Weapon);
        scenario.DefineEquipment(101, weaponDefinition);
        var shieldDefinition = Substitute.For<IEquipmentDefinition>();
        shieldDefinition.Slot.Returns(EquipmentSlot.Shield);
        scenario.DefineEquipment(102, shieldDefinition);
        var incomingDefinition = Substitute.For<IEquipmentDefinition>();
        incomingDefinition.Slot.Returns(EquipmentSlot.Weapon);
        incomingDefinition.Type.Returns(EquipmentType.TwoHanded);
        scenario.DefineEquipment(103, incomingDefinition);
        var weapon = scenario.Builder.Create().WithId(101).WithCount(1).Build();
        var shield = scenario.Builder.Create().WithId(102).WithCount(1).Build();
        var incoming = scenario.Builder.Create().WithId(103).WithCount(1).Build();
        Assert.IsTrue(equipment.TryRestoreEquippedItem(EquipmentSlot.Weapon, weapon));
        Assert.IsTrue(equipment.TryRestoreEquippedItem(EquipmentSlot.Shield, shield));
        Assert.IsTrue(inventory.Items.Add(incoming));
        eventManager.ClearReceivedCalls();
        incoming.EquipmentScript.CanEquipItem(incoming, scenario.Owner).Returns(true);
        weapon.EquipmentScript.CanUnEquipItem(weapon, scenario.Owner).Returns(true);
        shield.EquipmentScript.CanUnEquipItem(shield, scenario.Owner).Returns(false);

        Assert.IsFalse(equipment.EquipItem(incoming));

        Assert.AreSame(incoming, inventory.Items[0]);
        Assert.AreSame(weapon, equipment[EquipmentSlot.Weapon]);
        Assert.AreSame(shield, equipment[EquipmentSlot.Shield]);
        weapon.EquipmentScript.Received(1).CanUnEquipItem(weapon, scenario.Owner);
        shield.EquipmentScript.Received(1).CanUnEquipItem(shield, scenario.Owner);
        weapon.EquipmentScript.DidNotReceive().UnEquipItem(weapon, scenario.Owner, Arg.Any<int>());
        shield.EquipmentScript.DidNotReceive().UnEquipItem(shield, scenario.Owner, Arg.Any<int>());
        incoming.EquipmentScript.DidNotReceive().OnEquipped(incoming, scenario.Owner);
        eventManager.DidNotReceive().SendEvent(Arg.Any<IEvent>());
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void EquipItem_TwoConflictsThatCannotBothFitRollsBackEveryStorageChange(bool joined)
    {
        using var scenario = new Scenario();
        var setup = CreateWeaponShieldReplacementSetup(scenario, inventoryCapacity: 1);
        setup.Weapon.EquipmentScript.CanUnEquipItem(setup.Weapon, scenario.Owner).Returns(true);
        setup.Shield.EquipmentScript.CanUnEquipItem(setup.Shield, scenario.Owner).Returns(true);
        scenario.Owner.ClearReceivedCalls();
        var equipmentBoundary = (ItemContainerMutationBoundary)typeof(EquipmentContainer)
            .GetField("_mutations", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(setup.Equipment)!;
        using var outer = joined ? ItemContainerTransaction.Begin(setup.Inventory.Items.Mutations, equipmentBoundary) : null;

        Assert.IsFalse(setup.Equipment.EquipItem(setup.Incoming));

        if (joined)
        {
            Assert.AreSame(outer, equipmentBoundary.Storage.Transaction);
            scenario.Owner.DidNotReceive().SendChatMessage(Arg.Any<string>());
            outer!.Dispose();
        }
        else scenario.Owner.Received(1).SendChatMessage("Not enough space in your inventory.");
        Assert.AreSame(setup.Incoming, setup.Inventory.Items[0]);
        Assert.AreSame(setup.Weapon, setup.Equipment[EquipmentSlot.Weapon]);
        Assert.AreSame(setup.Shield, setup.Equipment[EquipmentSlot.Shield]);
        setup.Weapon.EquipmentScript.DidNotReceive().OnUnequipped(setup.Weapon, scenario.Owner);
        setup.Shield.EquipmentScript.DidNotReceive().OnUnequipped(setup.Shield, scenario.Owner);
        setup.Incoming.EquipmentScript.DidNotReceive().OnEquipped(setup.Incoming, scenario.Owner);
        scenario.Owner.EventManager.DidNotReceive().SendEvent(Arg.Any<IEvent>());
    }

    [TestMethod]
    public void EquipItem_TwoConflictsCommitStorageBeforeOrderedLifecycleCallbacks()
    {
        using var scenario = new Scenario();
        var setup = CreateWeaponShieldReplacementSetup(scenario, inventoryCapacity: 3);
        var callbackOrder = new List<string>();
        var publicationFollowedCallbacks = false;
        var mediator = scenario.Owner.Mediator;
        mediator.When(bus => bus.Publish(Arg.Any<ProfileSetBoolAction>())).Do(_ => callbackOrder.Add("profile"));
        scenario.Owner.EventManager.When(manager => manager.SendEvent(Arg.Any<IEvent>())).Do(_ =>
            publicationFollowedCallbacks |= callbackOrder.Count == 4);
        setup.Weapon.EquipmentScript.When(script => script.OnUnequipped(setup.Weapon, scenario.Owner)).Do(_ =>
        {
            Assert.AreSame(setup.Incoming, setup.Equipment[EquipmentSlot.Weapon]);
            Assert.IsNull(setup.Equipment[EquipmentSlot.Shield]);
            Assert.AreSame(setup.Weapon, setup.Inventory.Items[0]);
            callbackOrder.Add("weapon");
        });
        setup.Shield.EquipmentScript.When(script => script.OnUnequipped(setup.Shield, scenario.Owner)).Do(_ =>
        {
            Assert.AreSame(setup.Incoming, setup.Equipment[EquipmentSlot.Weapon]);
            Assert.IsNull(setup.Equipment[EquipmentSlot.Shield]);
            Assert.AreSame(setup.Shield, setup.Inventory.Items[1]);
            callbackOrder.Add("shield");
        });
        setup.Incoming.EquipmentScript.When(script => script.OnEquipped(setup.Incoming, scenario.Owner)).Do(_ =>
        {
            Assert.AreSame(setup.Incoming, setup.Equipment[EquipmentSlot.Weapon]);
            Assert.AreSame(setup.Weapon, setup.Inventory.Items[0]);
            Assert.AreSame(setup.Shield, setup.Inventory.Items[1]);
            callbackOrder.Add("incoming");
        });
        setup.Weapon.EquipmentScript.CanUnEquipItem(setup.Weapon, scenario.Owner).Returns(true);
        setup.Shield.EquipmentScript.CanUnEquipItem(setup.Shield, scenario.Owner).Returns(true);

        Assert.IsTrue(setup.Equipment.EquipItem(setup.Incoming));

        CollectionAssert.AreEqual(new[] { "weapon", "shield", "profile", "incoming" }, callbackOrder);
        Assert.IsTrue(publicationFollowedCallbacks);
        Assert.AreSame(setup.Incoming, setup.Equipment[EquipmentSlot.Weapon]);
        Assert.AreSame(setup.Weapon, setup.Inventory.Items[0]);
        Assert.AreSame(setup.Shield, setup.Inventory.Items[1]);
    }

    [TestMethod]
    public void EquipItem_TwoConflictPublisherFailureLeavesCommittedStateAndRunsCallbacks()
    {
        using var scenario = new Scenario();
        var setup = CreateWeaponShieldReplacementSetup(scenario, inventoryCapacity: 3);
        var eventManager = Substitute.For<IEventManager>();
        scenario.Owner.EventManager.Returns(eventManager);
        var callbacks = new List<string>();
        setup.Weapon.EquipmentScript.When(script => script.OnUnequipped(setup.Weapon, scenario.Owner)).Do(_ => callbacks.Add("weapon"));
        setup.Shield.EquipmentScript.When(script => script.OnUnequipped(setup.Shield, scenario.Owner)).Do(_ => callbacks.Add("shield"));
        setup.Incoming.EquipmentScript.When(script => script.OnEquipped(setup.Incoming, scenario.Owner)).Do(_ => callbacks.Add("incoming"));
        setup.Weapon.EquipmentScript.CanUnEquipItem(setup.Weapon, scenario.Owner).Returns(true);
        setup.Shield.EquipmentScript.CanUnEquipItem(setup.Shield, scenario.Owner).Returns(true);
        eventManager.When(manager => manager.SendEvent(Arg.Any<IEvent>())).Do(call =>
        {
            if (call.Arg<IEvent>() is InventoryChangedEvent)
                throw new InvalidOperationException("Controlled inventory publication failure.");
        });

        Assert.ThrowsExactly<InvalidOperationException>(() => setup.Equipment.EquipItem(setup.Incoming));

        CollectionAssert.AreEqual(new[] { "weapon", "shield", "incoming" }, callbacks);
        Assert.AreSame(setup.Incoming, setup.Equipment[EquipmentSlot.Weapon]);
        Assert.IsNull(setup.Equipment[EquipmentSlot.Shield]);
        Assert.AreSame(setup.Weapon, setup.Inventory.Items[0]);
        Assert.AreSame(setup.Shield, setup.Inventory.Items[1]);
        eventManager.DidNotReceive().SendEvent(Arg.Is<IEvent>(gameEvent => gameEvent is EquipmentChangedEvent));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void EquipItem_HookAndPublisherFailuresPreserveBothAfterAttemptingOwnedHooks(bool multipleHooksFail)
    {
        using var scenario = new Scenario();
        var setup = CreateWeaponShieldReplacementSetup(scenario, inventoryCapacity: 3);
        var hookFailure = new InvalidOperationException("Weapon lifecycle failed.");
        var shieldFailure = new InvalidOperationException("Shield lifecycle failed.");
        var incomingFailure = new InvalidOperationException("Incoming lifecycle failed.");
        var publicationFailure = new InvalidOperationException("Inventory publication failed.");
        var callbacks = new List<string>();
        setup.Weapon.EquipmentScript.CanUnEquipItem(setup.Weapon, scenario.Owner).Returns(true);
        setup.Shield.EquipmentScript.CanUnEquipItem(setup.Shield, scenario.Owner).Returns(true);
        setup.Weapon.EquipmentScript.When(script => script.OnUnequipped(setup.Weapon, scenario.Owner)).Do(_ =>
        {
            var equipmentStorage = (ItemContainerStorage)typeof(EquipmentContainer)
                .GetField("_storage", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(setup.Equipment)!;
            Assert.IsFalse(Monitor.IsEntered(equipmentStorage.MutationLock));
            Assert.IsNull(equipmentStorage.Transaction);
            Assert.IsNull(((ItemContainerMutationBoundary)setup.Inventory.Items.Mutations).Storage.Transaction);
            callbacks.Add("weapon");
            throw hookFailure;
        });
        setup.Shield.EquipmentScript.When(script => script.OnUnequipped(setup.Shield, scenario.Owner)).Do(_ =>
        {
            callbacks.Add("shield");
            if (multipleHooksFail) throw shieldFailure;
        });
        scenario.Owner.Mediator.When(bus => bus.Publish(Arg.Any<ProfileSetBoolAction>())).Do(_ => callbacks.Add("profile"));
        setup.Incoming.EquipmentScript.When(script => script.OnEquipped(setup.Incoming, scenario.Owner)).Do(_ =>
        {
            callbacks.Add("incoming");
            if (multipleHooksFail) throw incomingFailure;
        });
        scenario.Owner.EventManager.When(manager => manager.SendEvent(Arg.Any<IEvent>())).Do(call =>
        {
            if (call.Arg<IEvent>() is InventoryChangedEvent) throw publicationFailure;
        });

        var thrown = Assert.ThrowsExactly<AggregateException>(() => setup.Equipment.EquipItem(setup.Incoming));

        CollectionAssert.AreEqual(new[] { "weapon", "shield", "profile", "incoming" }, callbacks);
        var expected = multipleHooksFail
            ? new Exception[] { hookFailure, shieldFailure, incomingFailure, publicationFailure }
            : new Exception[] { hookFailure, publicationFailure };
        CollectionAssert.AreEqual(expected, thrown.InnerExceptions.ToArray());
        Assert.IsNotNull(hookFailure.StackTrace);
        Assert.IsNotNull(publicationFailure.StackTrace);
        Assert.AreSame(setup.Incoming, setup.Equipment[EquipmentSlot.Weapon]);
        Assert.IsNull(setup.Equipment[EquipmentSlot.Shield]);
        Assert.AreSame(setup.Weapon, setup.Inventory.Items[0]);
        Assert.AreSame(setup.Shield, setup.Inventory.Items[1]);
        scenario.Owner.EventManager.DidNotReceive().SendEvent(Arg.Any<EquipmentChangedEvent>());
    }

    [TestMethod]
    public void EquipItem_NonWeaponReplacementStillInvokesAndHonorsUnequipCommand()
    {
        using var scenario = new Scenario();
        var inventory = CreateInventory(scenario, 2);
        scenario.Owner.Inventory.Returns(inventory);
        var equipment = new EquipmentContainer(scenario.Owner, 15, scenario.Builder);
        scenario.Owner.Equipment.Returns(equipment);
        scenario.DefaultEquipmentDefinition.Slot.Returns(EquipmentSlot.Hat);
        var equipped = scenario.Builder.Create().WithId(101).WithCount(1).Build();
        var incoming = scenario.Builder.Create().WithId(102).WithCount(1).Build();
        Assert.IsTrue(equipment.TryRestoreEquippedItem(EquipmentSlot.Hat, equipped));
        Assert.IsTrue(inventory.Items.Add(incoming));
        incoming.EquipmentScript.CanEquipItem(incoming, scenario.Owner).Returns(true);
        equipped.EquipmentScript.CanUnEquipItem(equipped, scenario.Owner).Returns(true);
        var publications = 0;
        scenario.Owner.EventManager.When(manager => manager.SendEvent(Arg.Any<InventoryChangedEvent>())).Do(_ => publications++);
        equipped.EquipmentScript.UnEquipItem(equipped, scenario.Owner, 0).Returns(_ =>
        {
            Assert.AreEqual(1, publications);
            var storage = (ItemContainerStorage)typeof(EquipmentContainer)
                .GetField("_storage", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(equipment)!;
            Assert.IsNull(storage.Transaction);
            Assert.IsFalse(Monitor.IsEntered(storage.MutationLock));
            Assert.IsNull(((ItemContainerMutationBoundary)inventory.Items.Mutations).Storage.Transaction);
            return false;
        });

        Assert.IsFalse(equipment.EquipItem(incoming));

        Assert.AreSame(incoming, inventory.Items[0]);
        Assert.AreSame(equipped, equipment[EquipmentSlot.Hat]);
        Assert.AreEqual(2, publications);
        equipped.EquipmentScript.Received(1).UnEquipItem(equipped, scenario.Owner, 0);
        incoming.EquipmentScript.DidNotReceive().OnEquipped(incoming, scenario.Owner);

        using var transaction = ItemContainerTransaction.Begin(inventory.Items.Mutations);
        Assert.ThrowsExactly<InvalidOperationException>(() => equipment.EquipItem(incoming));
        Assert.AreSame(transaction, ((ItemContainerMutationBoundary)inventory.Items.Mutations).Storage.Transaction);
        Assert.AreSame(incoming, inventory.Items[0]);
        Assert.AreSame(equipped, equipment[EquipmentSlot.Hat]);
        Assert.AreEqual(2, publications);
        equipped.EquipmentScript.Received(1).UnEquipItem(equipped, scenario.Owner, 0);
    }

    [TestMethod]
    public void EquipItem_SingleSlotReplacementPreflightRejectsWithoutMutation()
    {
        using var scenario = new Scenario();
        var inventory = CreateInventory(scenario, 2);
        scenario.Owner.Inventory.Returns(inventory);
        var eventManager = Substitute.For<IEventManager>();
        scenario.Owner.EventManager.Returns(eventManager);
        var equipment = new EquipmentContainer(scenario.Owner, 15, scenario.Builder);
        scenario.Owner.Equipment.Returns(equipment);
        var equippedDefinition = Substitute.For<IEquipmentDefinition>();
        equippedDefinition.Slot.Returns(EquipmentSlot.Hat);
        scenario.DefineEquipment(101, equippedDefinition);
        var incomingDefinition = Substitute.For<IEquipmentDefinition>();
        incomingDefinition.Slot.Returns(EquipmentSlot.Hat);
        scenario.DefineEquipment(102, incomingDefinition);
        var equipped = scenario.Builder.Create().WithId(101).WithCount(1).Build();
        var incoming = scenario.Builder.Create().WithId(102).WithCount(1).Build();
        Assert.IsTrue(equipment.TryRestoreEquippedItem(EquipmentSlot.Hat, equipped));
        Assert.IsTrue(inventory.Items.Add(incoming));
        eventManager.ClearReceivedCalls();
        incoming.EquipmentScript.CanEquipItem(incoming, scenario.Owner).Returns(true);
        equipped.EquipmentScript.CanUnEquipItem(equipped, scenario.Owner).Returns(false);

        Assert.IsFalse(equipment.EquipItem(incoming));

        Assert.AreSame(incoming, inventory.Items[0]);
        Assert.AreSame(equipped, equipment[EquipmentSlot.Hat]);
        equipped.EquipmentScript.DidNotReceive().UnEquipItem(equipped, scenario.Owner, Arg.Any<int>());
        incoming.EquipmentScript.DidNotReceive().OnEquipped(incoming, scenario.Owner);
        eventManager.DidNotReceive().SendEvent(Arg.Any<IEvent>());
    }

    [TestMethod]
    public void EquipItem_EquipmentPublicationThrowsAfterDomainEffectAndStorageCommit()
    {
        using var scenario = new Scenario();
        var (inventory, equipment, item) = CreateEquipmentSetup(scenario);
        var callbackSawCommittedStorage = ObserveEquippedState(scenario.Owner, inventory.Items, equipment, item);
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
        Assert.IsNull(inventory.Items[0]);
        Assert.AreSame(item, equipment[EquipmentSlot.Hat]);
        eventManager.Received(1).SendEvent(Arg.Is<IEvent>(gameEvent => gameEvent is InventoryChangedEvent));
        eventManager.Received(1).SendEvent(Arg.Is<IEvent>(gameEvent => gameEvent is EquipmentChangedEvent));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void UnEquipItem_InventoryFullLeavesEquipmentWithoutUnequippedCallback(bool joined)
    {
        using var scenario = new Scenario();
        var inventory = CreateInventory(scenario, 1);
        scenario.Owner.Inventory.Returns(inventory);
        var equipment = new EquipmentContainer(scenario.Owner, 15, scenario.Builder);
        scenario.Owner.Equipment.Returns(equipment);
        scenario.DefaultEquipmentDefinition.Slot.Returns(EquipmentSlot.Hat);
        var item = scenario.Builder.Create().WithId(101).WithCount(1).Build();
        equipment.TryRestoreEquippedItem(EquipmentSlot.Hat, item);
        inventory.Items.Add(scenario.Builder.Create().WithId(102).WithCount(1).Build());
        item.EquipmentScript.CanUnEquipItem(item, scenario.Owner).Returns(true);
        scenario.Owner.ClearReceivedCalls();
        var equipmentBoundary = (ItemContainerMutationBoundary)typeof(EquipmentContainer)
            .GetField("_mutations", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(equipment)!;
        var inventoryStorage = ((ItemContainerMutationBoundary)inventory.Items.Mutations).Storage;
        scenario.Owner.When(owner => owner.SendChatMessage(Arg.Any<string>())).Do(_ =>
        {
            Assert.IsNull(equipmentBoundary.Storage.Transaction);
            Assert.IsFalse(Monitor.IsEntered(equipmentBoundary.Storage.MutationLock));
            Assert.IsNull(inventoryStorage.Transaction);
            Assert.IsFalse(Monitor.IsEntered(inventoryStorage.MutationLock));
        });
        using var outer = joined ? ItemContainerTransaction.Begin(inventory.Items.Mutations, equipmentBoundary) : null;

        Assert.IsFalse(equipment.UnEquipItem(item));

        if (joined)
        {
            Assert.AreSame(outer, equipmentBoundary.Storage.Transaction);
            scenario.Owner.DidNotReceive().SendChatMessage(Arg.Any<string>());
        }
        else scenario.Owner.Received(1).SendChatMessage("Not enough space in your inventory.");
        Assert.AreSame(item, equipment[EquipmentSlot.Hat]);
        Assert.AreEqual(1, inventory.Items.GetCountById(102));
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
        equipment.TryRestoreEquippedItem(EquipmentSlot.Hat, item);
        item.EquipmentScript.CanUnEquipItem(item, scenario.Owner).Returns(true);
        var callbackSawCommittedStorage = false;
        item.EquipmentScript.When(script => script.OnUnequipped(item, scenario.Owner)).Do(_ =>
        {
            Assert.IsNull(equipment[EquipmentSlot.Hat]);
            Assert.AreSame(item, inventory.Items[0]);
            callbackSawCommittedStorage = true;
        });
        var eventManager = Substitute.For<IEventManager>();
        scenario.Owner.EventManager.Returns(eventManager);
        var publicationSawEquipmentEffect = false;
        TrackPublicationAfterCallback(
            eventManager,
            gameEvent => gameEvent is InventoryChangedEvent or EquipmentChangedEvent,
            () => callbackSawCommittedStorage,
            observed => publicationSawEquipmentEffect = observed);

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
        inventory.Items.Add(scenario.Builder.Create().WithId(102).WithCount(1).Build());
        var rewardItem = scenario.Builder.Create().WithId(101).WithCount(4).Build();
        rewards.Items.Add(rewardItem);

        Assert.AreEqual(-1, rewards.Claim(rewardItem, 1));

        Assert.AreEqual(4, rewards.Items.GetCountById(101));
        Assert.AreEqual(0, inventory.Items.GetCountById(101));
        Assert.AreEqual(1, inventory.Items.GetCountById(102));
    }
    [TestMethod]
    public void Dispose_EquipmentAndPouchEffectsAreDiscardedWithoutLeakingIntoLaterScope()
    {
        using var scenario = new Scenario();
        var (equipment, events, current, replacement) = CreateReplacementScenario(scenario);
        var inventory = CreateInventory(scenario, 4);
        scenario.Owner.Inventory.Returns(inventory);
        var pouch = new MoneyPouchContainer(scenario.Owner, new ComposedTestItemBuilder());
        Assert.IsTrue(pouch.TryAddExact(10));
        scenario.Owner.ClearReceivedCalls();
        events.ClearReceivedCalls();
        var boundary = (ItemContainerMutationBoundary)typeof(EquipmentContainer)
            .GetField("_mutations", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(equipment)!;
        using (ItemContainerTransaction.Begin(boundary, pouch.Mutations, inventory.Items.Mutations))
        {
            Assert.IsTrue(equipment.TryReplaceEquippedItem(EquipmentSlot.Hat, current, replacement));
            Assert.IsTrue(pouch.TryAddExact(2));
            Assert.IsTrue(pouch.MoveToInventory(3));
            Assert.AreSame(replacement, equipment[EquipmentSlot.Hat]);
            Assert.AreEqual(9, pouch.Count);
            Assert.AreEqual(3, inventory.Items.GetCountById(995));
        }
        Assert.AreSame(current, equipment[EquipmentSlot.Hat]);
        Assert.AreEqual(10, pouch.Count);
        Assert.AreEqual(0, inventory.Items.GetCountById(995));
        using (var fresh = ItemContainerTransaction.Begin(pouch.Mutations, boundary)) fresh.Commit();
        current.EquipmentScript.DidNotReceive().OnUnequipped(current, scenario.Owner);
        replacement.EquipmentScript.DidNotReceive().OnEquipped(replacement, scenario.Owner);
        scenario.Owner.DidNotReceive().SendChatMessage(Arg.Any<string>());
        events.DidNotReceive().SendEvent(Arg.Any<IEvent>());
    }

    [TestMethod]
    public void Commit_MultipleEquipmentMutationsCompleteOnceBeforePublicationDespiteAliases()
    {
        using var scenario = new Scenario();
        var (equipment, events, current, replacement) = CreateReplacementScenario(scenario);
        var finalItem = scenario.Builder.Create().WithId(103).WithCount(1).Build();
        var order = new List<string>();
        current.EquipmentScript.When(script => script.OnUnequipped(current, scenario.Owner)).Do(_ => order.Add("old unequip"));
        replacement.EquipmentScript.When(script => script.OnEquipped(replacement, scenario.Owner)).Do(_ => order.Add("middle equip"));
        replacement.EquipmentScript.When(script => script.OnUnequipped(replacement, scenario.Owner)).Do(_ => order.Add("middle unequip"));
        finalItem.EquipmentScript.When(script => script.OnEquipped(finalItem, scenario.Owner)).Do(_ => order.Add("final equip"));
        events.When(manager => manager.SendEvent(Arg.Any<IEvent>())).Do(_ => order.Add("publish"));
        var boundary = (ItemContainerMutationBoundary)typeof(EquipmentContainer)
            .GetField("_mutations", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(equipment)!;
        using var transaction = ItemContainerTransaction.Begin(boundary, boundary);
        Assert.IsTrue(equipment.TryReplaceEquippedItem(EquipmentSlot.Hat, current, replacement));
        Assert.IsTrue(equipment.TryReplaceEquippedItem(EquipmentSlot.Hat, replacement, finalItem));
        Assert.AreEqual(0, order.Count);
        transaction.Commit();
        transaction.Dispose();
        transaction.Dispose();
        CollectionAssert.AreEqual(new[] { "old unequip", "middle equip", "middle unequip", "final equip", "publish" }, order);
        Assert.AreSame(finalItem, equipment[EquipmentSlot.Hat]);
        using var fresh = ItemContainerTransaction.Begin(boundary);
        fresh.Commit();
        Assert.AreEqual(5, order.Count);
    }

    [TestMethod]
    public void EquipmentCompletion_FactsAndExecutionContractsContainNoDelegates()
    {
        static bool ContainsDelegate(Type type) => typeof(Delegate).IsAssignableFrom(type) ||
            type.HasElementType && ContainsDelegate(type.GetElementType()!) ||
            type.IsGenericType && type.GetGenericArguments().Any(ContainsDelegate);
        var equipmentType = typeof(EquipmentContainer);
        foreach (var type in equipmentType.GetNestedTypes(BindingFlags.NonPublic).Append(equipmentType))
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                Assert.IsFalse(ContainsDelegate(field.FieldType), $"Delegate in equipment facts: {type.Name}.{field.Name}");
        foreach (var method in equipmentType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
        {
            Assert.IsFalse(ContainsDelegate(method.ReturnType), $"Delegate conversion: {method.Name}");
            Assert.IsFalse(method.GetParameters().Any(parameter => ContainsDelegate(parameter.ParameterType)),
                $"Delegate execution contract: {method.Name}");
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void EquipmentCompletion_AttemptsEffectsAndPublicationRetainsFlatFailuresAndNeverRetries(bool transactional)
    {
        using var scenario = new Scenario();
        var (equipment, events, current, replacement) = CreateReplacementScenario(scenario);
        var firstFailure = new InvalidOperationException("first unequip failure");
        var secondFailure = new InvalidOperationException("second unequip failure");
        var unequipFailure = new AggregateException(firstFailure, secondFailure);
        var equipFailure = new InvalidOperationException("equip failure");
        var publicationFailure = new InvalidOperationException("publication failure");
        var order = ObserveFailingReplacementEffects(scenario.Owner, current, replacement, unequipFailure, equipFailure);
        events.When(manager => manager.SendEvent(Arg.Any<IEvent>())).Do(_ =>
        {
            order.Add("publication");
            throw publicationFailure;
        });
        var boundary = (ItemContainerMutationBoundary)typeof(EquipmentContainer)
            .GetField("_mutations", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(equipment)!;
        using var transaction = transactional ? ItemContainerTransaction.Begin(boundary) : null;
        var thrown = Assert.ThrowsExactly<AggregateException>(() =>
        {
            Assert.IsTrue(equipment.TryReplaceEquippedItem(EquipmentSlot.Hat, current, replacement));
            transaction?.Commit();
        });
        CollectionAssert.AreEqual(new[] { "unequip", "equip", "publication" }, order);
        CollectionAssert.AreEqual(new Exception[] { firstFailure, secondFailure, equipFailure, publicationFailure },
            thrown.InnerExceptions.ToArray());
        Assert.IsNotNull(unequipFailure.StackTrace);
        Assert.IsNotNull(equipFailure.StackTrace);
        Assert.IsNotNull(publicationFailure.StackTrace);
        Assert.AreSame(replacement, equipment[EquipmentSlot.Hat]);
        transaction?.Dispose();
        transaction?.Dispose();
        if (transaction != null) Assert.ThrowsExactly<InvalidOperationException>(() => transaction.Commit());
        using var fresh = ItemContainerTransaction.Begin(boundary);
        fresh.Commit();
        CollectionAssert.AreEqual(new[] { "unequip", "equip", "publication" }, order);
    }

    private static List<string> ObserveFailingReplacementEffects(ICharacter owner, IItem current, IItem replacement,
        Exception unequipFailure, Exception equipFailure)
    {
        var order = new List<string>();
        current.EquipmentScript.When(script => script.OnUnequipped(current, owner)).Do(_ =>
        {
            order.Add("unequip");
            throw unequipFailure;
        });
        replacement.EquipmentScript.When(script => script.OnEquipped(replacement, owner)).Do(_ =>
        {
            order.Add("equip");
            throw equipFailure;
        });
        return order;
    }

    private static InventoryContainer CreateInventory(Scenario scenario, int capacity) =>
        new(scenario.Owner, capacity, Substitute.For<IMapRegionService>(),
            Substitute.For<IGroundItemBuilder>(), scenario.Builder);

    private static List<string> ObserveUnequipBeforePublication(
        IEventManager eventManager,
        EquipmentContainer equipment,
        IItem item,
        ICharacter owner)
    {
        var order = new List<string>();
        item.EquipmentScript.When(script => script.OnUnequipped(item, owner)).Do(_ =>
        {
            Assert.IsNull(equipment[EquipmentSlot.Hat]);
            order.Add("unequip");
        });
        eventManager.When(manager => manager.SendEvent(Arg.Any<IEvent>())).Do(call =>
        {
            if (call.Arg<IEvent>() is EquipmentChangedEvent)
            {
                Assert.IsNull(equipment[EquipmentSlot.Hat]);
                CollectionAssert.AreEqual(new[] { "unequip" }, order);
                order.Add("publish");
            }
        });
        return order;
    }

    private static void TrackPublicationAfterCallback(
        IEventManager eventManager,
        Func<IEvent, bool> isRelevantPublication,
        Func<bool> callbackCompleted,
        Action<bool> observePublication)
    {
        eventManager.When(manager => manager.SendEvent(Arg.Any<IEvent>())).Do(call =>
        {
            if (isRelevantPublication(call.Arg<IEvent>()))
            {
                observePublication(callbackCompleted());
            }
        });
    }

    private static (EquipmentContainer Equipment, IEventManager EventManager) CreateEquipmentScenario(Scenario scenario)
    {
        var eventManager = Substitute.For<IEventManager>();
        scenario.Owner.EventManager.Returns(eventManager);
        return (new EquipmentContainer(scenario.Owner, 15, scenario.Builder), eventManager);
    }

    private static (EquipmentContainer Equipment, IEventManager EventManager, IItem Current, IItem Replacement)
        CreateReplacementScenario(Scenario scenario)
    {
        var (equipment, eventManager) = CreateEquipmentScenario(scenario);
        var current = scenario.Builder.Create().WithId(101).WithCount(1).Build();
        var replacement = scenario.Builder.Create().WithId(102).WithCount(1).Build();
        Assert.IsTrue(equipment.TryRestoreEquippedItem(EquipmentSlot.Hat, current));
        eventManager.ClearReceivedCalls();
        return (equipment, eventManager, current, replacement);
    }

    private static (InventoryContainer Inventory, ShopStockContainer Stock, IMoneyPouchContainer MoneyPouch)
        CreateShopScenario(Scenario scenario)
    {
        var inventory = CreateInventory(scenario, 4);
        scenario.Owner.Inventory.Returns(inventory);
        scenario.Owner.EventManager.Returns(Substitute.For<IEventManager>());
        var moneyPouch = new MoneyPouchContainer(scenario.Owner, scenario.Builder);
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
        Assert.IsTrue(inventory.Items.Add(item));
        item.EquipmentScript.CanEquipItem(item, scenario.Owner).Returns(true);
        return (inventory, equipment, item);
    }

    private static (InventoryContainer Inventory, EquipmentContainer Equipment, IItem Weapon, IItem Shield, IItem Incoming)
        CreateWeaponShieldReplacementSetup(Scenario scenario, int inventoryCapacity)
    {
        var inventory = CreateInventory(scenario, inventoryCapacity);
        scenario.Owner.Inventory.Returns(inventory);
        var eventManager = Substitute.For<IEventManager>();
        scenario.Owner.EventManager.Returns(eventManager);
        scenario.Owner.Mediator.Returns(Substitute.For<IGameMediator>());
        var profile = Substitute.For<IProfile>();
        profile.GetValue<int>(Arg.Any<string>()).Returns(0);
        scenario.Owner.Profile.Returns(profile);
        var equipment = new EquipmentContainer(scenario.Owner, 15, scenario.Builder);
        scenario.Owner.Equipment.Returns(equipment);

        var weaponDefinition = Substitute.For<IEquipmentDefinition>();
        weaponDefinition.Slot.Returns(EquipmentSlot.Weapon);
        weaponDefinition.AttackStyleIDs.Returns(Array.Empty<AttackStyle>());
        scenario.DefineEquipment(101, weaponDefinition);
        var shieldDefinition = Substitute.For<IEquipmentDefinition>();
        shieldDefinition.Slot.Returns(EquipmentSlot.Shield);
        scenario.DefineEquipment(102, shieldDefinition);
        var incomingDefinition = Substitute.For<IEquipmentDefinition>();
        incomingDefinition.Slot.Returns(EquipmentSlot.Weapon);
        incomingDefinition.Type.Returns(EquipmentType.TwoHanded);
        incomingDefinition.AttackStyleIDs.Returns(Array.Empty<AttackStyle>());
        scenario.DefineEquipment(103, incomingDefinition);

        var weapon = scenario.Builder.Create().WithId(101).WithCount(1).Build();
        var shield = scenario.Builder.Create().WithId(102).WithCount(1).Build();
        var incoming = scenario.Builder.Create().WithId(103).WithCount(1).Build();
        Assert.IsTrue(equipment.TryRestoreEquippedItem(EquipmentSlot.Weapon, weapon));
        Assert.IsTrue(equipment.TryRestoreEquippedItem(EquipmentSlot.Shield, shield));
        Assert.IsTrue(inventory.Items.Add(incoming));
        incoming.EquipmentScript.CanEquipItem(incoming, scenario.Owner).Returns(true);
        eventManager.ClearReceivedCalls();
        return (inventory, equipment, weapon, shield, incoming);
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
