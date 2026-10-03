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
    public void Transaction_InterfaceTypedParticipantsRollbackWithoutPublishing()
    {
        var sourcePublished = 0;
        var destinationPublished = 0;
        var source = new ItemContainer(StorageType.Normal, 2, _ => sourcePublished++);
        var destination = new ItemContainer(StorageType.Normal, 2, _ => destinationPublished++);
        var item = new TestItem(20, 3);
        Assert.IsTrue(source.Add(item));
        sourcePublished = 0;

        var transaction = new ItemContainerTransaction(source.Mutations, destination.Mutations);
        Assert.IsFalse(transaction.TryCommit(tx =>
        {
            Assert.IsTrue(tx.TryTransfer(source.Mutations, destination.Mutations, item, 3, 0));
            return false;
        }));
        Assert.IsFalse(transaction.Committed);
        Assert.IsFalse(transaction.PublishChanges());

        Assert.AreSame(item, source[0]);
        Assert.AreEqual(3, source[0]!.Count);
        Assert.IsNull(destination[0]);
        Assert.AreEqual(0, sourcePublished);
        Assert.AreEqual(0, destinationPublished);
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
        var transaction = new ItemContainerTransaction(source.Mutations, destination.Mutations);

        var result = transaction.TryCommit(tx =>
        {
            Assert.IsTrue(tx.TryTransfer(source.Mutations, destination.Mutations, sourceItem, sourceItem.Count, 0));
            return false;
        });

        Assert.IsFalse(result);
        Assert.IsFalse(transaction.Committed);
        Assert.IsFalse(transaction.PublishChanges());
        Assert.AreSame(sourceItem, source[0]);
        Assert.AreEqual(5, sourceItem.Count);
        Assert.AreSame(secondSourceItem, source[1]);
        Assert.AreEqual(2, secondSourceItem.Count);
        Assert.AreSame(destinationItem, destination[0]);
        Assert.AreEqual(4, destinationItem.Count);
        Assert.IsNull(destination[1]);
        Assert.AreEqual(0, sourcePublished);
        Assert.AreEqual(0, destinationPublished);

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
        var originalException = new System.InvalidOperationException("Abort after staging.");
        var transaction = new ItemContainerTransaction(source.Mutations, destination.Mutations);

        var thrown = Assert.ThrowsExactly<System.InvalidOperationException>(() => transaction.TryCommit(tx =>
        {
            Assert.IsTrue(tx.TryTransfer(source.Mutations, destination.Mutations, item, item.Count, 0));
            throw originalException;
        }));

        Assert.AreSame(originalException, thrown);
        Assert.IsFalse(transaction.Committed);
        Assert.IsFalse(transaction.PublishChanges());
        Assert.AreSame(item, source[0]);
        Assert.AreEqual(5, item.Count);
        Assert.AreSame(destinationItem, destination[0]);
        Assert.AreEqual(3, destinationItem.Count);
        Assert.IsNull(destination[1]);
        Assert.AreEqual(0, sourcePublished);
        Assert.AreEqual(0, destinationPublished);
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

        Assert.IsTrue(transaction.TryCommit(tx => tx.TryTransfer(source.Mutations, destination.Mutations, item, 1, 0)));

        Assert.ThrowsExactly<System.InvalidOperationException>(() => sourceEnumerator.MoveNext());
        Assert.ThrowsExactly<System.InvalidOperationException>(() => destinationEnumerator.MoveNext());
        Assert.IsNull(source[0]);
        Assert.AreSame(item, destination[0]);
    }

    [TestMethod]
    public void Transaction_EmptyRangeCommitsWithoutPublishingOrInvalidatingEnumerator()
    {
        var publications = 0;
        var container = new ItemContainer(StorageType.Normal, 2, _ => publications++);
        var existing = new TestItem(31, 2);
        Assert.IsTrue(container.Add(existing));
        publications = 0;
        var enumerator = container.GetEnumerator();
        var transaction = new ItemContainerTransaction(container.Mutations);

        var result = transaction.TryCommit(tx =>
        {
            Assert.IsTrue(tx.TryAddRange(container.Mutations, System.Array.Empty<IItem?>()));
            return true;
        });

        Assert.IsTrue(result);
        Assert.IsTrue(transaction.Committed);
        Assert.IsTrue(transaction.PublishChanges());
        Assert.AreEqual(0, publications);
        Assert.AreSame(existing, container[0]);
        Assert.AreEqual(2, existing.Count);
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreSame(existing, enumerator.Current);
    }

    [TestMethod]
    public void Transaction_RealRangeMutationInvalidatesEnumeratorAndPublishesExplicitly()
    {
        var publications = 0;
        var container = new ItemContainer(StorageType.Normal, 2, _ => publications++);
        var enumerator = container.GetEnumerator();
        var transaction = new ItemContainerTransaction(container.Mutations);

        Assert.IsTrue(transaction.TryCommit(tx =>
        {
            Assert.IsTrue(tx.TryAddRange(container.Mutations, [new TestItem(32, 1)]));
            return true;
        }));

        Assert.AreEqual(0, publications);
        Assert.IsTrue(transaction.PublishChanges());
        Assert.AreEqual(1, publications);
        Assert.ThrowsExactly<System.InvalidOperationException>(() => enumerator.MoveNext());
    }

    [TestMethod]
    public void Transaction_CommitAndPublicationAreSeparateAndPublishOnceInParticipantOrder()
    {
        var order = new List<string>();
        var source = new ItemContainer(StorageType.Normal, 2, _ => order.Add("source"));
        var destination = new ItemContainer(StorageType.Normal, 2, _ => order.Add("destination"));
        var item = new TestItem(21, 1);
        Assert.IsTrue(source.Add(item));
        order.Clear();
        var transaction = new ItemContainerTransaction(source.Mutations, destination.Mutations);

        Assert.IsFalse(transaction.Committed);
        Assert.IsFalse(transaction.PublishChanges());
        Assert.IsTrue(transaction.TryCommit(tx => tx.TryTransfer(source.Mutations, destination.Mutations, item, 1, 0)));
        Assert.IsTrue(transaction.Committed);
        Assert.IsNull(source[0]);
        Assert.AreSame(item, destination[0]);
        Assert.AreEqual(0, order.Count);

        Assert.IsTrue(transaction.PublishChanges());
        CollectionAssert.AreEqual(new[] { "source", "destination" }, order);
        Assert.IsFalse(transaction.PublishChanges());
        CollectionAssert.AreEqual(new[] { "source", "destination" }, order);
    }

    [TestMethod]
    public void Transaction_PublisherFailureKeepsCommittedStorageAndStopsLaterPublishers()
    {
        var publicationException = new InvalidOperationException("Source publisher failed.");
        var sourceCalls = 0;
        var destinationCalls = 0;
        var failPublication = false;
        var source = new ItemContainer(StorageType.Normal, 2, _ =>
        {
            sourceCalls++;
            if (failPublication) throw publicationException;
        });
        var destination = new ItemContainer(StorageType.Normal, 2, _ => destinationCalls++);
        var item = new TestItem(24, 3);
        Assert.IsTrue(source.Add(item));
        sourceCalls = 0;
        failPublication = true;
        var transaction = new ItemContainerTransaction(source.Mutations, destination.Mutations);
        Assert.IsTrue(transaction.TryCommit(tx => tx.TryTransfer(source.Mutations, destination.Mutations, item, 3, 0)));

        var thrown = Assert.ThrowsExactly<InvalidOperationException>(() => transaction.PublishChanges());

        Assert.AreSame(publicationException, thrown);
        Assert.IsTrue(transaction.Committed);
        Assert.IsNull(source[0]);
        Assert.AreSame(item, destination[0]);
        Assert.AreEqual(1, sourceCalls);
        Assert.AreEqual(0, destinationCalls);
        Assert.IsFalse(transaction.PublishChanges());
        Assert.AreEqual(1, sourceCalls);
        Assert.AreEqual(0, destinationCalls);
    }

    [TestMethod]
    public void Transaction_LaterPublisherFailurePreservesEarlierPublicationAndCommittedStorage()
    {
        var secondFailure = new InvalidOperationException("Second publisher failed.");
        var calls = new List<string>();
        var first = new ItemContainer(StorageType.Normal, 2, _ => calls.Add("first"));
        var second = new ItemContainer(StorageType.Normal, 2, _ =>
        {
            calls.Add("second");
            throw secondFailure;
        });
        var transaction = new ItemContainerTransaction(first.Mutations, second.Mutations);
        Assert.IsTrue(transaction.TryCommit(tx =>
            tx.TryAddRange(first.Mutations, [new TestItem(25, 1)]) &&
            tx.TryAddRange(second.Mutations, [new TestItem(26, 1)])));

        var thrown = Assert.ThrowsExactly<InvalidOperationException>(() => transaction.PublishChanges());

        CollectionAssert.AreEqual(new[] { "first", "second" }, calls);
        Assert.AreSame(secondFailure, thrown);
        Assert.IsTrue(transaction.Committed);
        Assert.AreEqual(1, first.TakenSlots);
        Assert.AreEqual(1, second.TakenSlots);
    }

    [TestMethod]
    public void Transaction_PublicShapeSeparatesExecutionFromStagingAndKeepsChangedSlotsPrivate()
    {
        var container = new ItemContainer(StorageType.Normal, 2);
        var transaction = new ItemContainerTransaction(container.Mutations);
        var contextType = typeof(IItemContainerTransaction);
        var transactionType = typeof(ItemContainerTransaction);

        Assert.IsNull(contextType.GetMethod("TryCommit"));
        Assert.AreEqual(1, transactionType.GetMethods().Count(method => method.Name == "TryCommit"));
        Assert.AreEqual(1, transactionType.GetMethods().Count(method => method.Name == "PublishChanges"));
        Assert.IsNull(contextType.GetMethod("OnCommitted"));
        Assert.IsNull(transactionType.GetMethod("OnCommitted", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        Assert.IsNull(transactionType.GetMethod("OnCommittedBeforePublish", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        Assert.IsNull(contextType.GetMethod("RecordChangedSlots"));
        Assert.IsNull(contextType.GetMethod("RecordFullChange"));
        Assert.IsNull(contextType.GetMethod("TransferAll"));
        Assert.IsFalse(contextType.GetMethods().Any(method => method.GetParameters()
            .Any(parameter => parameter.ParameterType.IsByRef)));
        Assert.AreEqual(1, transactionType.GetMethods().Count(method => method.Name == "TryAddRange"));
        Assert.AreEqual(1, transactionType.GetMethods().Count(method => method.Name == "TryRemoveExact"));
        Assert.IsFalse(transactionType.GetMethods().Where(method => method.Name is "TryAddRange" or "TryRemoveExact")
            .Any(method => method.GetParameters().Any(parameter => parameter.ParameterType.IsByRef)));

        Assert.IsTrue(transaction.TryCommit(tx =>
        {
            Assert.IsTrue(tx.TryAddRange(container.Mutations, [new TestItem(23, 1)]));
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
        Assert.IsTrue(transaction.TryCommit(_ => transaction.TryAddAt(container.Mutations, 1, incoming)));
        Assert.IsTrue(transaction.PublishChanges());

        Assert.AreSame(existing, container[0]);
        Assert.AreSame(incoming, container[1]);
        Assert.AreEqual(1, updates);
        Assert.IsFalse(typeof(IItemContainerTransaction).GetMethod("TryAddAt") is not null);
        var method = typeof(ItemContainerTransaction).GetMethod("TryAddAt", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);
        Assert.IsTrue(method!.IsAssembly);

        var rejected = new ItemContainerTransaction(container.Mutations);
        Assert.IsFalse(rejected.TryCommit(_ => rejected.TryAddAt(container.Mutations, 0, new TestItem(22, 1))));

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
    public void ItemContainer_KeepsRawStorageAndStateHelpersInternal()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var type = typeof(ItemContainer);

        Assert.IsNull(type.GetProperty("Storage", flags));
        foreach (var name in new[] { "RestoreItems", "SnapshotItems", "ReplaceState", "CountToResetTo", "ExecuteUnderMutationLock" })
        {
            var member = (System.Reflection.MemberInfo?)type.GetMethod(name, flags) ?? type.GetProperty(name, flags);
            Assert.IsNotNull(member, name);
            if (member is System.Reflection.MethodInfo method)
            {
                Assert.IsTrue(method.IsAssembly, name);
                Assert.IsFalse(method.IsPublic, name);
            }
            else
            {
                var getter = ((System.Reflection.PropertyInfo)member!).GetGetMethod(true)!;
                Assert.IsTrue(getter.IsAssembly, name);
                Assert.IsFalse(getter.IsPublic, name);
            }
        }
    }

    [TestMethod]
    public void Transaction_InterfaceTypedParticipantsRollbackWithoutPublishingWhenOperationThrows()
    {
        var sourcePublished = 0;
        var destinationPublished = 0;
        var source = new ItemContainer(StorageType.Normal, 2, _ => sourcePublished++);
        var destination = new ItemContainer(StorageType.Normal, 2, _ => destinationPublished++);
        var item = new TestItem(22, 5);
        Assert.IsTrue(source.Add(item));
        sourcePublished = 0;
        var transaction = new ItemContainerTransaction(source.Mutations, destination.Mutations);

        Assert.ThrowsExactly<System.InvalidOperationException>(() => transaction.TryCommit(tx =>
        {
            Assert.IsTrue(tx.TryTransfer(source.Mutations, destination.Mutations, item, 5, 0));
            throw new System.InvalidOperationException("Abort staged transaction.");
        }));
        Assert.IsFalse(transaction.Committed);
        Assert.IsFalse(transaction.PublishChanges());

        Assert.AreSame(item, source[0]);
        Assert.AreEqual(5, source[0]!.Count);
        Assert.IsNull(destination[0]);
        Assert.AreEqual(0, sourcePublished);
        Assert.AreEqual(0, destinationPublished);
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
        Assert.AreEqual(typeof(IMoneyPouchMutationBoundary), typeof(IMoneyPouchContainer).GetProperty("Mutations")!.PropertyType);
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
