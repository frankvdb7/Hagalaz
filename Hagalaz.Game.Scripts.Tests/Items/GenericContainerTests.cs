using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Items;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Items;

[TestClass]
public sealed class GenericContainerTests
{
    [TestMethod]
    public void Add_PublishesChangedSlotsAfterTheStorageCommit()
    {
        var item = new TestItem(10, 2);
        var callbackCount = 0;
        HashSet<int>? publishedSlots = null;
        GenericContainer? container = null;
        container = new GenericContainer(StorageType.Normal, 2, slots =>
        {
            callbackCount++;
            publishedSlots = slots;
            Assert.AreSame(item, container[0]);
        });

        Assert.IsTrue(container.Add(item));

        Assert.AreEqual(1, callbackCount);
        CollectionAssert.AreEquivalent(new[] { 0 }, publishedSlots!.ToArray());
    }

    [TestMethod]
    public void AddRange_FailureLeavesStorageUnchangedAndDoesNotPublish()
    {
        var first = new TestItem(10, 1);
        var second = new TestItem(11, 1);
        var callbackCount = 0;
        var container = new GenericContainer(StorageType.Normal, 1, _ => callbackCount++);

        Assert.IsFalse(container.AddRange([first, second]));

        Assert.AreEqual(0, container.TakenSlots);
        Assert.IsNull(container[0]);
        Assert.AreEqual(0, callbackCount);
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
