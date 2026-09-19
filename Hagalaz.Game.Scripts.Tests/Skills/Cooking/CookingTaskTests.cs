using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Scripts.Skills.Cooking;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Skills.Cooking;

[TestClass]
public sealed class CookingTaskTests
{
    [TestMethod]
    public void Tick_WhenCookingObjectWasRemoved_CancelsWithoutCookingOutput()
    {
        var (task, performer, cookingObject, region, mapRegionService, location) = CreateTask();
        region.FindGameObjects(location.RegionLocalX, location.RegionLocalY, location.Z)
            .Returns(new[] { cookingObject });

        task.Tick();

        region.FindGameObjects(location.RegionLocalX, location.RegionLocalY, location.Z)
            .Returns(Array.Empty<IGameObject>());
        task.Tick();

        Assert.IsTrue(task.IsCancelled);
        performer.Received(1).QueueAnimation(Arg.Any<IAnimation>());
        performer.Received(1).SendChatMessage(Arg.Any<string>());
        mapRegionService.Received(2).FindMapRegion(location.RegionId, location.Dimension);
    }

    [TestMethod]
    public void Tick_WhenReplacementOccupiesCookingLocation_CancelsForTheOldObject()
    {
        var (task, _, cookingObject, region, _, location) = CreateTask();
        var replacement = Substitute.For<IGameObject>();
        region.FindGameObjects(location.RegionLocalX, location.RegionLocalY, location.Z)
            .Returns(new[] { cookingObject });

        task.Tick();

        region.FindGameObjects(location.RegionLocalX, location.RegionLocalY, location.Z)
            .Returns(new[] { replacement });
        task.Tick();

        Assert.IsTrue(task.IsCancelled);
    }

    [TestMethod]
    public void Tick_WhenCookingObjectIsDisabled_CancelsWithoutResolvingRegion()
    {
        var (task, performer, cookingObject, _, mapRegionService, _) = CreateTask();
        cookingObject.IsDisabled.Returns(true);

        task.Tick();

        Assert.IsTrue(task.IsCancelled);
        mapRegionService.DidNotReceive().FindMapRegion(Arg.Any<int>(), Arg.Any<int>());
        performer.DidNotReceive().QueueAnimation(Arg.Any<IAnimation>());
        performer.DidNotReceive().SendChatMessage(Arg.Any<string>());
    }

    private static (
        CookingTask Task,
        ICharacter Performer,
        IGameObject CookingObject,
        IMapRegion Region,
        IMapRegionService MapRegionService,
        ILocation Location) CreateTask()
    {
        var performer = Substitute.For<ICharacter>();
        var context = Substitute.For<ICharacterContext>();
        var contextAccessor = Substitute.For<ICharacterContextAccessor>();
        var itemService = Substitute.For<IItemService>();
        var itemBuilder = Substitute.For<IItemBuilder>();
        var mapRegionService = Substitute.For<IMapRegionService>();
        var region = Substitute.For<IMapRegion>();
        var cookingObject = Substitute.For<IGameObject>();
        var location = Location.Create(3200, 3200, 0, 0);

        context.Character.Returns(performer);
        contextAccessor.Context.Returns(context);
        cookingObject.Location.Returns(location);
        cookingObject.IsDisabled.Returns(false);
        mapRegionService.FindMapRegion(location.RegionId, location.Dimension).Returns(region);
        itemService.FindItemDefinitionById(1).Returns(Substitute.For<IItemDefinition>());

        var task = new CookingTask(contextAccessor, itemService, itemBuilder, mapRegionService)
        {
            GameObject = cookingObject,
            RawDto = new RawFoodDto
            {
                ItemId = 1,
                CookedItemId = 2,
                BurntItemId = 3,
                RequiredLevel = 1,
                StopBurningLevel = 1,
                Experience = 1
            },
            TotalCookCount = 1
        };

        return (task, performer, cookingObject, region, mapRegionService, location);
    }
}
