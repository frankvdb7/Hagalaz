using System.Threading;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Features.Shops;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Services.GameWorld.Builders;
using Hagalaz.Services.GameWorld.Logic.Characters.Model;
using Hagalaz.Services.GameWorld.Logic.Shops;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class CharacterItemTransferTests
{
    [TestMethod]
    public void EquipmentItems_ReadOnlyViewReflectsEquipmentStorage()
    {
        using var scenario = new Scenario();
        var (equipment, _) = CreateEquipmentScenario(scenario);
        IEquipmentContainer equipmentContract = equipment;
        IReadOnlyItemContainer items = equipmentContract.Items;
        var item = scenario.Builder.Create().WithId(100).WithCount(3).Build();
        Assert.IsTrue(equipment.TryRestoreEquippedItem(EquipmentSlot.Hat, item));

        Assert.AreEqual(15, items.Capacity);
        Assert.AreEqual(14, items.FreeSlots);
        Assert.AreEqual(1, items.TakenSlots);
        Assert.AreSame(item, items[(int)EquipmentSlot.Hat]);
        Assert.AreSame(item, items.GetById(item.Id));
        Assert.AreEqual(3, items.GetCount(item));
        Assert.AreEqual(3, items.GetCountById(item.Id));
        Assert.AreEqual((int)EquipmentSlot.Hat, items.GetInstanceSlot(item));
        Assert.AreEqual((int)EquipmentSlot.Hat, items.GetSlotByItem(item));
        Assert.IsTrue(items.Contains(item.Id));
        Assert.IsTrue(items.Contains(item.Id, 3));

        var enumeratedItems = 0;
        foreach (var equippedItem in items)
        {
            if (equippedItem != null) enumeratedItems++;
        }

        Assert.AreEqual(1, enumeratedItems);
        Assert.IsFalse(items is IItemContainer);
    }

    [TestMethod]
    public void InventoryAdd_PublishesAfterUnlockWithoutRollbackTransaction()
    {
        using var scenario = new Scenario();
        var inventory = CreateInventory(scenario, 2);
        var eventManager = Substitute.For<IEventManager>();
        scenario.Owner.EventManager.Returns(eventManager);
        var boundary = Synchronization(inventory);
        var item = scenario.Builder.Create().WithId(99).WithCount(1).Build();

        eventManager.When(manager => manager.SendEvent(Arg.Any<IEvent>())).Do(call =>
        {
            if (call.Arg<IEvent>() is not InventoryChangedEvent) return;

            Assert.AreSame(item, inventory.Items[0]);
            AssertStandalonePublicationOwnsBoundaryAfterUnlock(boundary);
        });

        Assert.IsTrue(inventory.AddItem(item));

        eventManager.Received(1).SendEvent(Arg.Is<IEvent>(gameEvent => gameEvent is InventoryChangedEvent));
    }

    [TestMethod]
    public void BankAdd_PublishesAfterUnlockWithoutRollbackTransaction()
    {
        using var scenario = new Scenario();
        var bank = new BankContainer(scenario.Owner, 2, scenario.Builder);
        var eventManager = Substitute.For<IEventManager>();
        scenario.Owner.EventManager.Returns(eventManager);
        var boundary = Synchronization(bank);
        AssertBankPublicationOwnsBoundaryAfterUnlock(eventManager, boundary);

        Assert.IsTrue(bank.AddItem(scenario.Builder.Create().WithId(98).WithCount(1).Build()));

        eventManager.Received(1).SendEvent(Arg.Is<IEvent>(gameEvent => gameEvent is BankChangedEvent));
    }

    [TestMethod]
    public void BankSort_PublishesAfterUnlockWithoutRollbackTransaction()
    {
        using var scenario = new Scenario();
        var bank = new BankContainer(scenario.Owner, 3, scenario.Builder);
        var eventManager = Substitute.For<IEventManager>();
        scenario.Owner.EventManager.Returns(eventManager);
        var boundary = Synchronization(bank);
        AssertBankPublicationOwnsBoundaryAfterUnlock(eventManager, boundary);
        var first = scenario.Builder.Create().WithId(98).WithCount(1).Build();
        var second = scenario.Builder.Create().WithId(99).WithCount(1).Build();
        Assert.IsTrue(bank.AddItem(first));
        Assert.IsTrue(bank.AddItem(second));
        bank.SwapItems(0, 2);
        eventManager.ClearReceivedCalls();

        bank.SortItems();

        Assert.AreSame(second, bank.Items[0]);
        Assert.AreSame(first, bank.Items[1]);
        eventManager.Received(1).SendEvent(Arg.Is<IEvent>(gameEvent => gameEvent is BankChangedEvent));
    }

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
    public void EquipmentSimpleMutations_ParticipateInEnlistedScopeAndDeferCompletion()
    {
        using var scenario = new Scenario();
        var (equipment, eventManager) = CreateEquipmentScenario(scenario);
        var current = scenario.Builder.Create().WithId(101).WithCount(1).Build();
        var restored = scenario.Builder.Create().WithId(102).WithCount(1).Build();
        var replacement = scenario.Builder.Create().WithId(103).WithCount(1).Build();
        Assert.IsTrue(equipment.TryRestoreEquippedItem(EquipmentSlot.Hat, current));
        eventManager.ClearReceivedCalls();
        var boundary = Synchronization(equipment);
        IEquipmentContainer participant = equipment;
        using var transaction = ItemContainerTransaction.Begin(participant);

        Assert.IsTrue(equipment.TryRestoreEquippedItem(EquipmentSlot.Amulet, restored));
        Assert.IsTrue(equipment.TryReplaceEquippedItem(EquipmentSlot.Hat, current, replacement));
        Assert.AreEqual(1, equipment.RemoveEquippedItem(replacement, EquipmentSlot.Hat));
        equipment.ClearEquipment();

        Assert.IsNull(equipment[EquipmentSlot.Hat]);
        Assert.IsNull(equipment[EquipmentSlot.Amulet]);
        Assert.IsTrue(Monitor.IsEntered(boundary.SynchronizationLock));
        current.EquipmentScript.DidNotReceive().OnUnequipped(current, scenario.Owner);
        restored.EquipmentScript.DidNotReceive().OnEquipped(restored, scenario.Owner);
        replacement.EquipmentScript.DidNotReceive().OnEquipped(replacement, scenario.Owner);
        eventManager.DidNotReceive().SendEvent(Arg.Any<IEvent>());

        transaction.Commit();
        current.EquipmentScript.Received(1).OnUnequipped(current, scenario.Owner);
        replacement.EquipmentScript.Received(1).OnEquipped(replacement, scenario.Owner);
        replacement.EquipmentScript.Received(1).OnUnequipped(replacement, scenario.Owner);
        restored.EquipmentScript.Received(1).OnUnequipped(restored, scenario.Owner);
        eventManager.Received(1).SendEvent(Arg.Is<IEvent>(gameEvent => gameEvent is EquipmentChangedEvent));
    }

    [TestMethod]
    public void TryMoveTo_CallerTransactionPublishesAfterUnequip()
    {
        using var scenario = new Scenario();
        var order = new List<string>();
        var (equipment, eventManager) = CreateEquipmentScenario(scenario);
        var item = scenario.Builder.Create().WithId(101).WithCount(1).Build();
        Assert.IsTrue(equipment.TryRestoreEquippedItem(EquipmentSlot.Hat, item));
        eventManager.ClearReceivedCalls();
        var destination = new ItemContainer(StorageType.Normal, 2);
        item.EquipmentScript.When(script => script.OnUnequipped(item, scenario.Owner)).Do(_ =>
        {
            AssertCompletionRunsAfterUnlock(equipment, destination);
            Assert.IsNull(equipment[EquipmentSlot.Hat]);
            Assert.AreSame(item, destination[0]);
            order.Add("unequip");
        });
        eventManager.When(manager => manager.SendEvent(Arg.Any<IEvent>())).Do(call =>
        {
            if (call.Arg<IEvent>() is not EquipmentChangedEvent) return;
            AssertCompletionRunsAfterUnlock(equipment, destination);
            Assert.IsNull(equipment[EquipmentSlot.Hat]);
            Assert.AreSame(item, destination[0]);
            CollectionAssert.AreEqual(new[] { "unequip" }, order);
            order.Add("equipment");
        });

        using var transaction = ItemContainerTransaction.Begin(equipment, destination);
        Assert.IsTrue(equipment.TryMoveTo(destination, item, item.Count, EquipmentSlot.Hat));

        item.EquipmentScript.DidNotReceive().OnUnequipped(item, scenario.Owner);
        eventManager.DidNotReceive().SendEvent(Arg.Is<IEvent>(gameEvent => gameEvent is EquipmentChangedEvent));
        transaction.Commit();

        Assert.IsNull(equipment[EquipmentSlot.Hat]);
        Assert.AreSame(item, destination[0]);
        item.EquipmentScript.Received(1).OnUnequipped(item, scenario.Owner);
        eventManager.Received(1).SendEvent(Arg.Is<IEvent>(gameEvent => gameEvent is EquipmentChangedEvent));
        CollectionAssert.AreEqual(new[] { "unequip", "equipment" }, order);
        CollectionAssert.AreEqual(new[] { 0 }, destination.ChangeTracker.GetSlots()!.Order().ToArray());
        AssertMutationLocksReleased(equipment, destination);
    }

    [TestMethod]
    public void TryMoveTo_PartialRemovalWithinCallerTransactionDoesNotRunUnequipped()
    {
        using var scenario = new Scenario();
        var (equipment, eventManager, destination, item) = CreateEquipmentMoveScenario(scenario, count: 3);

        using var transaction = ItemContainerTransaction.Begin(equipment, destination);
        Assert.IsTrue(equipment.TryMoveTo(destination, item, 2, EquipmentSlot.Hat));

        Assert.AreSame(item, equipment[EquipmentSlot.Hat]);
        Assert.AreEqual(1, item.Count);
        Assert.AreEqual(2, destination[0]!.Count);
        item.EquipmentScript.DidNotReceive().OnUnequipped(item, scenario.Owner);
        eventManager.DidNotReceive().SendEvent(Arg.Is<IEvent>(gameEvent => gameEvent is EquipmentChangedEvent));
        transaction.Commit();
        eventManager.Received(1).SendEvent(Arg.Is<IEvent>(gameEvent => gameEvent is EquipmentChangedEvent));
        AssertMutationLocksReleased(equipment, destination);
    }

    [TestMethod]
    public void TryMoveTo_WithoutCallerTransactionRejectsBeforeMutation()
    {
        using var scenario = new Scenario();
        var (equipment, eventManager, destination, item) = CreateEquipmentMoveScenario(scenario);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            equipment.TryMoveTo(destination, item, item.Count, EquipmentSlot.Hat));

        AssertMoveRejectedWithoutMutation(equipment, eventManager, destination, item, scenario.Owner);
        AssertMutationLocksReleased(equipment, destination);
    }

    [TestMethod]
    public void TryMoveTo_ValidatesExpectedItemAndCountWhileBothLocksAreHeld()
    {
        using var scenario = new Scenario();
        var (equipment, _) = CreateEquipmentScenario(scenario);
        var destination = new ItemContainer(StorageType.Normal, 2);
        var item = new TransferProbeItem(201, 3);
        Assert.IsTrue(equipment.TryRestoreEquippedItem(EquipmentSlot.Hat, item));
        var equipmentLock = Synchronization(equipment).SynchronizationLock;
        var destinationLock = Synchronization(destination).SynchronizationLock;
        var sawLockedCountRead = false;
        item.OnCountRead = () =>
        {
            Assert.IsTrue(Monitor.IsEntered(equipmentLock));
            Assert.IsTrue(Monitor.IsEntered(destinationLock));
            sawLockedCountRead = true;
        };

        using var transaction = ItemContainerTransaction.Begin(equipment, destination);
        Assert.IsTrue(equipment.TryMoveTo(destination, item, 3, EquipmentSlot.Hat));

        Assert.IsTrue(sawLockedCountRead, "Equipment's expected-instance/count validation must run under both mutation locks.");
        Assert.IsNull(equipment[EquipmentSlot.Hat]);
        Assert.AreSame(item, destination[0]);
        transaction.Commit();
        AssertMutationLocksReleased(equipment, destination);
    }

    [TestMethod]
    public void TryMoveTo_LifecycleFailurePublishesEquipmentAndRetainsOwnerlessDestinationChanges()
    {
        using var scenario = new Scenario();
        var (equipment, eventManager, destination, item) = CreateEquipmentMoveScenario(scenario);
        var hookFailure = new InvalidOperationException("Unequip hook failed.");
        item.EquipmentScript.When(script => script.OnUnequipped(item, scenario.Owner)).Do(_ => throw hookFailure);

        using var transaction = ItemContainerTransaction.Begin(equipment, destination);
        Assert.IsTrue(equipment.TryMoveTo(destination, item, 1, EquipmentSlot.Hat));
        var postCommitException = Assert.ThrowsExactly<ItemContainerPostCommitPublicationException>(transaction.Commit);
        Assert.AreSame(hookFailure, postCommitException.InnerException);

        Assert.IsNull(equipment[EquipmentSlot.Hat]);
        Assert.AreSame(item, destination[0]);
        eventManager.Received(1).SendEvent(Arg.Is<IEvent>(gameEvent => gameEvent is EquipmentChangedEvent));
        CollectionAssert.AreEqual(new[] { 0 }, destination.ChangeTracker.GetSlots()!.Order().ToArray());
        AssertMutationLocksReleased(equipment, destination);
    }

    [TestMethod]
    public void TryMoveTo_ParticipatesInCallerTransactionUntilCommit()
    {
        using var scenario = new Scenario();
        var (equipment, eventManager, destination, item) = CreateEquipmentMoveScenario(scenario);
        IEquipmentContainer participant = equipment;
        using var transaction = ItemContainerTransaction.Begin(participant, destination);

        Assert.IsTrue(participant.TryMoveTo(destination, item, item.Count, EquipmentSlot.Hat));

        Assert.IsNull(equipment[EquipmentSlot.Hat]);
        Assert.AreSame(item, destination[0]);
        item.EquipmentScript.DidNotReceive().OnUnequipped(item, scenario.Owner);
        eventManager.DidNotReceive().SendEvent(Arg.Any<IEvent>());

        transaction.Commit();

        item.EquipmentScript.Received(1).OnUnequipped(item, scenario.Owner);
        eventManager.Received(1).SendEvent(Arg.Is<IEvent>(gameEvent => gameEvent is EquipmentChangedEvent));
        CollectionAssert.AreEqual(new[] { 0 }, destination.ChangeTracker.GetSlots()!.Order().ToArray());
    }

    [TestMethod]
    public void TryMoveTo_RollsBackWithCallerTransaction()
    {
        using var scenario = new Scenario();
        var (equipment, eventManager, destination, item) =
            CreateEquipmentMoveScenario(scenario);
        IEquipmentContainer participant = equipment;

        using (ItemContainerTransaction.Begin(participant, destination))
        {
            Assert.IsTrue(participant.TryMoveTo(destination, item, item.Count, EquipmentSlot.Hat));
            Assert.IsNull(equipment[EquipmentSlot.Hat]);
            Assert.AreSame(item, destination[0]);
        }

        AssertMoveRejectedWithoutMutation(equipment, eventManager, destination, item, scenario.Owner);
        CollectionAssert.AreEqual(Array.Empty<int>(), destination.ChangeTracker.GetSlots()!.Order().ToArray());
    }

    [TestMethod]
    public void TryMoveTo_RejectsPartialEnlistmentBeforeMutation()
    {
        using var scenario = new Scenario();
        var (equipment, eventManager, destination, item) = CreateEquipmentMoveScenario(scenario);
        IEquipmentContainer participant = equipment;
        using var transaction = ItemContainerTransaction.Begin(participant);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            participant.TryMoveTo(destination, item, item.Count, EquipmentSlot.Hat));

        AssertMoveRejectedWithoutMutation(equipment, eventManager, destination, item, scenario.Owner);
    }

    [TestMethod]
    public void TryMoveTo_RejectsSeparateActiveTransactionsBeforeMutation()
    {
        using var scenario = new Scenario();
        var (equipment, eventManager, destination, item) = CreateEquipmentMoveScenario(scenario);
        IEquipmentContainer participant = equipment;
        using var equipmentTransaction = ItemContainerTransaction.Begin(participant);
        using var destinationTransaction = ItemContainerTransaction.Begin(destination);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            participant.TryMoveTo(destination, item, item.Count, EquipmentSlot.Hat));

        Assert.AreSame(item, equipment[EquipmentSlot.Hat]);
        Assert.IsNull(destination[0]);
        item.EquipmentScript.DidNotReceive().OnUnequipped(item, scenario.Owner);
        eventManager.DidNotReceive().SendEvent(Arg.Any<IEvent>());
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
    public void TryReplaceEquippedItem_WhenOldCallbackThrows_AttemptsRemainingEffectsAndPublishes()
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
    public void TryReplaceEquippedItem_WhenBothCallbacksThrow_AggregatesFailuresAndPublishes()
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

        CollectionAssert.AreEquivalent(new Exception[] { unequipFailure, equipFailure }, thrown.InnerExceptions.ToArray());
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
        inventory.AddItem(item);

        Assert.IsTrue(bank.DepositFromInventory(item, 3, out var deposited));

        Assert.AreEqual(3, deposited!.Count);
        Assert.AreEqual(2, inventory.Items[0]!.Count);
        Assert.AreEqual(3, bank.Items.GetCountById(101));
        CollectionAssert.AreEqual(new long[] { 11, 22 }, bank.Items[0]!.ExtraData);
    }

    [TestMethod]
    public void BankDepositFromMoneyPouch_WhenBankCannotAcceptCoins_LeavesBothStoresUnchanged()
    {
        using var scenario = new Scenario();
        var inventory = CreateInventory(scenario, 4);
        scenario.Owner.Inventory.Returns(inventory);
        var moneyPouch = new MoneyPouchContainer(scenario.Owner, scenario.Builder);
        scenario.Owner.MoneyPouch.Returns(moneyPouch);
        Assert.IsTrue(moneyPouch.Add(5));
        var bank = new BankContainer(scenario.Owner, 2, scenario.Builder);
        Assert.IsTrue(bank.AddItem(scenario.Builder.Create().WithId(995).WithCount(int.MaxValue).Build()));
        var events = Substitute.For<IEventManager>();
        scenario.Owner.EventManager.Returns(events);
        scenario.Owner.ClearReceivedCalls();
        var bankBoundary = Synchronization(bank);
        var pouchStorages = ((IItemTransactionSource)moneyPouch).Synchronizations;
        scenario.Owner.When(owner => owner.SendChatMessage(Arg.Any<string>())).Do(_ =>
        {
            Assert.IsFalse(Monitor.IsEntered(bankBoundary.SynchronizationLock));
            foreach (var boundary in pouchStorages)
            {
                Assert.IsFalse(Monitor.IsEntered(boundary.SynchronizationLock));
            }
        });
        Assert.IsFalse(bank.DepositFromMoneyPouch(out var deposited));

        Assert.IsNull(deposited);
        scenario.Owner.Received(1).SendChatMessage("Not enough space in your bank.");
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
        inventory.AddItem(note);

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
        bank.AddItem(item);

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
        inventory.AddItem(scenario.Builder.Create().WithId(102).WithCount(1).Build());
        var item = scenario.Builder.Create().WithId(101).WithCount(5).Build();
        bank.AddItem(item);

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
    public void ClearEquipment_WhenCallbackThrows_AttemptsEveryCallbackAndPublishes()
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
        Assert.IsTrue(equipment.Items.All(item => item == null));
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
        inventory.AddItem(item);

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
        inventory.AddItem(item);

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
        inventory.AddItem(item);

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
        familiar.Hydrate([new Hagalaz.Game.Abstractions.Logic.Characters.Model.HydratedItem(101, 1, 0, null),
            new Hagalaz.Game.Abstractions.Logic.Characters.Model.HydratedItem(102, 1, 1, null)]);
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
        Assert.IsTrue(inventory.AddItem(scenario.Builder.Create().WithId(199).WithCount(1).Build()));
        var familiar = new FamiliarInventoryContainer(scenario.Owner, StorageType.Normal, 3, scenario.Builder);
        familiar.Hydrate([new Hagalaz.Game.Abstractions.Logic.Characters.Model.HydratedItem(101, 1, 0, null),
            new Hagalaz.Game.Abstractions.Logic.Characters.Model.HydratedItem(102, 1, 1, null),
            new Hagalaz.Game.Abstractions.Logic.Characters.Model.HydratedItem(103, 1, 2, null)]);
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
        Assert.IsTrue(inventory.AddItem(scenario.Builder.Create().WithId(199).WithCount(1).Build()));
        var familiar = new FamiliarInventoryContainer(scenario.Owner, StorageType.Normal, 1, scenario.Builder);
        familiar.Hydrate([new Hagalaz.Game.Abstractions.Logic.Characters.Model.HydratedItem(101, 1, 0, null)]);
        var familiarItem = familiar.Items[0]!;
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
    public void FamiliarInventory_WithdrawalsAreRejectedWhileGameplayScriptIsBusy()
    {
        using var scenario = new Scenario();
        scenario.DefineItem(101, stackable: false);
        var inventory = CreateInventory(scenario, 1);
        scenario.Owner.Inventory.Returns(inventory);
        var busyScript = Substitute.For<ICharacterScript>();
        busyScript.IsBusy().Returns(true);
        scenario.Owner.GetScripts().Returns([busyScript]);
        var familiar = new FamiliarInventoryContainer(scenario.Owner, StorageType.Normal, 1, scenario.Builder);
        familiar.Hydrate([new Hagalaz.Game.Abstractions.Logic.Characters.Model.HydratedItem(101, 1, 0, null)]);
        var familiarItem = familiar.Items[0]!;
        var eventManager = Substitute.For<IEventManager>();
        scenario.Owner.EventManager.Returns(eventManager);

        Assert.IsFalse(familiar.WithdrawFromFamiliarInventory(familiarItem, 1));
        familiar.WithdrawAvailableToInventory();

        Assert.AreSame(familiarItem, familiar.Items[0]);
        Assert.IsNull(inventory.Items[0]);
        scenario.Owner.Received(2).SendChatMessage(
            "Please finish what you're doing before withdrawing items from your familiar.");
        eventManager.DidNotReceive().SendEvent(Arg.Any<IEvent>());
    }

    [TestMethod]
    public void FamiliarInventory_WithdrawalsAreAllowedWhenInventoryOverlayIsOpen()
    {
        using var scenario = new Scenario();
        scenario.DefineItem(101, stackable: false);
        scenario.DefineItem(102, stackable: false);
        var inventory = CreateInventory(scenario, 2);
        scenario.Owner.Inventory.Returns(inventory);
        // Character.IsBusy() also returns true for the inventory overlay used by the familiar screen.
        scenario.Owner.IsBusy().Returns(true);
        scenario.Owner.GetScripts().Returns(Array.Empty<ICharacterScript>());
        scenario.Owner.Combat.IsDead.Returns(false);
        scenario.Owner.Combat.IsInCombat().Returns(false);
        var familiar = new FamiliarInventoryContainer(scenario.Owner, StorageType.Normal, 2, scenario.Builder);
        familiar.Hydrate([
            new Hagalaz.Game.Abstractions.Logic.Characters.Model.HydratedItem(101, 1, 0, null),
            new Hagalaz.Game.Abstractions.Logic.Characters.Model.HydratedItem(102, 1, 1, null)
        ]);

        Assert.IsTrue(familiar.WithdrawFromFamiliarInventory(familiar.Items[0]!, 1));
        familiar.WithdrawAvailableToInventory();

        Assert.AreEqual(0, familiar.Items.TakenSlots);
        Assert.AreEqual(1, inventory.Items.GetCountById(101));
        Assert.AreEqual(1, inventory.Items.GetCountById(102));
        scenario.Owner.DidNotReceive().SendChatMessage(
            "Please finish what you're doing before withdrawing items from your familiar.");
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
        inventory.AddItem(scenario.Builder.Create().WithId(102).WithCount(1).Build());
        var rewardItem = scenario.Builder.Create().WithId(101).WithCount(4).Build();
        rewards.Hydrate([new HydratedItemDto(101, 4, 0, null)]);
        rewardItem = rewards.Items[0]!;

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
    public void EquipItem_TwoHandedWeaponRejectsIfShieldEquipsWhileWaitingForTransaction()
    {
        using var scenario = new Scenario();
        var setup = CreateEmptyWeaponShieldSetup(scenario);

        var equipResult = RunEquipAfterConcurrentEquipmentMutation(
            setup.Inventory, setup.Equipment, scenario.Owner, setup.Weapon, EquipmentSlot.Shield, setup.Shield);

        Assert.IsFalse(equipResult);
        Assert.AreSame(setup.Weapon, setup.Inventory.Items[0]);
        Assert.AreEqual(1, setup.Inventory.Items.GetCountById(setup.Weapon.Id));
        Assert.IsNull(setup.Equipment[EquipmentSlot.Weapon]);
        Assert.AreSame(setup.Shield, setup.Equipment[EquipmentSlot.Shield]);
        setup.Weapon.EquipmentScript.Received(1).CanEquipItem(setup.Weapon, scenario.Owner);
        setup.Weapon.EquipmentScript.DidNotReceive().OnEquipped(setup.Weapon, scenario.Owner);
        setup.Shield.EquipmentScript.DidNotReceive().OnUnequipped(setup.Shield, scenario.Owner);
        setup.EventManager.Received(1).SendEvent(Arg.Is<IEvent>(gameEvent => gameEvent is InventoryChangedEvent));
        setup.EventManager.Received(1).SendEvent(Arg.Is<IEvent>(gameEvent => gameEvent is EquipmentChangedEvent));
        scenario.Owner.DidNotReceive().SendChatMessage(Arg.Any<string>());
        AssertMutationLocksReleased(setup.Equipment, setup.Inventory);

        scenario.DefaultEquipmentDefinition.Slot.Returns(EquipmentSlot.Hat);
        var validItem = scenario.Builder.Create().WithId(103).WithCount(1).Build();
        Assert.IsTrue(setup.Inventory.AddItem(validItem));
        validItem.EquipmentScript.CanEquipItem(validItem, scenario.Owner).Returns(true);
        setup.EventManager.ClearReceivedCalls();

        Assert.IsTrue(setup.Equipment.EquipItem(validItem));

        Assert.AreSame(validItem, setup.Equipment[EquipmentSlot.Hat]);
        validItem.EquipmentScript.Received(1).OnEquipped(validItem, scenario.Owner);
    }

    [TestMethod]
    public void EquipItem_ShieldRejectsIfTwoHandedWeaponEquipsWhileWaitingForTransaction()
    {
        using var scenario = new Scenario();
        var setup = CreateEmptyWeaponShieldSetup(scenario);

        var equipResult = RunEquipAfterConcurrentEquipmentMutation(
            setup.Inventory, setup.Equipment, scenario.Owner, setup.Shield, EquipmentSlot.Weapon, setup.Weapon);

        Assert.IsFalse(equipResult);
        Assert.AreSame(setup.Shield, setup.Inventory.Items[1]);
        Assert.AreEqual(1, setup.Inventory.Items.GetCountById(setup.Shield.Id));
        Assert.AreSame(setup.Weapon, setup.Equipment[EquipmentSlot.Weapon]);
        Assert.IsNull(setup.Equipment[EquipmentSlot.Shield]);
        setup.Shield.EquipmentScript.Received(1).CanEquipItem(setup.Shield, scenario.Owner);
        setup.Shield.EquipmentScript.DidNotReceive().OnEquipped(setup.Shield, scenario.Owner);
        setup.Weapon.EquipmentScript.DidNotReceive().OnUnequipped(setup.Weapon, scenario.Owner);
        setup.EventManager.Received(1).SendEvent(Arg.Is<IEvent>(gameEvent => gameEvent is InventoryChangedEvent));
        setup.EventManager.Received(1).SendEvent(Arg.Is<IEvent>(gameEvent => gameEvent is EquipmentChangedEvent));
        scenario.Owner.DidNotReceive().SendChatMessage(Arg.Any<string>());
        AssertMutationLocksReleased(setup.Equipment, setup.Inventory);
    }

    [TestMethod]
    public void EquipItem_EmptyWeaponAndShieldSlotsStillEquipExactItems()
    {
        AssertEmptyEquipmentSlotEquipSucceeds(EquipmentSlot.Weapon, 101);
        AssertEmptyEquipmentSlotEquipSucceeds(EquipmentSlot.Shield, 102);
    }

    [TestMethod]
    public void EquipItem_StalePreflightDoesNotConsumeEquivalentReplacement()
    {
        using var scenario = new Scenario();
        var (inventory, equipment, item) = CreateEquipmentSetup(scenario);
        var equivalent = scenario.Builder.Create().WithId(item.Id).WithCount(item.Count).Build();
        item.EquipmentScript.CanEquipItem(item, scenario.Owner).Returns(_ =>
        {
            Assert.IsTrue(inventory.TryRemoveItemExact(item));
            Assert.IsTrue(inventory.AddItem(equivalent));
            return true;
        });

        Assert.IsFalse(equipment.EquipItem(item));

        Assert.AreSame(equivalent, inventory.Items[0]);
        Assert.IsNull(equipment[EquipmentSlot.Hat]);
        item.EquipmentScript.DidNotReceive().OnEquipped(item, scenario.Owner);
        equivalent.EquipmentScript.DidNotReceive().OnEquipped(equivalent, scenario.Owner);
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
        Assert.IsTrue(inventory.AddItem(incoming));
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
    public void EquipItem_TwoConflictsThatCannotBothFitRollsBackEveryStorageChange()
    {
        using var scenario = new Scenario();
        var setup = CreateWeaponShieldReplacementSetup(scenario, inventoryCapacity: 1);
        setup.Weapon.EquipmentScript.CanUnEquipItem(setup.Weapon, scenario.Owner).Returns(true);
        setup.Shield.EquipmentScript.CanUnEquipItem(setup.Shield, scenario.Owner).Returns(true);
        scenario.Owner.ClearReceivedCalls();
        Assert.IsFalse(setup.Equipment.EquipItem(setup.Incoming));

        scenario.Owner.Received(1).SendChatMessage("Not enough space in your inventory.");
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
    public void EquipItem_TwoHandedReplacementRejectsStaleIncomingInstance()
    {
        using var scenario = new Scenario();
        var setup = CreateWeaponShieldReplacementSetup(scenario, inventoryCapacity: 3);
        var replacement = scenario.Builder.Create().WithId(setup.Incoming.Id).WithCount(setup.Incoming.Count).Build();
        var eventManager = scenario.Owner.EventManager;
        setup.Weapon.EquipmentScript.CanUnEquipItem(setup.Weapon, scenario.Owner).Returns(_ =>
        {
            Assert.IsTrue(setup.Inventory.TryRemoveItemExact(setup.Incoming, 0));
            Assert.IsTrue(setup.Inventory.AddItem(replacement));
            eventManager.ClearReceivedCalls();
            scenario.Owner.ClearReceivedCalls();
            setup.Weapon.EquipmentScript.ClearReceivedCalls();
            setup.Shield.EquipmentScript.ClearReceivedCalls();
            setup.Incoming.EquipmentScript.ClearReceivedCalls();
            replacement.EquipmentScript.ClearReceivedCalls();
            return true;
        });
        setup.Shield.EquipmentScript.CanUnEquipItem(setup.Shield, scenario.Owner).Returns(true);

        Assert.IsFalse(setup.Equipment.EquipItem(setup.Incoming));

        Assert.AreSame(replacement, setup.Inventory.Items[0]);
        Assert.AreSame(setup.Weapon, setup.Equipment[EquipmentSlot.Weapon]);
        Assert.AreSame(setup.Shield, setup.Equipment[EquipmentSlot.Shield]);
        setup.Incoming.EquipmentScript.DidNotReceive().OnEquipped(setup.Incoming, scenario.Owner);
        setup.Weapon.EquipmentScript.DidNotReceive().OnUnequipped(setup.Weapon, scenario.Owner);
        setup.Shield.EquipmentScript.DidNotReceive().OnUnequipped(setup.Shield, scenario.Owner);
        replacement.EquipmentScript.DidNotReceive().OnEquipped(replacement, scenario.Owner);
        eventManager.DidNotReceive().SendEvent(Arg.Any<IEvent>());
        scenario.Owner.DidNotReceive().SendChatMessage("Not enough space in your inventory.");
    }

    [TestMethod]
    public void EquipItem_TwoHandedReplacementRejectsStaleWeaponInstance()
    {
        using var scenario = new Scenario();
        var setup = CreateWeaponShieldReplacementSetup(scenario, inventoryCapacity: 3);
        var replacementWeapon = scenario.Builder.Create().WithId(setup.Weapon.Id).WithCount(setup.Weapon.Count).Build();
        var eventManager = scenario.Owner.EventManager;
        setup.Weapon.EquipmentScript.CanUnEquipItem(setup.Weapon, scenario.Owner).Returns(_ =>
        {
            Assert.IsTrue(setup.Equipment.TryReplaceEquippedItem(EquipmentSlot.Weapon, setup.Weapon, replacementWeapon));
            eventManager.ClearReceivedCalls();
            scenario.Owner.ClearReceivedCalls();
            setup.Weapon.EquipmentScript.ClearReceivedCalls();
            setup.Shield.EquipmentScript.ClearReceivedCalls();
            setup.Incoming.EquipmentScript.ClearReceivedCalls();
            replacementWeapon.EquipmentScript.ClearReceivedCalls();
            return true;
        });
        setup.Shield.EquipmentScript.CanUnEquipItem(setup.Shield, scenario.Owner).Returns(true);

        Assert.IsFalse(setup.Equipment.EquipItem(setup.Incoming));

        Assert.AreSame(setup.Incoming, setup.Inventory.Items[0]);
        Assert.AreSame(replacementWeapon, setup.Equipment[EquipmentSlot.Weapon]);
        Assert.AreSame(setup.Shield, setup.Equipment[EquipmentSlot.Shield]);
        setup.Weapon.EquipmentScript.DidNotReceive().OnUnequipped(setup.Weapon, scenario.Owner);
        setup.Shield.EquipmentScript.DidNotReceive().OnUnequipped(setup.Shield, scenario.Owner);
        setup.Incoming.EquipmentScript.DidNotReceive().OnEquipped(setup.Incoming, scenario.Owner);
        replacementWeapon.EquipmentScript.DidNotReceive().OnUnequipped(replacementWeapon, scenario.Owner);
        eventManager.DidNotReceive().SendEvent(Arg.Any<IEvent>());
        scenario.Owner.DidNotReceive().SendChatMessage("Not enough space in your inventory.");
    }

    [TestMethod]
    public void EquipItem_TwoHandedReplacementRejectsStaleShieldInstance()
    {
        using var scenario = new Scenario();
        var setup = CreateWeaponShieldReplacementSetup(scenario, inventoryCapacity: 3);
        var replacementShield = scenario.Builder.Create().WithId(setup.Shield.Id).WithCount(setup.Shield.Count).Build();
        var eventManager = scenario.Owner.EventManager;
        setup.Weapon.EquipmentScript.CanUnEquipItem(setup.Weapon, scenario.Owner).Returns(_ =>
        {
            Assert.IsTrue(setup.Equipment.TryReplaceEquippedItem(EquipmentSlot.Shield, setup.Shield, replacementShield));
            eventManager.ClearReceivedCalls();
            scenario.Owner.ClearReceivedCalls();
            setup.Weapon.EquipmentScript.ClearReceivedCalls();
            setup.Shield.EquipmentScript.ClearReceivedCalls();
            setup.Incoming.EquipmentScript.ClearReceivedCalls();
            replacementShield.EquipmentScript.ClearReceivedCalls();
            return true;
        });
        setup.Shield.EquipmentScript.CanUnEquipItem(setup.Shield, scenario.Owner).Returns(true);

        Assert.IsFalse(setup.Equipment.EquipItem(setup.Incoming));

        Assert.AreSame(setup.Incoming, setup.Inventory.Items[0]);
        Assert.AreSame(setup.Weapon, setup.Equipment[EquipmentSlot.Weapon]);
        Assert.AreSame(replacementShield, setup.Equipment[EquipmentSlot.Shield]);
        setup.Weapon.EquipmentScript.DidNotReceive().OnUnequipped(setup.Weapon, scenario.Owner);
        setup.Shield.EquipmentScript.DidNotReceive().OnUnequipped(setup.Shield, scenario.Owner);
        setup.Incoming.EquipmentScript.DidNotReceive().OnEquipped(setup.Incoming, scenario.Owner);
        replacementShield.EquipmentScript.DidNotReceive().OnEquipped(replacementShield, scenario.Owner);
        eventManager.DidNotReceive().SendEvent(Arg.Any<IEvent>());
        scenario.Owner.DidNotReceive().SendChatMessage("Not enough space in your inventory.");
    }

    [TestMethod]
    public void EquipItem_TwoConflictInventoryPublisherFailureStopsEquipmentPublication()
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
        var inventoryPublicationFailure = new InvalidOperationException("Controlled inventory publication failure.");
        eventManager.When(manager => manager.SendEvent(Arg.Any<IEvent>())).Do(call =>
        {
            if (call.Arg<IEvent>() is InventoryChangedEvent)
                throw inventoryPublicationFailure;
        });

        var failure = Assert.ThrowsExactly<ItemContainerPostCommitPublicationException>(() => setup.Equipment.EquipItem(setup.Incoming));
        Assert.AreSame(inventoryPublicationFailure, failure.InnerException);

        CollectionAssert.AreEqual(new[] { "weapon", "shield", "incoming" }, callbacks);
        Assert.AreSame(setup.Incoming, setup.Equipment[EquipmentSlot.Weapon]);
        Assert.IsNull(setup.Equipment[EquipmentSlot.Shield]);
        Assert.AreSame(setup.Weapon, setup.Inventory.Items[0]);
        Assert.AreSame(setup.Shield, setup.Inventory.Items[1]);
        eventManager.Received(1).SendEvent(Arg.Is<IEvent>(gameEvent => gameEvent is InventoryChangedEvent));
        eventManager.DidNotReceive().SendEvent(Arg.Is<IEvent>(gameEvent => gameEvent is EquipmentChangedEvent));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void EquipItem_HookFailureAttemptsOwnedHooksAndPublishes(bool multipleHooksFail)
    {
        using var scenario = new Scenario();
        var setup = CreateWeaponShieldReplacementSetup(scenario, inventoryCapacity: 3);
        var hookFailure = new InvalidOperationException("Weapon lifecycle failed.");
        var shieldFailure = new InvalidOperationException("Shield lifecycle failed.");
        var incomingFailure = new InvalidOperationException("Incoming lifecycle failed.");
        var callbacks = new List<string>();
        setup.Weapon.EquipmentScript.CanUnEquipItem(setup.Weapon, scenario.Owner).Returns(true);
        setup.Shield.EquipmentScript.CanUnEquipItem(setup.Shield, scenario.Owner).Returns(true);
        setup.Weapon.EquipmentScript.When(script => script.OnUnequipped(setup.Weapon, scenario.Owner)).Do(_ =>
        {
            var equipmentBoundary = Synchronization(setup.Equipment);
            Assert.IsFalse(Monitor.IsEntered(equipmentBoundary.SynchronizationLock));
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
            if (call.Arg<IEvent>() is EquipmentChangedEvent) callbacks.Add("publish");
        });
        var postCommitException = Assert.ThrowsExactly<ItemContainerPostCommitPublicationException>(() =>
            setup.Equipment.EquipItem(setup.Incoming));
        var thrown = postCommitException.InnerException;

        CollectionAssert.AreEqual(new[] { "weapon", "shield", "profile", "incoming", "publish" }, callbacks);
        var expected = multipleHooksFail
            ? new Exception[] { hookFailure, shieldFailure, incomingFailure }
            : new Exception[] { hookFailure };
        if (thrown is AggregateException aggregate)
            CollectionAssert.AreEquivalent(expected, aggregate.InnerExceptions.ToArray());
        else
            Assert.AreSame(hookFailure, thrown);
        Assert.IsNotNull(hookFailure.StackTrace);
        Assert.AreSame(setup.Incoming, setup.Equipment[EquipmentSlot.Weapon]);
        Assert.IsNull(setup.Equipment[EquipmentSlot.Shield]);
        Assert.AreSame(setup.Weapon, setup.Inventory.Items[0]);
        Assert.AreSame(setup.Shield, setup.Inventory.Items[1]);
        scenario.Owner.EventManager.Received(1).SendEvent(Arg.Is<IEvent>(gameEvent => gameEvent is EquipmentChangedEvent));
    }

    [TestMethod]
    public void EquipItem_NonWeaponReplacementSwapsIntoFreedInventorySlot()
    {
        using var scenario = new Scenario();
        var inventory = CreateInventory(scenario, 2);
        scenario.Owner.Inventory.Returns(inventory);
        var equipment = new EquipmentContainer(scenario.Owner, 15, scenario.Builder);
        scenario.Owner.Equipment.Returns(equipment);
        scenario.DefaultEquipmentDefinition.Slot.Returns(EquipmentSlot.Hat);
        var equipped = scenario.Builder.Create().WithId(101).WithCount(1).Build();
        var incoming = scenario.Builder.Create().WithId(102).WithCount(1).Build();
        var inventoryBlocker = scenario.Builder.Create().WithId(103).WithCount(1).Build();
        Assert.IsTrue(equipment.TryRestoreEquippedItem(EquipmentSlot.Hat, equipped));
        Assert.IsTrue(inventory.AddItem(incoming));
        Assert.IsTrue(inventory.AddItem(inventoryBlocker));
        incoming.EquipmentScript.CanEquipItem(incoming, scenario.Owner).Returns(true);
        equipped.EquipmentScript.CanUnEquipItem(equipped, scenario.Owner).Returns(true);
        Assert.IsTrue(equipment.EquipItem(incoming));

        Assert.AreSame(incoming, equipment[EquipmentSlot.Hat]);
        Assert.AreSame(equipped, inventory.Items[0]);
        Assert.AreSame(inventoryBlocker, inventory.Items[1]);
        equipped.EquipmentScript.DidNotReceive().UnEquipItem(equipped, scenario.Owner, Arg.Any<int>());
        equipped.EquipmentScript.Received(1).OnUnequipped(equipped, scenario.Owner);
        incoming.EquipmentScript.Received(1).OnEquipped(incoming, scenario.Owner);

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
        Assert.IsTrue(inventory.AddItem(incoming));
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

        var postCommitException = Assert.ThrowsExactly<ItemContainerPostCommitPublicationException>(() => equipment.EquipItem(item));

        Assert.IsTrue(callbackSawCommittedStorage());
        Assert.IsTrue(publicationSawEquipmentEffect);
        Assert.IsNull(inventory.Items[0]);
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
        equipment.TryRestoreEquippedItem(EquipmentSlot.Hat, item);
        inventory.AddItem(scenario.Builder.Create().WithId(102).WithCount(1).Build());
        item.EquipmentScript.CanUnEquipItem(item, scenario.Owner).Returns(true);
        scenario.Owner.ClearReceivedCalls();
        var equipmentSynchronization = Synchronization(equipment);
        var inventorySynchronization = Synchronization(inventory);
        scenario.Owner.When(owner => owner.SendChatMessage(Arg.Any<string>())).Do(_ =>
        {
            Assert.IsFalse(Monitor.IsEntered(equipmentSynchronization.SynchronizationLock));
            Assert.IsFalse(Monitor.IsEntered(inventorySynchronization.SynchronizationLock));
        });
        Assert.IsFalse(equipment.UnEquipItem(item));

        scenario.Owner.Received(1).SendChatMessage("Not enough space in your inventory.");
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
        item.EquipmentScript.Received(1).OnUnequipped(item, scenario.Owner);
    }

    [TestMethod]
    public void UnEquipItem_RejectsStaleEquippedInstanceAfterPreflight()
    {
        using var scenario = new Scenario();
        var inventory = CreateInventory(scenario, 2);
        scenario.Owner.Inventory.Returns(inventory);
        var equipment = new EquipmentContainer(scenario.Owner, 15, scenario.Builder);
        scenario.Owner.Equipment.Returns(equipment);
        var item = scenario.Builder.Create().WithId(101).WithCount(1).Build();
        var replacement = scenario.Builder.Create().WithId(item.Id).WithCount(item.Count).Build();
        Assert.IsTrue(equipment.TryRestoreEquippedItem(EquipmentSlot.Hat, item));
        item.EquipmentScript.CanUnEquipItem(item, scenario.Owner).Returns(_ =>
        {
            Assert.IsTrue(equipment.TryReplaceEquippedItem(EquipmentSlot.Hat, item, replacement));
            scenario.Owner.EventManager.ClearReceivedCalls();
            scenario.Owner.ClearReceivedCalls();
            item.EquipmentScript.ClearReceivedCalls();
            replacement.EquipmentScript.ClearReceivedCalls();
            return true;
        });

        Assert.IsFalse(equipment.UnEquipItem(item));

        Assert.AreSame(replacement, equipment[EquipmentSlot.Hat]);
        Assert.IsNull(inventory.Items[0]);
        Assert.IsNull(inventory.Items[1]);
        item.EquipmentScript.DidNotReceive().OnUnequipped(item, scenario.Owner);
        replacement.EquipmentScript.DidNotReceive().OnUnequipped(replacement, scenario.Owner);
        scenario.Owner.EventManager.DidNotReceive().SendEvent(Arg.Any<IEvent>());
        scenario.Owner.DidNotReceive().SendChatMessage("Not enough space in your inventory.");
    }

    [TestMethod]
    public void UnEquipItem_UsesRequestedEmptyInventorySlot()
    {
        using var scenario = new Scenario();
        var inventory = CreateInventory(scenario, 3);
        scenario.Owner.Inventory.Returns(inventory);
        var equipment = new EquipmentContainer(scenario.Owner, 15, scenario.Builder);
        scenario.Owner.Equipment.Returns(equipment);
        var item = scenario.Builder.Create().WithId(101).WithCount(1).Build();
        Assert.IsTrue(equipment.TryRestoreEquippedItem(EquipmentSlot.Hat, item));
        item.EquipmentScript.CanUnEquipItem(item, scenario.Owner).Returns(true);

        Assert.IsTrue(equipment.UnEquipItem(item, 2));

        Assert.IsNull(equipment[EquipmentSlot.Hat]);
        Assert.IsNull(inventory.Items[0]);
        Assert.IsNull(inventory.Items[1]);
        Assert.AreSame(item, inventory.Items[2]);
        item.EquipmentScript.Received(1).OnUnequipped(item, scenario.Owner);
    }

    [TestMethod]
    public void UnEquipItem_FallsBackWhenRequestedInventorySlotBecomesOccupiedDuringPreflight()
    {
        using var scenario = new Scenario();
        var inventory = CreateInventory(scenario, 3);
        scenario.Owner.Inventory.Returns(inventory);
        var equipment = new EquipmentContainer(scenario.Owner, 15, scenario.Builder);
        scenario.Owner.Equipment.Returns(equipment);
        var item = scenario.Builder.Create().WithId(101).WithCount(1).Build();
        var blocker = scenario.Builder.Create().WithId(102).WithCount(1).Build();
        Assert.IsTrue(equipment.TryRestoreEquippedItem(EquipmentSlot.Hat, item));
        item.EquipmentScript.CanUnEquipItem(item, scenario.Owner).Returns(_ =>
        {
        Assert.IsTrue(inventory.AddItem(2, blocker));
            scenario.Owner.EventManager.ClearReceivedCalls();
            scenario.Owner.ClearReceivedCalls();
            return true;
        });

        Assert.IsTrue(equipment.UnEquipItem(item, 2));

        Assert.IsNull(equipment[EquipmentSlot.Hat]);
        Assert.AreSame(item, inventory.Items[0]);
        Assert.IsNull(inventory.Items[1]);
        Assert.AreSame(blocker, inventory.Items[2]);
        item.EquipmentScript.Received(1).OnUnequipped(item, scenario.Owner);
    }

    [TestMethod]
    public void RewardClaim_WhenInventoryFullLeavesRewardUnchanged()
    {
        using var scenario = new Scenario();
        var inventory = CreateInventory(scenario, 1);
        scenario.Owner.Inventory.Returns(inventory);
        var rewards = new RewardContainer(scenario.Owner, scenario.Builder);
        inventory.AddItem(scenario.Builder.Create().WithId(102).WithCount(1).Build());
        var rewardItem = scenario.Builder.Create().WithId(101).WithCount(4).Build();
        rewards.Hydrate([new HydratedItemDto(101, 4, 0, null)]);
        rewardItem = rewards.Items[0]!;

        Assert.AreEqual(-1, rewards.Claim(rewardItem, 1));

        Assert.AreEqual(4, rewards.Items.GetCountById(101));
        Assert.AreEqual(0, inventory.Items.GetCountById(101));
        Assert.AreEqual(1, inventory.Items.GetCountById(102));
    }
    [TestMethod]
    public void Dispose_PouchEffectsAreDiscardedWithoutLeakingIntoLaterScope()
    {
        using var scenario = new Scenario();
        var inventory = CreateInventory(scenario, 4);
        scenario.Owner.Inventory.Returns(inventory);
        var pouch = new MoneyPouchContainer(scenario.Owner, new ComposedTestItemBuilder());
        Assert.IsTrue(pouch.Add(10));
        scenario.Owner.ClearReceivedCalls();
        using (ItemContainerTransaction.Begin(pouch, inventory))
        {
            Assert.IsTrue(pouch.Add(2));
            Assert.IsTrue(inventory.AddItems([scenario.Builder.Create().WithId(995).WithCount(3).Build()]));
            Assert.IsTrue(pouch.TryRemoveExact(3));
            Assert.AreEqual(9, pouch.Count);
            Assert.AreEqual(3, inventory.Items.GetCountById(995));
        }
        Assert.AreEqual(10, pouch.Count);
        Assert.AreEqual(0, inventory.Items.GetCountById(995));
        using (var fresh = ItemContainerTransaction.Begin(pouch)) fresh.Commit();
        scenario.Owner.DidNotReceive().SendChatMessage(Arg.Any<string>());
    }

    [TestMethod]
    public void EquipItem_NonWeaponReplacementRunsOnEquippedBeforePublication()
    {
        using var scenario = new Scenario();
        var setup = CreateNonWeaponReplacementSetup(scenario);
        var order = new List<string>();
        ObserveReplacementCompletion(setup, scenario.Owner, order, null, null);

        Assert.IsTrue(setup.Equipment.EquipItem(setup.Incoming));

        Assert.AreSame(setup.Incoming, setup.Equipment[EquipmentSlot.Hat]);
        CollectionAssert.AreEqual(new[] { "unequip", "equip", "publish" }, order);
        setup.Current.EquipmentScript.DidNotReceive().UnEquipItem(setup.Current, scenario.Owner, Arg.Any<int>());
    }

    [TestMethod]
    public void EquipItem_NonWeaponReplacementRollsBackOnDestinationPlanningFailure()
    {
        using var scenario = new Scenario();
        var inventory = CreateInventory(scenario, 2);
        scenario.Owner.Inventory.Returns(inventory);
        var (equipment, eventManager) = CreateEquipmentScenario(scenario);
        scenario.Owner.Equipment.Returns(equipment);
        scenario.DefaultEquipmentDefinition.Slot.Returns(EquipmentSlot.Hat);
        var current = new TransferProbeItem(101, 2);
        current.EquipmentDefinition.Slot.Returns(EquipmentSlot.Hat);
        var incoming = scenario.Builder.Create().WithId(102).WithCount(1).Build();
        Assert.IsTrue(equipment.TryRestoreEquippedItem(EquipmentSlot.Hat, current));
        Assert.IsTrue(inventory.AddItem(incoming));
        current.EquipmentScript.CanUnEquipItem(current, scenario.Owner).Returns(true);
        incoming.EquipmentScript.CanEquipItem(incoming, scenario.Owner).Returns(true);
        current.OnClone = () =>
            throw new InvalidOperationException("Controlled destination planning failure.");
        eventManager.ClearReceivedCalls();

        Assert.ThrowsExactly<InvalidOperationException>(() => equipment.EquipItem(incoming));

        Assert.AreSame(incoming, inventory.Items[0]);
        Assert.AreEqual(1, inventory.Items.GetCountById(incoming.Id));
        Assert.AreSame(current, equipment[EquipmentSlot.Hat]);
        Assert.AreEqual(2, current.Count);
        eventManager.DidNotReceive().SendEvent(Arg.Any<IEvent>());
        current.EquipmentScript.DidNotReceive().OnUnequipped(current, scenario.Owner);
        incoming.EquipmentScript.DidNotReceive().OnEquipped(incoming, scenario.Owner);
    }

    [TestMethod]
    public void EquipItem_RequiresTheExactIncomingInstanceToRemainInInventory()
    {
        using var scenario = new Scenario();
        var setup = CreateNonWeaponReplacementSetup(scenario);
        var originalSlot = setup.Inventory.Items.GetInstanceSlot(setup.Incoming);

        Assert.IsTrue(setup.Inventory.TryRemoveItemExact(setup.Incoming, originalSlot));
        var equivalent = scenario.Builder.Create().WithId(setup.Incoming.Id).WithCount(1).Build();
        Assert.IsTrue(setup.Inventory.AddItem(equivalent));
        setup.EventManager.ClearReceivedCalls();

        Assert.IsFalse(setup.Equipment.EquipItem(setup.Incoming));

        Assert.AreSame(equivalent, setup.Inventory.Items[originalSlot]);
        Assert.AreSame(setup.Current, setup.Equipment[EquipmentSlot.Hat]);
        Assert.AreEqual(-1, setup.Inventory.Items.GetInstanceSlot(setup.Incoming));
        setup.EventManager.DidNotReceive().SendEvent(Arg.Any<IEvent>());
    }

    [TestMethod]
    public void EquipItem_RepeatedRequestForSameInstanceDoesNotDuplicateItems()
    {
        using var scenario = new Scenario();
        var setup = CreateNonWeaponReplacementSetup(scenario);
        setup.EventManager.ClearReceivedCalls();

        Assert.IsTrue(setup.Equipment.EquipItem(setup.Incoming));
        Assert.IsFalse(setup.Equipment.EquipItem(setup.Incoming));

        Assert.AreSame(setup.Incoming, setup.Equipment[EquipmentSlot.Hat]);
        Assert.AreSame(setup.Current, setup.Inventory.Items[0]);
        setup.Current.EquipmentScript.Received(1).OnUnequipped(setup.Current, scenario.Owner);
        setup.Incoming.EquipmentScript.Received(1).OnEquipped(setup.Incoming, scenario.Owner);
        setup.EventManager.Received(2).SendEvent(Arg.Any<IEvent>());
    }

    [TestMethod]
    public async Task EquipItem_ConcurrentRequestsForSameInstanceDoNotDuplicateItems()
    {
        using var scenario = new Scenario();
        var setup = CreateNonWeaponReplacementSetup(scenario);
        setup.EventManager.ClearReceivedCalls();

        using var barrier = new Barrier(2);
        var firstEquip = Task.Run(() =>
        {
            Assert.IsTrue(barrier.SignalAndWait(TimeSpan.FromSeconds(5)));
            return setup.Equipment.EquipItem(setup.Incoming);
        });
        var secondEquip = Task.Run(() =>
        {
            Assert.IsTrue(barrier.SignalAndWait(TimeSpan.FromSeconds(5)));
            return setup.Equipment.EquipItem(setup.Incoming);
        });
        var results = await Task.WhenAll(firstEquip, secondEquip);

        Assert.IsTrue(results[0] ^ results[1]);
        Assert.AreSame(setup.Incoming, setup.Equipment[EquipmentSlot.Hat]);
        Assert.AreSame(setup.Current, setup.Inventory.Items[0]);
        Assert.AreEqual(1, setup.Inventory.Items.GetCountById(setup.Current.Id));
        Assert.AreEqual(1, setup.Equipment.Items.GetCountById(setup.Incoming.Id));
        setup.Current.EquipmentScript.Received(1).OnUnequipped(setup.Current, scenario.Owner);
        setup.Incoming.EquipmentScript.Received(1).OnEquipped(setup.Incoming, scenario.Owner);
        setup.EventManager.Received(2).SendEvent(Arg.Any<IEvent>());
    }

    [TestMethod]
    public void DropItem_DoesNotCreateGroundItemWhenExactRemovalLosesRace()
    {
        using var scenario = new Scenario();
        var mapRegionService = Substitute.For<IMapRegionService>();
        var groundItemBuilder = Substitute.For<IGroundItemBuilder>();
        var inventory = new InventoryContainer(scenario.Owner, 1, mapRegionService, groundItemBuilder, scenario.Builder);
        IItem item = new TransferProbeItem(101, 1);
        Assert.IsTrue(inventory.AddItem(item));
        using var cloneEntered = new ManualResetEventSlim();
        using var continueClone = new ManualResetEventSlim();
        ((TransferProbeItem)item).OnClone = () =>
        {
            cloneEntered.Set();
            if (!continueClone.Wait(TimeSpan.FromSeconds(5)))
                throw new TimeoutException("Drop clone was not released by the test.");
        };
        var dropResult = true;
        Exception? dropFailure = null;
        var dropThread = new Thread(() =>
        {
            try { dropResult = inventory.DropItem(item); }
            catch (Exception exception) { dropFailure = exception; }
        }) { IsBackground = true };
        dropThread.Start();

        try
        {
            Assert.IsTrue(cloneEntered.Wait(TimeSpan.FromSeconds(5)));
            Assert.IsTrue(inventory.TryRemoveItemExact(item));
        }
        finally
        {
            continueClone.Set();
        }

        Assert.IsTrue(dropThread.Join(TimeSpan.FromSeconds(5)));
        Assert.IsNull(dropFailure);
        Assert.IsFalse(dropResult);
        mapRegionService.DidNotReceive().AddGroundItem(Arg.Any<IGroundItem>());
    }

    [TestMethod]
    public void DropItem_CreatesGroundItemWithExactRemovedQuantity()
    {
        using var scenario = new Scenario();
        var mapRegionService = Substitute.For<IMapRegionService>();
        var groundItemBuilder = Substitute.For<IGroundItemBuilder>();
        var onGround = Substitute.For<IGroundItemOnGround>();
        var location = Substitute.For<IGroundItemLocation>();
        var optional = Substitute.For<IGroundItemOptional>();
        var groundItem = Substitute.For<IGroundItem>();
        var ownerLocation = Substitute.For<ILocation>();
        scenario.Owner.Location.Returns(ownerLocation);
        groundItemBuilder.Create().Returns(onGround);
        onGround.WithItem(Arg.Any<IItem>()).Returns(location);
        location.WithLocation(ownerLocation).Returns(optional);
        optional.WithOwner(scenario.Owner).Returns(optional);
        optional.Build().Returns(groundItem);
        var inventory = new InventoryContainer(scenario.Owner, 1, mapRegionService, groundItemBuilder, scenario.Builder);
        var item = scenario.Builder.Create().WithId(101).WithCount(4).Build();
        Assert.IsTrue(inventory.AddItem(item));

        Assert.IsTrue(inventory.DropItem(item));

        Assert.IsNull(inventory.Items[0]);
        onGround.Received(1).WithItem(Arg.Is<IItem>(dropped =>
            !ReferenceEquals(dropped, item) && dropped.Id == item.Id && dropped.Count == 4));
        mapRegionService.Received(1).AddGroundItem(groundItem);
    }
    [TestMethod]
    public void EquipItem_NonWeaponReplacementOnEquippedFailurePublishesCommittedState()
    {
        using var scenario = new Scenario();
        var setup = CreateNonWeaponReplacementSetup(scenario);
        var failure = new InvalidOperationException("incoming equip failed");
        var order = new List<string>();
        ObserveReplacementCompletion(setup, scenario.Owner, order, failure, null);

        var postCommitException = Assert.ThrowsExactly<ItemContainerPostCommitPublicationException>(() =>
            setup.Equipment.EquipItem(setup.Incoming));
        Assert.AreSame(failure, postCommitException.InnerException);
        CollectionAssert.AreEqual(new[] { "unequip", "equip", "publish" }, order);
        setup.EventManager.Received(1).SendEvent(Arg.Is<IEvent>(gameEvent => gameEvent is EquipmentChangedEvent));
        AssertReplacementCommittedAndReleased(setup);
    }

    [TestMethod]
    public void EquipItem_NonWeaponReplacementPublicationFailureFollowsOnEquipped()
    {
        using var scenario = new Scenario();
        var setup = CreateNonWeaponReplacementSetup(scenario);
        var failure = new InvalidOperationException("equipment publication failed");
        var order = new List<string>();
        ObserveReplacementCompletion(setup, scenario.Owner, order, null, failure);

        var postCommitException = Assert.ThrowsExactly<ItemContainerPostCommitPublicationException>(() =>
            setup.Equipment.EquipItem(setup.Incoming));
        Assert.AreSame(failure, postCommitException.InnerException);
        CollectionAssert.AreEqual(new[] { "unequip", "equip", "publish" }, order);
        AssertReplacementCommittedAndReleased(setup);
    }

    [TestMethod]
    public void EquipItem_NonWeaponReplacementRejectsEnlistedEquipmentBeforeMutation()
    {
        using var scenario = new Scenario();
        var setup = CreateNonWeaponReplacementSetup(scenario);
        AssertReplacementRejectsExistingEnlistment(setup, scenario.Owner, setup.Equipment);
    }

    [TestMethod]
    public void EquipItem_NonWeaponReplacementRejectsEnlistedInventoryBeforeMutation()
    {
        using var scenario = new Scenario();
        var setup = CreateNonWeaponReplacementSetup(scenario);
        AssertReplacementRejectsExistingEnlistment(setup, scenario.Owner, setup.Inventory);
    }

    [TestMethod]
    public void EquipItem_StandaloneOwnershipSpansLifecycleAndPublication()
    {
        using var scenario = new Scenario();
        var setup = CreateNonWeaponReplacementSetup(scenario);
        using var tryBegin = new ManualResetEventSlim();
        using var beginAttempted = new ManualResetEventSlim();
        using var transactionEntered = new ManualResetEventSlim();
        using var allowTransactionToCommit = new ManualResetEventSlim();
        using var publicationEntered = new ManualResetEventSlim();
        using var allowPublication = new ManualResetEventSlim();
        Exception? transactionFailure = null;
        var transactionThread = new Thread(() =>
        {
            try
            {
                Assert.IsTrue(tryBegin.Wait(TimeSpan.FromSeconds(5)));
                beginAttempted.Set();
                using var transaction = ItemContainerTransaction.Begin(setup.Equipment);
                transactionEntered.Set();
                Assert.IsTrue(allowTransactionToCommit.Wait(TimeSpan.FromSeconds(5)));
                transaction.Commit();
            }
            catch (Exception exception) { transactionFailure = exception; }
        }) { IsBackground = true };
        transactionThread.Start();

        setup.Incoming.EquipmentScript.When(script => script.OnEquipped(setup.Incoming, scenario.Owner)).Do(_ =>
        {
            Assert.IsFalse(Monitor.IsEntered(setup.SynchronizationLock));
            Assert.ThrowsExactly<InvalidOperationException>(() => ItemContainerTransaction.Begin(setup.Equipment));
            tryBegin.Set();
            Assert.IsTrue(beginAttempted.Wait(TimeSpan.FromSeconds(5)));
            Assert.IsTrue(SpinWait.SpinUntil(
                () => (transactionThread.ThreadState & ThreadState.WaitSleepJoin) != 0,
                TimeSpan.FromSeconds(5)), "Begin must wait through Equipment lifecycle completion.");
            Assert.IsFalse(transactionEntered.IsSet);
        });
        setup.EventManager.When(manager => manager.SendEvent(Arg.Any<IEvent>())).Do(call =>
        {
            if (call.Arg<IEvent>() is EquipmentChangedEvent &&
                ReferenceEquals(setup.Incoming, setup.Equipment[EquipmentSlot.Hat]))
            {
                Assert.IsFalse(Monitor.IsEntered(setup.SynchronizationLock));
                Assert.IsFalse(transactionEntered.IsSet);
                publicationEntered.Set();
                Assert.IsTrue(allowPublication.Wait(TimeSpan.FromSeconds(5)));
            }
        });

        Exception? equipFailure = null;
        var equipThread = new Thread(() =>
        {
            try { Assert.IsTrue(setup.Equipment.EquipItem(setup.Incoming)); }
            catch (Exception exception) { equipFailure = exception; }
        }) { IsBackground = true };
        equipThread.Start();
        try
        {
            Assert.IsTrue(publicationEntered.Wait(TimeSpan.FromSeconds(5)));
            Assert.IsFalse(transactionEntered.IsSet);
            allowPublication.Set();
            Assert.IsTrue(transactionEntered.Wait(TimeSpan.FromSeconds(5)));
            allowTransactionToCommit.Set();
        }
        finally
        {
            allowPublication.Set();
            allowTransactionToCommit.Set();
        }

        Assert.IsTrue(equipThread.Join(TimeSpan.FromSeconds(5)));
        Assert.IsTrue(transactionThread.Join(TimeSpan.FromSeconds(5)));
        Assert.IsNull(equipFailure);
        Assert.IsNull(transactionFailure);
        Assert.AreSame(setup.Incoming, setup.Equipment[EquipmentSlot.Hat]);
    }

    private static void ObserveReplacementCompletion(
        (InventoryContainer Inventory, EquipmentContainer Equipment, IEventManager EventManager,
            IItem Current, IItem Incoming, object SynchronizationLock) setup,
        ICharacter owner,
        List<string> order,
        Exception? equipFailure,
        Exception? publicationFailure)
    {
        setup.Current.EquipmentScript.When(script => script.OnUnequipped(setup.Current, owner)).Do(_ =>
        {
            Assert.AreSame(setup.Incoming, setup.Equipment[EquipmentSlot.Hat]);
            Assert.AreSame(setup.Current, setup.Inventory.Items[0]);
            Assert.IsFalse(Monitor.IsEntered(setup.SynchronizationLock));
            order.Add("unequip");
        });
        setup.Incoming.EquipmentScript.When(script => script.OnEquipped(setup.Incoming, owner)).Do(_ =>
        {
            Assert.AreSame(setup.Incoming, setup.Equipment[EquipmentSlot.Hat]);
            Assert.AreSame(setup.Current, setup.Inventory.Items[0]);
            Assert.IsFalse(Monitor.IsEntered(setup.SynchronizationLock));
            order.Add("equip");
            if (equipFailure is not null) throw equipFailure;
        });
        setup.EventManager.When(manager => manager.SendEvent(Arg.Any<IEvent>())).Do(call =>
        {
            if (call.Arg<IEvent>() is EquipmentChangedEvent &&
                ReferenceEquals(setup.Incoming, setup.Equipment[EquipmentSlot.Hat]))
            {
                Assert.AreSame(setup.Incoming, setup.Equipment[EquipmentSlot.Hat]);
                Assert.IsFalse(Monitor.IsEntered(setup.SynchronizationLock));
                order.Add("publish");
                if (publicationFailure is not null) throw publicationFailure;
            }
        });
    }

    private static void AssertReplacementCommittedAndReleased(
        (InventoryContainer Inventory, EquipmentContainer Equipment, IEventManager EventManager,
            IItem Current, IItem Incoming, object SynchronizationLock) setup)
    {
        Assert.AreSame(setup.Incoming, setup.Equipment[EquipmentSlot.Hat]);
        Assert.AreSame(setup.Current, setup.Inventory.Items[0]);
        using var transaction = ItemContainerTransaction.Begin(setup.Equipment, setup.Inventory);
        transaction.Commit();
    }

    private static void AssertReplacementRejectsExistingEnlistment(
        (InventoryContainer Inventory, EquipmentContainer Equipment, IEventManager EventManager,
            IItem Current, IItem Incoming, object SynchronizationLock) setup,
        ICharacter owner,
        IItemTransactional participant)
    {
        using var transaction = ItemContainerTransaction.Begin(participant);
        Assert.ThrowsExactly<InvalidOperationException>(() => setup.Equipment.EquipItem(setup.Incoming));
        Assert.AreSame(setup.Current, setup.Equipment[EquipmentSlot.Hat]);
        Assert.AreEqual(1, setup.Inventory.Items.GetCountById(setup.Incoming.Id));
        setup.Current.EquipmentScript.DidNotReceive().UnEquipItem(setup.Current, owner, 0);
    }

    [TestMethod]
    public void EquipmentCompletion_AttemptsAllEffectsAndPublishesAfterFailure()
    {
        using var scenario = new Scenario();
        var (equipment, events, current, replacement) = CreateReplacementScenario(scenario);
        var firstFailure = new InvalidOperationException("first unequip failure");
        var secondFailure = new InvalidOperationException("second unequip failure");
        var unequipFailure = new AggregateException(firstFailure, secondFailure);
        var equipFailure = new InvalidOperationException("equip failure");
        var order = ObserveFailingReplacementEffects(scenario.Owner, current, replacement, unequipFailure, equipFailure);
        events.When(manager => manager.SendEvent(Arg.Any<IEvent>())).Do(call =>
        {
            if (call.Arg<IEvent>() is EquipmentChangedEvent) order.Add("publish");
        });
        var thrown = Assert.ThrowsExactly<AggregateException>(() =>
            Assert.IsTrue(equipment.TryReplaceEquippedItem(EquipmentSlot.Hat, current, replacement)));
        CollectionAssert.AreEqual(new[] { "unequip", "equip", "publish" }, order);
        CollectionAssert.AreEquivalent(new Exception[] { firstFailure, secondFailure, equipFailure },
            thrown.InnerExceptions.ToArray());
        Assert.IsNotNull(unequipFailure.StackTrace);
        Assert.IsNotNull(equipFailure.StackTrace);
        events.Received(1).SendEvent(Arg.Is<IEvent>(gameEvent => gameEvent is EquipmentChangedEvent));
        Assert.AreSame(replacement, equipment[EquipmentSlot.Hat]);
        CollectionAssert.AreEqual(new[] { "unequip", "equip", "publish" }, order);
    }

    [TestMethod]
    public void EquipmentCompletion_PreservesHookAndPublicationFailures()
    {
        using var scenario = new Scenario();
        var (equipment, events, current, replacement) = CreateReplacementScenario(scenario);
        var hookFailure = new InvalidOperationException("unequip failed");
        var publicationFailure = new InvalidOperationException("equipment publication failed");
        var order = ObserveFailingReplacementEffects(scenario.Owner, current, replacement, hookFailure, null);
        events.When(manager => manager.SendEvent(Arg.Any<IEvent>())).Do(call =>
        {
            if (call.Arg<IEvent>() is EquipmentChangedEvent)
            {
                order.Add("publish");
                throw publicationFailure;
            }
        });

        var thrown = Assert.ThrowsExactly<AggregateException>(() =>
            Assert.IsTrue(equipment.TryReplaceEquippedItem(EquipmentSlot.Hat, current, replacement)));

        CollectionAssert.AreEquivalent(new Exception[] { hookFailure, publicationFailure }, thrown.InnerExceptions.ToArray());
        CollectionAssert.AreEqual(new[] { "unequip", "equip", "publish" }, order);
        Assert.AreSame(replacement, equipment[EquipmentSlot.Hat]);
        Assert.IsNotNull(hookFailure.StackTrace);
        Assert.IsNotNull(publicationFailure.StackTrace);
        events.Received(1).SendEvent(Arg.Is<IEvent>(gameEvent => gameEvent is EquipmentChangedEvent));
    }

    private static List<string> ObserveFailingReplacementEffects(ICharacter owner, IItem current, IItem replacement,
        Exception unequipFailure, Exception? equipFailure)
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
            if (equipFailure is not null) throw equipFailure;
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

    private static (EquipmentContainer Equipment, IEventManager EventManager, ItemContainer Destination, IItem Item)
        CreateEquipmentMoveScenario(Scenario scenario, int count = 1)
    {
        var (equipment, eventManager) = CreateEquipmentScenario(scenario);
        var item = scenario.Builder.Create().WithId(101).WithCount(count).Build();
        Assert.IsTrue(equipment.TryRestoreEquippedItem(EquipmentSlot.Hat, item));
        eventManager.ClearReceivedCalls();
        var destination = new ItemContainer(StorageType.Normal, 2);
        return (equipment, eventManager, destination, item);
    }

    private static ItemContainerSynchronization Synchronization(IItemTransactional participant) =>
        ((IItemTransactionSource)participant).Synchronizations.Single();

    private static void AssertStandalonePublicationOwnsBoundaryAfterUnlock(ItemContainerSynchronization boundary)
    {
        Assert.IsFalse(Monitor.IsEntered(boundary.SynchronizationLock));
        lock (boundary.SynchronizationLock)
        {
            Assert.IsTrue(boundary.IsStandalonePublicationOwnedByCurrentThread);
        }
    }

    private static void AssertBankPublicationOwnsBoundaryAfterUnlock(IEventManager eventManager,
        ItemContainerSynchronization boundary) =>
        eventManager.When(manager => manager.SendEvent(Arg.Any<IEvent>())).Do(call =>
        {
            if (call.Arg<IEvent>() is not BankChangedEvent) return;

            AssertStandalonePublicationOwnsBoundaryAfterUnlock(boundary);
        });

    private static void AssertCompletionRunsAfterUnlock(EquipmentContainer equipment, ItemContainer destination)
    {
        var equipmentSynchronization = Synchronization(equipment);
        foreach (var boundary in new[] { equipmentSynchronization, Synchronization(destination) })
        {
            Assert.IsFalse(Monitor.IsEntered(boundary.SynchronizationLock));
        }
    }

    private static void AssertMutationLocksReleased(EquipmentContainer equipment,
        IItemTransactional destination)
    {
        foreach (var boundary in new[] { Synchronization(equipment), Synchronization(destination) })
        {
            Assert.IsFalse(Monitor.IsEntered(boundary.SynchronizationLock));
        }
    }

    private static void AssertMoveRejectedWithoutMutation(EquipmentContainer equipment, IEventManager eventManager,
        ItemContainer destination, IItem item, ICharacter owner)
    {
        Assert.AreSame(item, equipment[EquipmentSlot.Hat]);
        Assert.IsNull(destination[0]);
        item.EquipmentScript.DidNotReceive().OnUnequipped(item, owner);
        eventManager.DidNotReceive().SendEvent(Arg.Any<IEvent>());
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

    private static (InventoryContainer Inventory, EquipmentContainer Equipment, IEventManager EventManager,
        IItem Current, IItem Incoming, object SynchronizationLock) CreateNonWeaponReplacementSetup(Scenario scenario, int equippedCount = 1)
    {
        var inventory = CreateInventory(scenario, 2);
        scenario.Owner.Inventory.Returns(inventory);
        var (equipment, eventManager) = CreateEquipmentScenario(scenario);
        scenario.Owner.Equipment.Returns(equipment);
        scenario.DefaultEquipmentDefinition.Slot.Returns(EquipmentSlot.Hat);
        var current = scenario.Builder.Create().WithId(101).WithCount(equippedCount).Build();
        var incoming = scenario.Builder.Create().WithId(102).WithCount(1).Build();
        Assert.IsTrue(equipment.TryRestoreEquippedItem(EquipmentSlot.Hat, current));
        Assert.IsTrue(inventory.AddItem(incoming));
        incoming.EquipmentScript.CanEquipItem(incoming, scenario.Owner).Returns(true);
        current.EquipmentScript.CanUnEquipItem(current, scenario.Owner).Returns(true);
        eventManager.ClearReceivedCalls();
        var synchronizationLock = Synchronization(equipment).SynchronizationLock;
        return (inventory, equipment, eventManager, current, incoming, synchronizationLock);
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
        Assert.IsTrue(inventory.AddItem(item));
        item.EquipmentScript.CanEquipItem(item, scenario.Owner).Returns(true);
        return (inventory, equipment, item);
    }

    private static (InventoryContainer Inventory, EquipmentContainer Equipment, IEventManager EventManager,
        IItem Weapon, IItem Shield) CreateEmptyWeaponShieldSetup(Scenario scenario)
    {
        var inventory = CreateInventory(scenario, 3);
        scenario.Owner.Inventory.Returns(inventory);
        var (equipment, eventManager) = CreateEquipmentScenario(scenario);
        scenario.Owner.Equipment.Returns(equipment);

        var weaponDefinition = Substitute.For<IEquipmentDefinition>();
        weaponDefinition.Slot.Returns(EquipmentSlot.Weapon);
        weaponDefinition.Type.Returns(EquipmentType.TwoHanded);
        weaponDefinition.AttackStyleIDs.Returns(Array.Empty<AttackStyle>());
        scenario.DefineEquipment(101, weaponDefinition);
        var shieldDefinition = Substitute.For<IEquipmentDefinition>();
        shieldDefinition.Slot.Returns(EquipmentSlot.Shield);
        scenario.DefineEquipment(102, shieldDefinition);

        var weapon = scenario.Builder.Create().WithId(101).WithCount(1).Build();
        var shield = scenario.Builder.Create().WithId(102).WithCount(1).Build();
        Assert.IsTrue(inventory.AddItem(weapon));
        Assert.IsTrue(inventory.AddItem(shield));
        weapon.EquipmentScript.CanEquipItem(weapon, scenario.Owner).Returns(true);
        shield.EquipmentScript.CanEquipItem(shield, scenario.Owner).Returns(true);
        eventManager.ClearReceivedCalls();
        return (inventory, equipment, eventManager, weapon, shield);
    }

    private static bool RunEquipAfterConcurrentEquipmentMutation(
        InventoryContainer inventory,
        EquipmentContainer equipment,
        ICharacter owner,
        IItem incoming,
        EquipmentSlot concurrentSlot,
        IItem concurrentItem)
    {
        var transaction = ItemContainerTransaction.Begin(inventory, equipment);
        using var preflightCompleted = new ManualResetEventSlim();
        Exception? equipFailure = null;
        var equipResult = false;
        var equipThread = new Thread(() =>
        {
            try { equipResult = equipment.EquipItem(incoming); }
            catch (Exception exception) { equipFailure = exception; }
        }) { IsBackground = true };
        var threadStarted = false;
        incoming.EquipmentScript.CanEquipItem(incoming, owner).Returns(_ =>
        {
            preflightCompleted.Set();
            return true;
        });

        try
        {
            equipThread.Start();
            threadStarted = true;
            Assert.IsTrue(preflightCompleted.Wait(TimeSpan.FromSeconds(5)),
                "The background equipment request did not complete eligibility preflight.");
            Assert.IsTrue(SpinWait.SpinUntil(
                    () => (equipThread.ThreadState & ThreadState.WaitSleepJoin) != 0,
                    TimeSpan.FromSeconds(5)),
                "The background equipment request did not block while acquiring the transaction.");
            Assert.IsTrue(inventory.TryRemoveItemExact(concurrentItem));
            Assert.IsTrue(equipment.TryRestoreEquippedItem(concurrentSlot, concurrentItem));
            transaction.Commit();
        }
        finally
        {
            try { transaction.Dispose(); }
            finally
            {
                if (threadStarted && !equipThread.Join(TimeSpan.FromSeconds(5)))
                    throw new TimeoutException("The background equipment request did not finish after transaction release.");
            }
        }

        Assert.IsNull(equipFailure, equipFailure?.ToString());
        return equipResult;
    }

    private static void AssertEmptyEquipmentSlotEquipSucceeds(EquipmentSlot slot, int itemId)
    {
        using var scenario = new Scenario();
        var inventory = CreateInventory(scenario, 1);
        scenario.Owner.Inventory.Returns(inventory);
        var (equipment, eventManager) = CreateEquipmentScenario(scenario);
        scenario.Owner.Equipment.Returns(equipment);

        var definition = Substitute.For<IEquipmentDefinition>();
        definition.Slot.Returns(slot);
        definition.Type.Returns(EquipmentType.Normal);
        scenario.DefineEquipment(itemId, definition);
        var item = scenario.Builder.Create().WithId(itemId).WithCount(1).Build();
        Assert.IsTrue(inventory.AddItem(item));
        item.EquipmentScript.CanEquipItem(item, scenario.Owner).Returns(true);
        eventManager.ClearReceivedCalls();

        var order = new List<string>();
        item.EquipmentScript.When(script => script.OnEquipped(item, scenario.Owner)).Do(_ =>
        {
            Assert.IsNull(inventory.Items[0]);
            Assert.AreSame(item, equipment[slot]);
            order.Add("equipped");
        });
        eventManager.When(manager => manager.SendEvent(Arg.Any<IEvent>())).Do(call =>
        {
            if (call.Arg<IEvent>() is not EquipmentChangedEvent) return;
            CollectionAssert.AreEqual(new[] { "equipped" }, order);
            order.Add("published");
        });

        Assert.IsTrue(equipment.EquipItem(item));

        Assert.AreSame(item, equipment[slot]);
        Assert.AreEqual(-1, inventory.Items.GetInstanceSlot(item));
        item.EquipmentScript.Received(1).OnEquipped(item, scenario.Owner);
        CollectionAssert.AreEqual(new[] { "equipped", "published" }, order);
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
        Assert.IsTrue(inventory.AddItem(incoming));
        incoming.EquipmentScript.CanEquipItem(incoming, scenario.Owner).Returns(true);
        eventManager.ClearReceivedCalls();
        return (inventory, equipment, weapon, shield, incoming);
    }

    private static Func<bool> ObserveEquippedState(
        ICharacter owner,
        IReadOnlyItemContainer inventory,
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

    private sealed class TransferProbeItem : IItem
    {
        private int _count;
        private readonly IItemDefinition _definition = Substitute.For<IItemDefinition>();
        private readonly IItemScript _itemScript = Substitute.For<IItemScript>();

        public int Id { get; }
        public int Count
        {
            get
            {
                OnCountRead?.Invoke();
                return _count;
            }
            set => _count = value;
        }
        public Action? OnCountRead { get; set; }
        public Action? OnClone { get; set; }
        public string Name => $"Probe item {Id}";
        public long[] ExtraData => [];
        public IItemDefinition ItemDefinition => _definition;
        public IEquipmentDefinition EquipmentDefinition { get; } = Substitute.For<IEquipmentDefinition>();
        public IItemScript ItemScript => _itemScript;
        public IEquipmentScript EquipmentScript { get; } = Substitute.For<IEquipmentScript>();

        public TransferProbeItem(int id, int count)
        {
            Id = id;
            _count = count;
            _definition.Stackable.Returns(true);
            _definition.Noted.Returns(false);
            _itemScript.CanStackItem(Arg.Any<IItem>(), Arg.Any<IItem>(), Arg.Any<bool>()).Returns(true);
        }

        public IItem Clone() => Clone(1);
        public IItem Clone(int newCount)
        {
            OnClone?.Invoke();
            return new TransferProbeItem(Id, newCount);
        }
        public bool Equals(IItem otherItem, bool ignoreCount = true) =>
            otherItem != null && Id == otherItem.Id && (ignoreCount || Count == otherItem.Count);
        public string? SerializeExtraData() => null;
    }
}
