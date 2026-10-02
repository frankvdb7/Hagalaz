using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Items;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Items;

[TestClass]
public sealed class ItemContainerCompositionTests
{
    [TestMethod]
    public void Add_PublishesChangedSlotsAfterTheStorageCommit()
    {
        var item = ComposedTestContainer.CreateTestItem(10, 2);
        var callbackCount = 0;
        HashSet<int>? publishedSlots = null;
        ItemContainer? container = null;
        container = new ItemContainer(StorageType.Normal, 2, slots =>
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
        var first = ComposedTestContainer.CreateTestItem(10, 1);
        var second = ComposedTestContainer.CreateTestItem(11, 1);
        var callbackCount = 0;
        var container = new ItemContainer(StorageType.Normal, 1, _ => callbackCount++);

        Assert.IsFalse(container.AddRange([first, second]));

        Assert.AreEqual(0, container.TakenSlots);
        Assert.IsNull(container[0]);
        Assert.AreEqual(0, callbackCount);
    }

}

