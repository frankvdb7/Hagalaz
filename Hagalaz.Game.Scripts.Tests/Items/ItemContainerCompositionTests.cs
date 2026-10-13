using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Items;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Items;

[TestClass]
public sealed class ItemContainerCompositionTests
{
    [TestMethod]
    public void Add_TracksChangedSlotsForTheOwningDomainToPublish()
    {
        var item = ComposedTestContainer.CreateTestItem(10, 2);
        var container = new ItemContainer(StorageType.Normal, 2);

        Assert.IsTrue(container.Add(item));

        Assert.AreSame(item, container[0]);
        CollectionAssert.AreEquivalent(new[] { 0 }, container.ChangeTracker.GetSlots()!.ToArray());
    }

    [TestMethod]
    public void AddRange_FailureLeavesStorageUnchangedAndDoesNotPublish()
    {
        var first = ComposedTestContainer.CreateTestItem(10, 1);
        var second = ComposedTestContainer.CreateTestItem(11, 1);
        var container = new ItemContainer(StorageType.Normal, 1);

        Assert.IsFalse(container.AddRange([first, second]));

        Assert.AreEqual(0, container.TakenSlots);
        Assert.IsNull(container[0]);
        Assert.IsFalse(container.ChangeTracker.HasChanges);
    }

}

