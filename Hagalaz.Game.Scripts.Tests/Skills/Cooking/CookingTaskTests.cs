using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Scripts.Skills.Cooking;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Skills.Cooking;

[TestClass]
public sealed class CookingTaskTests
{
    [TestMethod]
    [DoNotParallelize]
    public void Tick_RecordsOneCompletionPerCookedItem()
    {
        var measurements = new List<(long Value, string? Unit, KeyValuePair<string, object?>[] Tags)>();
        using var listener = SkillCompletionListener.Start((instrument, value, tags, _) =>
            measurements.Add((value, instrument.Unit, tags.ToArray())));
        var (task, performer, cookingObject, region, _, location) = CreateTask();
        region.FindGameObjects(location.RegionLocalX, location.RegionLocalY, location.Z)
            .Returns(new[] { cookingObject });
        var firstRawItem = Substitute.For<IItem>();
        var secondRawItem = Substitute.For<IItem>();
        performer.Inventory.Items.GetById(task.RawDto.ItemId).Returns(firstRawItem, secondRawItem);
        performer.Inventory.Items.GetInstanceSlot(firstRawItem).Returns(0);
        performer.Inventory.Items.GetInstanceSlot(secondRawItem).Returns(1);
        performer.Statistics.GetSkillLevel(StatisticsConstants.Cooking).Returns(task.RawDto.StopBurningLevel);
        task.TotalCookCount = 2;

        task.Tick();
        task.Tick();
        Assert.IsEmpty(measurements);

        task.Tick();
        performer.Inventory.Received(1).ReplaceItem(0, Arg.Is<IItem>(item => item != null && item.Id == task.RawDto.CookedItemId));
        Assert.HasCount(1, measurements);

        for (var tick = 4; tick <= 12; tick++)
        {
            task.Tick();
        }

        Assert.IsTrue(task.IsCancelled);
        performer.Inventory.Received(1).ReplaceItem(1, Arg.Is<IItem>(item => item != null && item.Id == task.RawDto.CookedItemId));
        Assert.HasCount(2, measurements);
        foreach (var measurement in measurements)
        {
            Assert.AreEqual(1L, measurement.Value);
            Assert.AreEqual("{action}", measurement.Unit);
            Assert.AreEqual(new KeyValuePair<string, object?>("skill", "cooking"), measurement.Tags.Single());
        }
    }

    [TestMethod]
    [DataRow(true, DisplayName = "Cooking interrupted before output")]
    [DataRow(false, DisplayName = "Cooking missing raw input")]
    [DoNotParallelize]
    public void Tick_InterruptedOrMissingInput_DoesNotRecordCompletion(bool interrupted)
    {
        var measurements = new List<long>();
        using var listener = SkillCompletionListener.Start((_, value, _, _) => measurements.Add(value));
        EventHappened<CreatureInterruptedEvent>? interrupt = null;
        var (task, performer, cookingObject, region, _, location) = CreateTask(character =>
            character.RegisterEventHandler(Arg.Do<EventHappened<CreatureInterruptedEvent>>(handler => interrupt = handler)));
        region.FindGameObjects(location.RegionLocalX, location.RegionLocalY, location.Z)
            .Returns(new[] { cookingObject });
        performer.Inventory.Items.GetById(task.RawDto.ItemId).Returns((IItem?)null);

        task.Tick();
        if (interrupted)
        {
            Assert.IsNotNull(interrupt);
            interrupt(new CreatureInterruptedEvent(performer, this));
            Assert.IsTrue(task.IsCancelled);
        }

        task.Tick();
        task.Tick();

        Assert.IsTrue(task.IsCancelled);
        performer.Inventory.DidNotReceive().ReplaceItem(Arg.Any<int>(), Arg.Any<IItem>());
        Assert.IsEmpty(measurements);
    }

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
        ILocation Location) CreateTask(Action<ICharacter>? configurePerformer = null)
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
        var cookedItem = Substitute.For<IItem>();
        cookedItem.Id.Returns(2);
        itemBuilder.Create().WithId(2).Build().Returns(cookedItem);
        configurePerformer?.Invoke(performer);

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
