using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Logic.Pathfinding;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

/// <summary>
/// Overlapping coordinates of an encounter instance and the global world are unrelated tiles:
/// every collision read of an instance-originated traversal must come from the instance's own world.
/// </summary>
[TestClass]
public sealed class InstanceCollisionIsolationTests
{
    private const int Instance = 1;

    [TestMethod]
    public void DumbFind_InstanceSource_ReadsOnlyInstanceCollision()
    {
        var world = new FakeWorld();
        world.Block(Instance, 12, 10);
        var pathFinder = new DumbPathFinder(world.Service);

        var instancePath = pathFinder.Find(Location.Create(10, 10, 0, Instance), 1, Location.Create(14, 10, 0), 1, 1, 0, 0, 0, false);
        var globalPath = pathFinder.Find(Location.Create(10, 10, 0), 1, Location.Create(14, 10, 0), 1, 1, 0, 0, 0, false);

        Assert.IsFalse(instancePath.Successful);
        Assert.IsTrue(globalPath.Successful);
    }

    [TestMethod]
    public void DumbFind_GlobalBlockerDoesNotStopInstanceSource()
    {
        var world = new FakeWorld();
        world.Block(0, 12, 10);
        var pathFinder = new DumbPathFinder(world.Service);

        var path = pathFinder.Find(Location.Create(10, 10, 0, Instance), 1, Location.Create(14, 10, 0), 1, 1, 0, 0, 0, false);

        Assert.IsTrue(path.Successful);
        Assert.IsTrue(world.QueriedDimensions.All(d => d == Instance));
    }

    [TestMethod]
    public void DumbFind_CrossingRegionBoundary_ReadsEveryTileFromTheSameInstance()
    {
        var world = new FakeWorld();
        var pathFinder = new DumbPathFinder(world.Service);

        var path = pathFinder.Find(Location.Create(60, 10, 0, Instance), 1, Location.Create(70, 10, 0), 1, 1, 0, 0, 0, false);

        Assert.IsTrue(path.Successful);
        Assert.IsTrue(world.QueriedTiles.Any(t => t.X >= 64));
        Assert.IsTrue(world.QueriedDimensions.All(d => d == Instance));
    }

    [TestMethod]
    public void SmartFind_InstanceSource_ReadsOnlyInstanceCollision()
    {
        var world = new FakeWorld();
        var pathFinder = new SmartPathFinder(world.Service);

        var path = pathFinder.Find(Location.Create(60, 10, 0, Instance), 1, Location.Create(70, 10, 0), 1, 1, 0, 0, 0, false);

        Assert.IsTrue(path.Successful);
        Assert.IsTrue(world.QueriedDimensions.Count > 0);
        Assert.IsTrue(world.QueriedDimensions.All(d => d == Instance));
    }

    [TestMethod]
    public void CheckStep_BindsTheWorldOfTheStepLocationAndRestoresGlobal()
    {
        var world = new FakeWorld();
        world.Block(Instance, 11, 10);
        var pathFinder = new DumbPathFinder(world.Service);

        Assert.IsFalse(pathFinder.CheckStep(Location.Create(11, 10, 0, Instance), 1, 0, 1));
        Assert.IsTrue(pathFinder.CheckStep(Location.Create(11, 10, 0), 1, 0, 1));
        Assert.IsTrue(pathFinder.CheckStep(11, 10, 0, 1, 0, 1));
    }

    [TestMethod]
    public void ProjectileFind_InstanceSource_UsesInstanceLineOfSight()
    {
        var world = new FakeWorld();
        world.Block(Instance, 12, 10, CollisionFlag.ObjectBlock);
        var pathFinder = new ProjectilePathFinder(world.Service);

        var instance = pathFinder.Find(Location.Create(10, 10, 0, Instance), 1, Location.Create(14, 10, 0, Instance), 1, 1, 0, 0, 0, true);
        var global = pathFinder.Find(Location.Create(10, 10, 0), 1, Location.Create(14, 10, 0), 1, 1, 0, 0, 0, true);

        Assert.IsFalse(instance.Successful);
        Assert.IsTrue(global.Successful);
    }

    [TestMethod]
    public void Find_EntitiesInDifferentWorlds_FailsWithoutReadingCollision()
    {
        var world = new FakeWorld();
        var source = CreateEntity(Location.Create(10, 10, 0, Instance));
        var target = CreateEntity(Location.Create(11, 10, 0));

        foreach (var pathFinder in new PathFinderBase[]
                 {
                     new DumbPathFinder(world.Service), new SmartPathFinder(world.Service), new ProjectilePathFinder(world.Service)
                 })
        {
            Assert.IsFalse(pathFinder.Find(source, target, true).Successful, pathFinder.GetType().Name);
        }

        Assert.AreEqual(0, world.QueriedTiles.Count);
    }

    [TestMethod]
    public void Find_EntitiesInSameInstance_StillPath()
    {
        var world = new FakeWorld();
        var source = CreateEntity(Location.Create(10, 10, 0, Instance));
        var target = CreateEntity(Location.Create(13, 10, 0, Instance));

        Assert.IsTrue(new DumbPathFinder(world.Service).Find(source, target, true).Successful);
    }

    private static IEntity CreateEntity(ILocation location)
    {
        var entity = Substitute.For<IEntity>();
        entity.Location.Returns(location);
        entity.Size.Returns(1);
        return entity;
    }

    private sealed class FakeWorld
    {
        private readonly Dictionary<(int Dimension, int X, int Y), CollisionFlag> _flags = new();

        public FakeWorld()
        {
            Service = Substitute.For<IMapRegionService>();
            Service.GetClippingFlag(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>()).Returns(call =>
            {
                var x = call.ArgAt<int>(0);
                var y = call.ArgAt<int>(1);
                var dimension = call.ArgAt<int>(3);
                QueriedDimensions.Add(dimension);
                QueriedTiles.Add((x, y));
                return _flags.GetValueOrDefault((dimension, x, y), CollisionFlag.Walkable);
            });
        }

        public IMapRegionService Service { get; }

        public List<int> QueriedDimensions { get; } = [];

        public List<(int X, int Y)> QueriedTiles { get; } = [];

        public void Block(int dimension, int x, int y, CollisionFlag flag = CollisionFlag.FloorBlock) => _flags[(dimension, x, y)] = flag;
    }
}
