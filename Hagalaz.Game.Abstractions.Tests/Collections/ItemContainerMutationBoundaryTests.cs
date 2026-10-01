using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Items;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Abstractions.Tests.Collections;

[TestClass]
public sealed class ItemContainerMutationBoundaryTests
{
    [TestMethod]
    public void TryTransferTo_InterfaceTypedBoundariesMoveExactQuantityAndPublishAfterCommit()
    {
        var sourceUpdates = 0;
        var destinationUpdates = 0;
        var source = new ItemContainer(StorageType.Normal, 2, _ => sourceUpdates++);
        var destination = new ItemContainer(StorageType.Normal, 2, _ => destinationUpdates++);
        var item = new TestItem(10, 7);
        Assert.IsTrue(source.Add(item));

        IItemContainerMutationBoundary sourceBoundary = source.Mutations;
        IItemContainerMutationBoundary destinationBoundary = destination.Mutations;
        Assert.IsTrue(sourceBoundary.TryTransferTo(destinationBoundary, item, 7, 0));

        Assert.AreEqual(0, source.TakenSlots);
        Assert.AreSame(item, destination[0]);
        Assert.AreEqual(7, destination[0]!.Count);
        Assert.AreEqual(2, sourceUpdates);
        Assert.AreEqual(1, destinationUpdates);
    }

    [TestMethod]
    public void TryTransferTo_InterfaceTypedBoundariesLeaveBothStoragesUnchangedOnFailure()
    {
        var sourceUpdates = 0;
        var destinationUpdates = 0;
        var source = new ItemContainer(StorageType.Normal, 2, _ => sourceUpdates++);
        var destination = new ItemContainer(StorageType.Normal, 1, _ => destinationUpdates++);
        var item = new TestItem(10, 2);
        var blockingItem = new TestItem(11, 1);
        Assert.IsTrue(source.Add(item));
        Assert.IsTrue(destination.Add(blockingItem));

        IItemContainerMutationBoundary sourceBoundary = source.Mutations;
        IItemContainerMutationBoundary destinationBoundary = destination.Mutations;
        Assert.IsFalse(sourceBoundary.TryTransferTo(destinationBoundary, item, 2, 0));

        Assert.AreSame(item, source[0]);
        Assert.AreEqual(2, source[0]!.Count);
        Assert.AreSame(blockingItem, destination[0]);
        Assert.AreEqual(1, sourceUpdates);
        Assert.AreEqual(1, destinationUpdates);
    }

    [TestMethod]
    public void Transaction_InterfaceTypedParticipantsRollbackWithoutPublishingOrRunningCallbacks()
    {
        var sourcePublished = 0;
        var destinationPublished = 0;
        var callbackRan = false;
        var source = new ItemContainer(StorageType.Normal, 2, _ => sourcePublished++);
        var destination = new ItemContainer(StorageType.Normal, 2, _ => destinationPublished++);
        var item = new TestItem(20, 3);
        Assert.IsTrue(source.Add(item));
        sourcePublished = 0;

        var transaction = new ItemContainerTransaction(source.Mutations, destination.Mutations);
        Assert.IsFalse(transaction.TryExecute(tx =>
        {
            Assert.IsTrue(tx.TryTransfer(source.Mutations, destination.Mutations, item, 3, 0));
            tx.OnCommitted(() => callbackRan = true);
            return false;
        }));

        Assert.AreSame(item, source[0]);
        Assert.AreEqual(3, source[0]!.Count);
        Assert.IsNull(destination[0]);
        Assert.AreEqual(0, sourcePublished);
        Assert.AreEqual(0, destinationPublished);
        Assert.IsFalse(callbackRan);
    }

    [TestMethod]
    public void Transaction_FalseResultRollbackPreservesEnumeratorValidity()
    {
        var sourcePublished = 0;
        var destinationPublished = 0;
        var source = new ItemContainer(StorageType.Normal, 3, _ => sourcePublished++);
        var destination = new ItemContainer(StorageType.Normal, 3, _ => destinationPublished++);
        var sourceItem = new TestItem(20, 5);
        var secondSourceItem = new TestItem(21, 2);
        var destinationItem = new TestItem(30, 4);
        Assert.IsTrue(source.Add(0, sourceItem));
        Assert.IsTrue(source.Add(1, secondSourceItem));
        Assert.IsTrue(destination.Add(0, destinationItem));
        sourcePublished = 0;
        destinationPublished = 0;
        var sourceEnumerator = source.GetEnumerator();
        var destinationEnumerator = destination.GetEnumerator();
        var beforePublishCallbackRan = false;
        var committedCallbackRan = false;
        var transaction = new ItemContainerTransaction(source.Mutations, destination.Mutations);

        var result = transaction.TryExecute(tx =>
        {
            Assert.IsTrue(tx.TryTransfer(source.Mutations, destination.Mutations, sourceItem, sourceItem.Count, 0));
            transaction.OnCommittedBeforePublish(() => beforePublishCallbackRan = true);
            tx.OnCommitted(() => committedCallbackRan = true);
            return false;
        });

        Assert.IsFalse(result);
        Assert.AreSame(sourceItem, source[0]);
        Assert.AreEqual(5, sourceItem.Count);
        Assert.AreSame(secondSourceItem, source[1]);
        Assert.AreEqual(2, secondSourceItem.Count);
        Assert.AreSame(destinationItem, destination[0]);
        Assert.AreEqual(4, destinationItem.Count);
        Assert.IsNull(destination[1]);
        Assert.AreEqual(0, sourcePublished);
        Assert.AreEqual(0, destinationPublished);
        Assert.IsFalse(beforePublishCallbackRan);
        Assert.IsFalse(committedCallbackRan);

        Assert.IsTrue(sourceEnumerator.MoveNext());
        Assert.AreSame(sourceItem, sourceEnumerator.Current);
        Assert.IsTrue(sourceEnumerator.MoveNext());
        Assert.AreSame(secondSourceItem, sourceEnumerator.Current);
        Assert.IsTrue(sourceEnumerator.MoveNext());
        Assert.IsNull(sourceEnumerator.Current);
        Assert.IsFalse(sourceEnumerator.MoveNext());
        Assert.IsTrue(destinationEnumerator.MoveNext());
        Assert.AreSame(destinationItem, destinationEnumerator.Current);
        Assert.IsTrue(destinationEnumerator.MoveNext());
        Assert.IsNull(destinationEnumerator.Current);
        Assert.IsTrue(destinationEnumerator.MoveNext());
        Assert.IsNull(destinationEnumerator.Current);
        Assert.IsFalse(destinationEnumerator.MoveNext());
    }

    [TestMethod]
    public void Transaction_ThrownOperationRollbackPreservesEnumeratorValidity()
    {
        var sourcePublished = 0;
        var destinationPublished = 0;
        var source = new ItemContainer(StorageType.Normal, 2, _ => sourcePublished++);
        var destination = new ItemContainer(StorageType.Normal, 2, _ => destinationPublished++);
        var item = new TestItem(22, 5);
        var destinationItem = new TestItem(23, 3);
        Assert.IsTrue(source.Add(item));
        Assert.IsTrue(destination.Add(destinationItem));
        sourcePublished = 0;
        destinationPublished = 0;
        var sourceEnumerator = source.GetEnumerator();
        var destinationEnumerator = destination.GetEnumerator();
        var beforePublishCallbackRan = false;
        var committedCallbackRan = false;
        var originalException = new System.InvalidOperationException("Abort after staging.");
        var transaction = new ItemContainerTransaction(source.Mutations, destination.Mutations);

        var thrown = Assert.ThrowsExactly<System.InvalidOperationException>(() => transaction.TryExecute(tx =>
        {
            Assert.IsTrue(tx.TryTransfer(source.Mutations, destination.Mutations, item, item.Count, 0));
            transaction.OnCommittedBeforePublish(() => beforePublishCallbackRan = true);
            tx.OnCommitted(() => committedCallbackRan = true);
            throw originalException;
        }));

        Assert.AreSame(originalException, thrown);
        Assert.AreSame(item, source[0]);
        Assert.AreEqual(5, item.Count);
        Assert.AreSame(destinationItem, destination[0]);
        Assert.AreEqual(3, destinationItem.Count);
        Assert.IsNull(destination[1]);
        Assert.AreEqual(0, sourcePublished);
        Assert.AreEqual(0, destinationPublished);
        Assert.IsFalse(beforePublishCallbackRan);
        Assert.IsFalse(committedCallbackRan);
        Assert.IsTrue(sourceEnumerator.MoveNext());
        Assert.AreSame(item, sourceEnumerator.Current);
        Assert.IsTrue(sourceEnumerator.MoveNext());
        Assert.IsNull(sourceEnumerator.Current);
        Assert.IsFalse(sourceEnumerator.MoveNext());
        Assert.IsTrue(destinationEnumerator.MoveNext());
        Assert.AreSame(destinationItem, destinationEnumerator.Current);
        Assert.IsTrue(destinationEnumerator.MoveNext());
        Assert.IsNull(destinationEnumerator.Current);
        Assert.IsFalse(destinationEnumerator.MoveNext());
    }

    [TestMethod]
    public void Transaction_SuccessfulTransferStillInvalidatesExistingEnumerators()
    {
        var source = new ItemContainer(StorageType.Normal, 2);
        var destination = new ItemContainer(StorageType.Normal, 2);
        var item = new TestItem(24, 1);
        Assert.IsTrue(source.Add(item));
        var sourceEnumerator = source.GetEnumerator();
        var destinationEnumerator = destination.GetEnumerator();
        var transaction = new ItemContainerTransaction(source.Mutations, destination.Mutations);

        Assert.IsTrue(transaction.TryExecute(tx => tx.TryTransfer(source.Mutations, destination.Mutations, item, 1, 0)));

        Assert.ThrowsExactly<System.InvalidOperationException>(() => sourceEnumerator.MoveNext());
        Assert.ThrowsExactly<System.InvalidOperationException>(() => destinationEnumerator.MoveNext());
        Assert.IsNull(source[0]);
        Assert.AreSame(item, destination[0]);
    }

    [TestMethod]
    public void Transaction_OnCommittedRunsAfterCommittedBoundaryPublication()
    {
        var publicationOrder = new List<string>();
        var source = new ItemContainer(StorageType.Normal, 2, _ => publicationOrder.Add("source"));
        var destination = new ItemContainer(StorageType.Normal, 2, _ => publicationOrder.Add("destination"));
        var item = new TestItem(21, 1);
        Assert.IsTrue(source.Add(item));
        publicationOrder.Clear();
        var transaction = new ItemContainerTransaction(source.Mutations, destination.Mutations);

        Assert.IsTrue(transaction.TryExecute(tx =>
        {
            Assert.IsTrue(tx.TryTransfer(source.Mutations, destination.Mutations, item, 1, 0));
            tx.OnCommitted(() => publicationOrder.Add("domain"));
            return true;
        }));

        CollectionAssert.AreEqual(new[] { "source", "destination", "domain" }, publicationOrder);
    }

    [TestMethod]
    public void Transaction_PostCommitPublisherFailureDoesNotRollbackAndAttemptsLaterActions()
    {
        var originalException = new InvalidOperationException("Source publisher failed.");
        var sourcePublisherCalls = 0;
        var destinationPublisherCalls = 0;
        var committedCallbackCalls = 0;
        var throwOnSourcePublish = false;
        var source = new ItemContainer(StorageType.Normal, 2, _ =>
        {
            sourcePublisherCalls++;
            if (throwOnSourcePublish) throw originalException;
        });
        var destination = new ItemContainer(StorageType.Normal, 2, _ => destinationPublisherCalls++);
        var item = new TestItem(24, 3);
        Assert.IsTrue(source.Add(item));
        sourcePublisherCalls = 0;
        throwOnSourcePublish = true;
        var transaction = new ItemContainerTransaction(source.Mutations, destination.Mutations);

        var thrown = Assert.ThrowsExactly<InvalidOperationException>(() => transaction.TryExecute(tx =>
        {
            Assert.IsTrue(tx.TryTransfer(source.Mutations, destination.Mutations, item, 3, 0));
            tx.OnCommitted(() => committedCallbackCalls++);
            return true;
        }));

        Assert.AreSame(originalException, thrown);
        Assert.IsNull(source[0]);
        Assert.AreSame(item, destination[0]);
        Assert.AreEqual(1, sourcePublisherCalls);
        Assert.AreEqual(1, destinationPublisherCalls);
        Assert.AreEqual(1, committedCallbackCalls);
    }

    [TestMethod]
    public void Transaction_PostCommitActionsRunInPhaseAndParticipantRegistrationOrder()
    {
        var order = new List<string>();
        var source = new ItemContainer(StorageType.Normal, 2, _ => order.Add("source publisher"));
        var destination = new ItemContainer(StorageType.Normal, 2, _ => order.Add("destination publisher"));
        var transaction = new ItemContainerTransaction(source.Mutations, destination.Mutations);

        Assert.IsTrue(transaction.TryExecute(tx =>
        {
            Assert.IsTrue(tx.TryAddRange(destination.Mutations, [new TestItem(25, 1)]));
            Assert.IsTrue(tx.TryAddRange(source.Mutations, [new TestItem(26, 1)]));
            transaction.OnCommittedBeforePublish(() => order.Add("before"));
            tx.OnCommitted(() => order.Add("committed"));
            return true;
        }));

        CollectionAssert.AreEqual(new[] { "before", "source publisher", "destination publisher", "committed" }, order);
    }

    [TestMethod]
    public void Transaction_PostCommitCallbackFailureStillRunsLaterCallbacks()
    {
        var firstException = new InvalidOperationException("First callback failed.");
        var secondCallbackRan = false;
        var container = new ItemContainer(StorageType.Normal, 2);
        var transaction = new ItemContainerTransaction(container.Mutations);

        var thrown = Assert.ThrowsExactly<InvalidOperationException>(() => transaction.TryExecute(tx =>
        {
            Assert.IsTrue(tx.TryAddRange(container.Mutations, [new TestItem(27, 1)]));
            tx.OnCommitted(() => throw firstException);
            tx.OnCommitted(() => secondCallbackRan = true);
            return true;
        }));

        Assert.AreSame(firstException, thrown);
        Assert.AreEqual(1, container.TakenSlots);
        Assert.IsTrue(secondCallbackRan);
    }

    [TestMethod]
    public void Transaction_MultiplePostCommitFailuresAreAggregatedAfterAllActionsRun()
    {
        var beforeException = new InvalidOperationException("Before publish failed.");
        var publisherException = new InvalidOperationException("Publisher failed.");
        var committedException = new InvalidOperationException("Committed callback failed.");
        var committedCallbackRan = false;
        var container = new ItemContainer(StorageType.Normal, 2, _ => throw publisherException);
        var transaction = new ItemContainerTransaction(container.Mutations);

        var thrown = Assert.ThrowsExactly<AggregateException>(() => transaction.TryExecute(tx =>
        {
            Assert.IsTrue(tx.TryAddRange(container.Mutations, [new TestItem(28, 1)]));
            transaction.OnCommittedBeforePublish(() => throw beforeException);
            tx.OnCommitted(() => throw committedException);
            tx.OnCommitted(() => committedCallbackRan = true);
            return true;
        }));

        StringAssert.StartsWith(thrown.Message, "Multiple post-commit item-container actions failed.");
        CollectionAssert.AreEqual(new Exception[] { beforeException, publisherException, committedException },
            thrown.InnerExceptions.ToArray());
        Assert.AreEqual(1, container.TakenSlots);
        Assert.IsTrue(committedCallbackRan);
    }

    [TestMethod]
    public void Transaction_BeforePublishFailureStillPublishesAndRunsCommittedCallbacks()
    {
        var beforeException = new InvalidOperationException("Before publish failed.");
        var publisherCalls = 0;
        var committedCallbackCalls = 0;
        var container = new ItemContainer(StorageType.Normal, 2, _ => publisherCalls++);
        var transaction = new ItemContainerTransaction(container.Mutations);

        var thrown = Assert.ThrowsExactly<InvalidOperationException>(() => transaction.TryExecute(tx =>
        {
            Assert.IsTrue(tx.TryAddRange(container.Mutations, [new TestItem(29, 1)]));
            transaction.OnCommittedBeforePublish(() => throw beforeException);
            tx.OnCommitted(() => committedCallbackCalls++);
            return true;
        }));

        Assert.AreSame(beforeException, thrown);
        Assert.AreEqual(1, container.TakenSlots);
        Assert.AreEqual(1, publisherCalls);
        Assert.AreEqual(1, committedCallbackCalls);
    }

    [TestMethod]
    public void Transaction_PublicShapeSeparatesExecutionFromStagingAndKeepsChangedSlotsPrivate()
    {
        var container = new ItemContainer(StorageType.Normal, 2);
        var transaction = new ItemContainerTransaction(container.Mutations);
        var contextType = typeof(IItemContainerTransaction);
        var transactionType = typeof(ItemContainerTransaction);

        Assert.IsNull(contextType.GetMethod("TryExecute"));
        Assert.IsNotNull(transactionType.GetMethod("TryExecute"));
        Assert.IsNull(contextType.GetMethod("RecordChangedSlots"));
        Assert.IsNull(contextType.GetMethod("RecordFullChange"));
        Assert.IsNull(contextType.GetMethod("TransferAll"));
        Assert.IsFalse(contextType.GetMethods().Any(method => method.GetParameters()
            .Any(parameter => parameter.ParameterType.IsByRef)));
        Assert.AreEqual(1, transactionType.GetMethods().Count(method => method.Name == "TryAddRange"));
        Assert.AreEqual(1, transactionType.GetMethods().Count(method => method.Name == "TryRemoveExact"));
        Assert.IsFalse(transactionType.GetMethods().Where(method => method.Name is "TryAddRange" or "TryRemoveExact")
            .Any(method => method.GetParameters().Any(parameter => parameter.ParameterType.IsByRef)));

        Assert.IsTrue(transaction.TryExecute(tx =>
        {
            Assert.IsTrue(tx.TryAddRange(container.Mutations, [new TestItem(23, 1)]));
            tx.OnCommitted(() => { });
            return true;
        }));
        Assert.AreEqual(1, container.TakenSlots);
    }

    [TestMethod]
    public void Transaction_TryAddAtIsInternalExactSlotStagingAndLeavesRejectedStateUnchanged()
    {
        var updates = 0;
        var container = new ItemContainer(StorageType.Normal, 2, _ => updates++);
        var existing = new TestItem(20, 1);
        var incoming = new TestItem(21, 1);
        Assert.IsTrue(container.Add(0, existing));
        updates = 0;
        var transaction = new ItemContainerTransaction(container.Mutations);

        Assert.ThrowsExactly<System.InvalidOperationException>(() => transaction.TryAddAt(container.Mutations, 1, incoming));
        Assert.IsTrue(transaction.TryExecute(_ => transaction.TryAddAt(container.Mutations, 1, incoming)));

        Assert.AreSame(existing, container[0]);
        Assert.AreSame(incoming, container[1]);
        Assert.AreEqual(1, updates);
        Assert.IsFalse(typeof(IItemContainerTransaction).GetMethod("TryAddAt") is not null);
        var method = typeof(ItemContainerTransaction).GetMethod("TryAddAt", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);
        Assert.IsTrue(method!.IsAssembly);

        var rejected = new ItemContainerTransaction(container.Mutations);
        Assert.IsFalse(rejected.TryExecute(_ => rejected.TryAddAt(container.Mutations, 0, new TestItem(22, 1))));

        Assert.AreSame(existing, container[0]);
        Assert.AreSame(incoming, container[1]);
        Assert.AreEqual(1, updates);
    }

    [TestMethod]
    public void MutationBoundary_DoesNotExposeStorageOrLocks()
    {
        var boundaryType = typeof(ItemContainerMutationBoundary);
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        Assert.IsNull(boundaryType.GetProperty("Storage", flags));
        Assert.IsNull(boundaryType.GetProperty("MutationLock", flags));
    }

    [TestMethod]
    public void Transaction_InterfaceTypedParticipantsRollbackAndSuppressCallbacksWhenOperationThrows()
    {
        var sourcePublished = 0;
        var destinationPublished = 0;
        var callbackRan = false;
        var source = new ItemContainer(StorageType.Normal, 2, _ => sourcePublished++);
        var destination = new ItemContainer(StorageType.Normal, 2, _ => destinationPublished++);
        var item = new TestItem(22, 5);
        Assert.IsTrue(source.Add(item));
        sourcePublished = 0;
        var transaction = new ItemContainerTransaction(source.Mutations, destination.Mutations);

        Assert.ThrowsExactly<System.InvalidOperationException>(() => transaction.TryExecute(tx =>
        {
            Assert.IsTrue(tx.TryTransfer(source.Mutations, destination.Mutations, item, 5, 0));
            tx.OnCommitted(() => callbackRan = true);
            throw new System.InvalidOperationException("Abort staged transaction.");
        }));

        Assert.AreSame(item, source[0]);
        Assert.AreEqual(5, source[0]!.Count);
        Assert.IsNull(destination[0]);
        Assert.AreEqual(0, sourcePublished);
        Assert.AreEqual(0, destinationPublished);
        Assert.IsFalse(callbackRan);
    }

    [TestMethod]
    public void OrdinaryDomainInterfacesTransferBetweenInterfaceTypedContainers()
    {
        IItemContainer inventoryItems = new ItemContainer(StorageType.Normal, 2);
        IItemContainer bankItems = new ItemContainer(StorageType.AlwaysStack, 2);
        var inventory = Substitute.For<IInventoryContainer>();
        inventory.Items.Returns(inventoryItems);
        var bank = Substitute.For<IBankContainer>();
        bank.Items.Returns(bankItems);
        var item = new TestItem(30, 4);
        Assert.IsTrue(inventory.Items.Add(item));

        Assert.IsTrue(inventory.Items.Mutations.TryTransferTo(bank.Items.Mutations, item, 4, 0));

        Assert.IsNull(inventory.Items[0]);
        Assert.AreSame(item, bank.Items[0]);
    }

    [TestMethod]
    public void DomainAndInfrastructureInterfacesDoNotExposeConcreteContainerImplementations()
    {
        var ordinaryContainers = new[]
        {
            typeof(IInventoryContainer), typeof(IBankContainer), typeof(IRewardContainer),
            typeof(IFamiliarInventoryContainer), typeof(IShopStockContainer)
        };
        foreach (var contract in ordinaryContainers)
        {
            Assert.AreEqual(typeof(IItemContainer), contract.GetProperty("Items")!.PropertyType, contract.Name);
        }

        Assert.AreEqual(typeof(IItemContainerMutationBoundary), typeof(IItemContainer).GetProperty("Mutations")!.PropertyType);
        var domainContracts = ordinaryContainers.Concat([typeof(IEquipmentContainer), typeof(IMoneyPouchContainer)]);
        foreach (var contract in domainContracts)
        {
            foreach (var method in contract.GetMethods())
            {
                Assert.IsFalse(method.GetParameters().Any(parameter =>
                    parameter.ParameterType == typeof(ItemContainer) ||
                    parameter.ParameterType == typeof(ItemContainerMutationBoundary) ||
                    parameter.ParameterType == typeof(ItemContainerTransaction) ||
                    parameter.ParameterType == typeof(ItemContainerStorage)), contract.Name + "." + method.Name);
                Assert.IsFalse(method.ReturnType == typeof(ItemContainer) ||
                    method.ReturnType == typeof(ItemContainerMutationBoundary) ||
                    method.ReturnType == typeof(ItemContainerTransaction) ||
                    method.ReturnType == typeof(ItemContainerStorage), contract.Name + "." + method.Name);
            }
        }

        Assert.IsFalse(typeof(IEquipmentContainer).GetProperty("Items") is not null);
        Assert.IsFalse(typeof(IEquipmentContainer).GetProperty("Mutations") is not null);
        Assert.IsFalse(typeof(IMoneyPouchContainer).GetProperty("Items") is not null);
        Assert.IsFalse(typeof(IMoneyPouchContainer).GetProperty("Mutations") is not null);
    }

    private sealed class TestItem : IItem
    {
        public int Id { get; }
        public int Count { get; set; }
        public string Name => $"Item {Id}";
        public IItemDefinition ItemDefinition { get; }
        public IEquipmentDefinition EquipmentDefinition { get; } = Substitute.For<IEquipmentDefinition>();
        public IItemScript ItemScript { get; }
        public IEquipmentScript EquipmentScript { get; } = Substitute.For<IEquipmentScript>();
        public long[] ExtraData => [];

        public TestItem(int id, int count)
        {
            Id = id;
            Count = count;
            ItemDefinition = Substitute.For<IItemDefinition>();
            ItemDefinition.Stackable.Returns(true);
            ItemDefinition.Noted.Returns(false);
            ItemScript = Substitute.For<IItemScript>();
            ItemScript.CanStackItem(Arg.Any<IItem>(), Arg.Any<IItem>(), Arg.Any<bool>())
                .Returns(call => call.ArgAt<IItem>(0).Id == call.ArgAt<IItem>(1).Id);
        }

        public IItem Clone() => Clone(1);
        public IItem Clone(int newCount) => new TestItem(Id, newCount);
        public bool Equals(IItem otherItem, bool ignoreCount = true) =>
            otherItem != null && Id == otherItem.Id && (ignoreCount || Count == otherItem.Count);
        public string? SerializeExtraData() => null;
    }
}
