using AutoMapper;
using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Model.Items;
using Hagalaz.Services.GameWorld.Model.Items;
using Hagalaz.Services.GameWorld.Model.Maps.Regions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Model.Items;

[TestClass]
public sealed class GroundItemPickupTests
{
    [TestMethod]
    public void TakeItem_WhenGroundItemReferenceIsStale_DoesNotGrantAgain()
    {
        var regionService = Substitute.For<IMapRegionService>();
        var region = CreateRegion(regionService);
        var item = Substitute.For<IItem>();
        var groundItem = new GroundItem(item, Location.Create(10, 10), null, 0, 100, regionService);
        var inventory = Substitute.For<IInventoryContainer>();
        var character = Substitute.For<ICharacter>();
        var clone = Substitute.For<IItem>();
        item.Clone().Returns(clone);
        character.Inventory.Returns(inventory);
        inventory.HasSpaceFor(item).Returns(true);
        inventory.Add(clone).Returns(true);
        regionService.GetOrCreateMapRegion(groundItem.Location.RegionId, groundItem.Location.Dimension, false).Returns(region);
        region.Add(groundItem);

        var script = new DefaultItemScript();

        var firstResult = script.TakeItem(groundItem, character);
        var secondResult = script.TakeItem(groundItem, character);

        Assert.IsTrue(firstResult);
        Assert.IsFalse(secondResult);
        inventory.Received(1).Add(clone);
        Assert.IsEmpty(region.FindAllGroundItems());
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

    private static MapRegion CreateRegion(IMapRegionService regionService)
    {
        var mapper = new MapperConfiguration(cfg => { }, LoggerFactory.Create(_ => { })).CreateMapper();
        return new MapRegion(
            Location.Create(0, 0),
            new int[4],
            Substitute.For<INpcService>(),
            regionService,
            Substitute.For<IGameObjectBuilder>(),
            Substitute.For<IGroundItemBuilder>(),
            mapper);
    }
}
