using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Items;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Abstractions.Tests.Collections;

[TestClass]
public sealed class ItemContainerMutationBoundaryTests
{
    [TestMethod]
    public void TryTransferStorage_MovesExactQuantityAndReturnsChangedSlots()
    {
        var source = new ItemContainer(StorageType.Normal, 2);
        var destination = new ItemContainer(StorageType.Normal, 2);
        var item = new TestItem(10, 7);
        Assert.IsTrue(source.Add(item));

        Assert.IsTrue(source.Mutations.TryTransferToStorage(destination.Mutations, item, 7, 0, -1, null,
            out var sourceSlots, out var destinationSlots));

        Assert.AreEqual(0, source.TakenSlots);
        Assert.AreSame(item, destination[0]);
        Assert.AreEqual(7, destination[0]!.Count);
        CollectionAssert.AreEquivalent(new[] { 0 }, sourceSlots.ToArray());
        CollectionAssert.AreEquivalent(new[] { 0 }, destinationSlots.ToArray());
    }

    [TestMethod]
    public void TryTransferStorage_FailureLeavesBothStoragesUnchanged()
    {
        var source = new ItemContainer(StorageType.Normal, 2);
        var destination = new ItemContainer(StorageType.Normal, 1);
        var item = new TestItem(10, 2);
        var blockingItem = new TestItem(11, 1);
        Assert.IsTrue(source.Add(item));
        Assert.IsTrue(destination.Add(blockingItem));

        Assert.IsFalse(source.Mutations.TryTransferToStorage(destination.Mutations, item, 2, 0, -1, null,
            out var sourceSlots, out var destinationSlots));

        Assert.AreSame(item, source[0]);
        Assert.AreEqual(2, source[0]!.Count);
        Assert.AreSame(blockingItem, destination[0]);
        Assert.AreEqual(0, sourceSlots.Count);
        Assert.AreEqual(0, destinationSlots.Count);
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
