using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Logic.Pathfinding;
using Hagalaz.Services.GameWorld.Model.Creatures;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class MovementTests
{
    [TestMethod]
    public void Tick_LongQueuedWaypoint_DoesNotSkipNewlyBlockedTile()
    {
        var start = Location.Create(10, 10, 0);
        var target = Location.Create(15, 10, 0);
        var (creature, mapRegionService, pathFinder) = CreateCreature(start, 1);
        var path = pathFinder.Find(start, 1, target, 1, 1, 0, 0, 0, false);
        Assert.IsTrue(path.Successful);

        creature.Movement.AddToQueue(path);
        mapRegionService.GetClippingFlag(11, 10, 0).Returns(CollisionFlag.FloorBlock);

        creature.Movement.Tick();

        Assert.AreEqual(start.X, creature.Location.X);
        Assert.AreEqual(start.Y, creature.Location.Y);
        Assert.IsTrue(creature.Movement.Moving);
    }

    [TestMethod]
    public void Tick_RunMovement_ValidatesEachAppliedUnit()
    {
        var start = Location.Create(10, 10, 0);
        var target = Location.Create(15, 10, 0);
        var (creature, mapRegionService, pathFinder) = CreateCreature(start, 1);
        var path = pathFinder.Find(start, 1, target, 1, 1, 0, 0, 0, false);
        Assert.IsTrue(path.Successful);

        creature.Movement.MovementType = MovementType.Run;
        creature.Movement.AddToQueue(path);
        mapRegionService.GetClippingFlag(12, 10, 0).Returns(CollisionFlag.FloorBlock);

        creature.Movement.Tick();

        Assert.AreEqual(11, creature.Location.X);
        Assert.AreEqual(start.Y, creature.Location.Y);
        Assert.IsTrue(creature.Movement.Moving);
    }

    [TestMethod]
    public void Tick_WalkMovement_AdvancesOneTileTowardQueuedWaypoint()
    {
        var start = Location.Create(10, 10, 0);
        var target = Location.Create(15, 10, 0);
        var (creature, _, _) = CreateCreature(start, 1);

        creature.Movement.AddToQueue(target);
        creature.Movement.Tick();

        Assert.AreEqual(11, creature.Location.X);
        Assert.AreEqual(start.Y, creature.Location.Y);
        Assert.IsTrue(creature.Movement.Moved);
        Assert.IsTrue(creature.Movement.Moving);
    }

    [TestMethod]
    public void Tick_RunMovement_AdvancesTwoTilesTowardQueuedWaypoint()
    {
        var start = Location.Create(10, 10, 0);
        var target = Location.Create(15, 10, 0);
        var (creature, _, _) = CreateCreature(start, 1);

        creature.Movement.MovementType = MovementType.Run;
        creature.Movement.AddToQueue(target);
        creature.Movement.Tick();

        Assert.AreEqual(12, creature.Location.X);
        Assert.AreEqual(start.Y, creature.Location.Y);
        Assert.IsTrue(creature.Movement.Moved);
        Assert.IsTrue(creature.Movement.Moving);
    }

    [TestMethod]
    public void Tick_WalkMovement_AdvancesOneDiagonalTileTowardQueuedWaypoint()
    {
        var start = Location.Create(10, 10, 0);
        var target = Location.Create(15, 15, 0);
        var (creature, _, _) = CreateCreature(start, 1);

        creature.Movement.AddToQueue(target);
        creature.Movement.Tick();

        Assert.AreEqual(11, creature.Location.X);
        Assert.AreEqual(11, creature.Location.Y);
        Assert.IsTrue(creature.Movement.Moved);
        Assert.IsTrue(creature.Movement.Moving);
    }

    [TestMethod]
    public void Tick_WarpMovement_CompletesLongQueuedWaypointInOneTick()
    {
        var start = Location.Create(10, 10, 0);
        var target = Location.Create(30, 10, 0);
        var (creature, _, _) = CreateCreature(start, 1);

        creature.Movement.MovementType = MovementType.Warp;
        creature.Movement.AddToQueue(target);
        creature.Movement.Tick();

        Assert.AreEqual(target.X, creature.Location.X);
        Assert.AreEqual(target.Y, creature.Location.Y);
        Assert.IsTrue(creature.Movement.Moved);
        Assert.IsFalse(creature.Movement.Moving);
    }

    [TestMethod]
    public void Tick_WarpMovement_StopsBeforeBlockedIntermediateTileAndResumesAfterUnblocking()
    {
        var start = Location.Create(10, 10, 0);
        var target = Location.Create(30, 10, 0);
        var (creature, mapRegionService, _) = CreateCreature(start, 1);

        creature.Movement.MovementType = MovementType.Warp;
        creature.Movement.AddToQueue(target);
        mapRegionService.GetClippingFlag(15, 10, 0).Returns(CollisionFlag.FloorBlock);

        creature.Movement.Tick();

        Assert.AreEqual(14, creature.Location.X);
        Assert.AreEqual(start.Y, creature.Location.Y);
        Assert.IsTrue(creature.Movement.Moving);

        mapRegionService.GetClippingFlag(15, 10, 0).Returns(CollisionFlag.Walkable);

        creature.Movement.Tick();

        Assert.AreEqual(target.X, creature.Location.X);
        Assert.AreEqual(target.Y, creature.Location.Y);
        Assert.IsFalse(creature.Movement.Moving);
    }

    [TestMethod]
    public void Tick_BlockedWaypoint_ResumesAfterInterveningTileUnblocks()
    {
        var start = Location.Create(10, 10, 0);
        var target = Location.Create(15, 10, 0);
        var (creature, mapRegionService, _) = CreateCreature(start, 1);

        creature.Movement.AddToQueue(target);
        mapRegionService.GetClippingFlag(11, 10, 0).Returns(CollisionFlag.FloorBlock);

        creature.Movement.Tick();

        Assert.AreEqual(start.X, creature.Location.X);
        Assert.IsTrue(creature.Movement.Moving);

        mapRegionService.GetClippingFlag(11, 10, 0).Returns(CollisionFlag.Walkable);
        for (var i = 0; i < 6; i++)
        {
            creature.Movement.Tick();
        }

        Assert.AreEqual(target.X, creature.Location.X);
        Assert.AreEqual(target.Y, creature.Location.Y);
        Assert.IsFalse(creature.Movement.Moving);
    }

    [TestMethod]
    [DataRow(2)]
    [DataRow(3)]
    public void Tick_SizeTwoPlusCreature_AdvancesWithClearClientFootprint(int size)
    {
        var start = Location.Create(10, 10, 0);
        var target = Location.Create(15, 10, 0);
        var (creature, _, _) = CreateCreature(start, size);

        creature.Movement.AddToQueue(target);
        creature.Movement.Tick();

        Assert.AreEqual(11, creature.Location.X);
        Assert.AreEqual(start.Y, creature.Location.Y);
        Assert.IsTrue(creature.Movement.Moved);
        Assert.IsTrue(creature.Movement.Moving);
    }

    [TestMethod]
    [DataRow(2, -1, 0, -1, 1)]
    [DataRow(2, 1, 0, 2, 1)]
    [DataRow(2, 0, -1, 1, -1)]
    [DataRow(2, 0, 1, 1, 2)]
    [DataRow(2, -1, -1, 0, -1)]
    [DataRow(2, 1, -1, 2, -1)]
    [DataRow(2, -1, 1, 0, 2)]
    [DataRow(2, 1, 1, 2, 1)]
    [DataRow(3, -1, 0, -1, 1)]
    [DataRow(3, 1, 0, 3, 1)]
    [DataRow(3, 0, -1, 1, -1)]
    [DataRow(3, 0, 1, 1, 3)]
    [DataRow(3, -1, -1, 0, -1)]
    [DataRow(3, 1, -1, 2, -1)]
    [DataRow(3, -1, 1, 0, 3)]
    [DataRow(3, 1, 1, 2, 3)]
    public void Tick_ClientServerParity_StopsAtExpectedIncomingFootprintEdge(
        int size,
        int xOffset,
        int yOffset,
        int blockedOffsetX,
        int blockedOffsetY)
    {
        var start = Location.Create(10, 10, 0);
        var target = Location.Create(10 + xOffset, 10 + yOffset, 0);
        var (creature, mapRegionService, _) = CreateCreature(start, size);

        creature.Movement.AddToQueue(target);
        mapRegionService.GetClippingFlag(10 + blockedOffsetX, 10 + blockedOffsetY, 0)
            .Returns(CollisionFlag.FloorBlock);

        creature.Movement.Tick();

        Assert.AreEqual(start.X, creature.Location.X);
        Assert.AreEqual(start.Y, creature.Location.Y);
        Assert.IsTrue(creature.Movement.Moving);
    }

    private static (TestCreature Creature, IMapRegionService MapRegionService, SmartPathFinder PathFinder) CreateCreature(
        ILocation location,
        int size)
    {
        var mapRegionService = Substitute.For<IMapRegionService>();
        mapRegionService.GetClippingFlag(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(CollisionFlag.Walkable);
        var pathFinder = new SmartPathFinder(mapRegionService);
        var serviceProvider = Substitute.For<IServiceProvider>();
        var serviceScope = Substitute.For<IServiceScope>();
        var mediator = Substitute.For<IScopedGameMediator>();
        var taskService = Substitute.For<ICreatureTaskService>();
        var areaService = Substitute.For<IAreaService>();

        serviceProvider.GetService(typeof(IScopedGameMediator)).Returns(mediator);
        serviceProvider.GetService(typeof(ICreatureTaskService)).Returns(taskService);
        serviceProvider.GetService(typeof(IAreaService)).Returns(areaService);
        serviceProvider.GetService(typeof(IMapRegionService)).Returns(mapRegionService);
        serviceProvider.GetService(typeof(ISmartPathFinder)).Returns(pathFinder);
        serviceScope.ServiceProvider.Returns(serviceProvider);

        var creature = new TestCreature(serviceScope, location, size);
        creature.InitializeMovement();
        return (creature, mapRegionService, pathFinder);
    }

    private sealed class TestCreature : Creature
    {
        private readonly int _size;

        public TestCreature(IServiceScope serviceScope, ILocation location, int size)
            : base(serviceScope)
        {
            Location = location;
            _size = size;
        }

        public override int Size => _size;
        public override IPathFinder PathFinder => Substitute.For<IPathFinder>();

        public void InitializeMovement() => Movement = new Movement(this);

        public override bool CanDestroy() => true;
        public override bool CanSuspend() => true;
        protected override void OnDestroy() { }
        public override void OnSpawn() { }
        public override void OnDeath() { }
        public override void OnKilledBy(ICreature killer) { }
        public override void OnTargetKilled(ICreature target) { }
        public override bool Poison(short amount) => false;
        public override void Respawn() { }
        public override void Interrupt(object source) { }
        public override void MovementTypeChanged(MovementType newtype) { }
        public override void TemporaryMovementTypeEnabled(MovementType type) { }
        protected override void ContentTick() { }
        protected override void UpdatesPrepareTick() { }
        protected override void UpdateTick() { }
        protected override void ResetTick() { }
        protected override void OnLocationChange(ILocation? oldLocation) { }
        protected override void OnRegionChange() { }
        protected override void AddToRegion(IMapRegion newRegion) { }
        protected override void RemoveFromRegion(IMapRegion region) { }
        protected override void CreatureFaced(ICreature? creature) { }
        protected override void TurnedTo(int x, int y) { }
        protected override void TextSpoken(string text) { }
        protected override void HitSplatRendered(IHitSplat splat) { }
        protected override void HitBarRendered(IHitBar bar) { }
        protected override void NonstandardMovementRendered(IForceMovement movement) { }
        protected override void GlowRendered(IGlow glow) { }
        public override bool ShouldBeRenderedFor(ICharacter viewer) => false;
        public override bool ShouldBeRenderedFor(INpc viewer) => false;
    }
}
