using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Model.Items;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Model.Items;

[TestClass]
public sealed class GroundItemPickupTests
{
    [TestMethod]
    public void TakeItem_WhenSameGroundItemIsQueuedTwice_GrantsItOnce()
    {
        var item = Substitute.For<IItem>();
        var clone = Substitute.For<IItem>();
        var groundItem = Substitute.For<IGroundItem>();
        var inventory = Substitute.For<IInventoryContainer>();
        var character = Substitute.For<ICharacter>();
        groundItem.ItemOnGround.Returns(item);
        groundItem.Despawn().Returns(true, false);
        item.Clone().Returns(clone);
        character.Inventory.Returns(inventory);
        inventory.HasSpaceFor(item).Returns(true);
        inventory.Add(clone).Returns(true);

        var script = new DefaultItemScript();

        var firstResult = script.TakeItem(groundItem, character);
        var secondResult = script.TakeItem(groundItem, character);

        Assert.IsTrue(firstResult);
        Assert.IsFalse(secondResult);
        groundItem.Received(2).Despawn();
        inventory.Received(1).Add(clone);
    }

    [TestMethod]
    public void TakeItem_WhenInventoryIsFull_LeavesGroundItemAvailable()
    {
        var item = Substitute.For<IItem>();
        var groundItem = Substitute.For<IGroundItem>();
        var inventory = Substitute.For<IInventoryContainer>();
        var character = Substitute.For<ICharacter>();
        groundItem.ItemOnGround.Returns(item);
        character.Inventory.Returns(inventory);
        inventory.HasSpaceFor(item).Returns(false);

        var result = new DefaultItemScript().TakeItem(groundItem, character);

        Assert.IsFalse(result);
        groundItem.DidNotReceive().Despawn();
        inventory.DidNotReceive().Add(Arg.Any<IItem>());
    }
}
