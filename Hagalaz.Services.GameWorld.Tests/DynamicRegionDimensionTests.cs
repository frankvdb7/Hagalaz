using AutoMapper;
using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Model.Maps.Regions;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class DynamicRegionDimensionTests
{
    [TestMethod]
    public void LoadPartObjects_UsesSourceDimensionAndDestinationDimensionForCopiedObjectsAndCollision()
    {
        var regionService = Substitute.For<IMapRegionService>();
        var source = CreateRegion(Location.Create(0, 0, 0, 1), regionService);
        var gameObjectBuilder = Substitute.For<IGameObjectBuilder>();
        var objectIdBuilder = Substitute.For<IGameObjectId>();
        var objectLocationBuilder = Substitute.For<IGameObjectLocation>();
        var objectOptionalBuilder = Substitute.For<IGameObjectOptional>();
        var copiedObject = Substitute.For<IGameObject>();
        ILocation? copiedLocation = null;
        gameObjectBuilder.Create().Returns(objectIdBuilder);
        objectIdBuilder.WithId(42).Returns(objectLocationBuilder);
        objectLocationBuilder.WithLocation(Arg.Any<ILocation>()).Returns(objectOptionalBuilder);
        objectLocationBuilder.When(builder => builder.WithLocation(Arg.Any<ILocation>()))
            .Do(call => copiedLocation = call.Arg<ILocation>());
        objectOptionalBuilder.WithRotation(Arg.Any<int>()).Returns(objectOptionalBuilder);
        objectOptionalBuilder.WithShape(Arg.Any<ShapeType>()).Returns(objectOptionalBuilder);
        objectOptionalBuilder.Build().Returns(copiedObject);
        copiedObject.Location.Returns(_ => copiedLocation!);
        copiedObject.IsStatic.Returns(false);
        copiedObject.ShapeType.Returns(ShapeType.GroundDefault);
        copiedObject.SizeX.Returns(1);
        copiedObject.SizeY.Returns(1);
        var destination = CreateRegion(Location.Create(128, 64, 0, 2), regionService, gameObjectBuilder);
        source.MakeStandard();
        destination.MakeDynamic();

        var sourceObject = Substitute.For<IGameObject>();
        sourceObject.Id.Returns(42);
        sourceObject.Location.Returns(Location.Create(1, 1, 0, 1));
        sourceObject.IsStatic.Returns(false);
        sourceObject.ShapeType.Returns(ShapeType.GroundDefault);
        sourceObject.Rotation.Returns(0);
        sourceObject.SizeX.Returns(1);
        sourceObject.SizeY.Returns(1);
        source.Add(sourceObject);
        source.FlagCollision(1, 1, 0, CollisionFlag.WallNorth);

        regionService.GetOrCreateMapRegion(source.Id, 1, true).Returns(source);

        destination.WriteBlock(0, 0, 0, 0, 0, 0, 1);

        regionService.Received(1).GetOrCreateMapRegion(source.Id, 1, true);
        copiedObject = destination.FindGameObjects(1, 1, 0).Single();
        Assert.AreEqual(2, copiedObject.Location.Dimension);
        Assert.AreEqual(CollisionFlag.WallNorth, destination.GetCollision(1, 1, 0));
        Assert.AreEqual(2, destination.BaseLocation.Dimension);

        var erasedPart = destination.GetRegionPartByLocalPart(1, 1, 0);
        erasedPart.Erase();
        destination.LoadPartObjects(destination.LocalPartXToPartX(1), destination.LocalPartYToPartY(1), 0, 0);
        Assert.IsFalse(erasedPart.HasDrawSource);
        Assert.IsFalse(destination.FindGameObjects(9, 9, 0).Any());
    }

    private static MapRegion CreateRegion(ILocation location, IMapRegionService regionService, IGameObjectBuilder? gameObjectBuilder = null) => new(
        location,
        [0, 0, 0, 0],
        Substitute.For<INpcService>(),
        regionService,
        gameObjectBuilder ?? Substitute.For<IGameObjectBuilder>(),
        Substitute.For<IGroundItemBuilder>(),
        Substitute.For<IMapper>());
}
