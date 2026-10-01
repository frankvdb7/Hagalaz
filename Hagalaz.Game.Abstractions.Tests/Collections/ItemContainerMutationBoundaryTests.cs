using System.Collections.Generic;
using System.Linq;
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
    public void Transaction_ContextExposesStagingButNotExecutionOrChangeBookkeeping()
    {
        var container = new ItemContainer(StorageType.Normal, 2);
        var transaction = new ItemContainerTransaction(container.Mutations);
        var contextType = typeof(IItemContainerTransaction);

        Assert.IsNull(contextType.GetMethod("TryExecute"));
        Assert.IsNull(contextType.GetMethod("RecordChangedSlots"));
        Assert.IsNull(contextType.GetMethod("RecordFullChange"));
        Assert.IsNull(contextType.GetMethod("TransferAll"));
        Assert.IsFalse(contextType.GetMethods().Any(method => method.GetParameters()
            .Any(parameter => parameter.ParameterType.IsByRef)));

        Assert.IsTrue(transaction.TryExecute(tx =>
        {
            Assert.IsTrue(tx.TryAddRange(container.Mutations, [new TestItem(23, 1)]));
            tx.OnCommitted(() => { });
            return true;
        }));
        Assert.AreEqual(1, container.TakenSlots);
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
