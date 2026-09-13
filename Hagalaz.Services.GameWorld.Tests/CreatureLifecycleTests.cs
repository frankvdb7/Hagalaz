using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Extensions;
using Hagalaz.Services.GameWorld.Model.Creatures;
using Hagalaz.Services.GameWorld.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class CreatureLifecycleTests
{
    [TestMethod]
    public void DestroyBeforeRegionRegistration_DoesNotCreateARegion()
    {
        var (creature, mapRegionService, scope) = CreateCreature();

        creature.Destroy();

        mapRegionService.DidNotReceive().GetOrCreateMapRegion(Arg.Any<int>(), Arg.Any<int>());
        scope.Received(1).Dispose();
    }

    [TestMethod]
    public void DestroyAfterRegionRegistration_UnlinksFromExistingRegionWithoutCreatingAnother()
    {
        var (creature, mapRegionService, scope) = CreateCreature();
        var region = Substitute.For<IMapRegion>();
        creature.RegionToAttach = region;

        creature.SetLocation(creature.Location, forceRegionUpdate: true, firstUpdate: true);
        creature.Destroy();

        mapRegionService.DidNotReceive().GetOrCreateMapRegion(Arg.Any<int>(), Arg.Any<int>());
        Assert.AreSame(region, creature.LastRemovedRegion);
        scope.Received(1).Dispose();
    }

    [TestMethod]
    public void Destroy_DetachesFromRegionAndAreaBeforeDestroyCallback()
    {
        var (creature, mapRegionService, _, area) = CreateCreatureWithArea();
        var region = Substitute.For<IMapRegion>();
        mapRegionService.GetOrCreateMapRegion(creature.Location.RegionId, creature.Location.Dimension).Returns(region);
        mapRegionService.FindMapRegion(creature.Location.RegionId, creature.Location.Dimension).Returns(region);

        var areaExited = false;
        area.When(value => value.OnCreatureExitArea(creature)).Do(_ =>
        {
            areaExited = true;
            creature.MarkAreaExited();
        });
        creature.SetLocation(creature.Location, forceRegionUpdate: true, firstUpdate: true);

        creature.Destroy();

        Assert.IsTrue(creature.ObservedRegionDetachedDuringDestroy);
        Assert.IsTrue(creature.ObservedAreaExitedDuringDestroy);
        Assert.IsTrue(areaExited);
    }

    [TestMethod]
    public void Destroy_WhenOnDestroyFails_DisposesOwnedScope()
    {
        var (creature, _, scope) = CreateCreature(onDestroyFailure: true);

        Assert.ThrowsExactly<InvalidOperationException>(() => creature.Destroy());

        scope.Received(1).Dispose();
    }

    [TestMethod]
    public void Destroy_CancelsPendingCreatureTask()
    {
        var scheduler = new RsTaskService(Microsoft.Extensions.Logging.Abstractions.NullLogger<RsTaskService>.Instance);
        var taskService = new CreatureTaskService(scheduler);
        var (creature, _, _) = CreateCreature(taskService: taskService);
        var executed = false;

        creature.QueueTask(new RsTask(() => executed = true, 2));
        creature.Destroy();
        scheduler.Tick();

        Assert.IsFalse(executed);
    }

    [TestMethod]
    public void Destroy_StopsLongLivedCreatureTask()
    {
        var scheduler = new RsTaskService(Microsoft.Extensions.Logging.Abstractions.NullLogger<RsTaskService>.Instance);
        var taskService = new CreatureTaskService(scheduler);
        var (creature, _, _) = CreateCreature(taskService: taskService);
        var ticks = 0;

        creature.QueueTask(new RsTickTask(() => ticks++));
        scheduler.Tick();
        creature.Destroy();
        scheduler.Tick();
        scheduler.Tick();

        Assert.AreEqual(1, ticks);
    }

    [TestMethod]
    public void QueueTask_AfterDestroy_IsCancelledWithoutExecuting()
    {
        var scheduler = new RsTaskService(Microsoft.Extensions.Logging.Abstractions.NullLogger<RsTaskService>.Instance);
        var taskService = new CreatureTaskService(scheduler);
        var (creature, _, _) = CreateCreature(taskService: taskService);
        var executed = false;

        creature.Destroy();
        creature.QueueTask(new RsTask(() => executed = true, 1));
        scheduler.Tick();

        Assert.IsFalse(executed);
    }

    [TestMethod]
    public async Task Destroy_PropagatesCancellationToAsyncCreatureTask()
    {
        var scheduler = new RsTaskService(Microsoft.Extensions.Logging.Abstractions.NullLogger<RsTaskService>.Instance);
        var taskService = new CreatureTaskService(scheduler);
        var (creature, _, _) = CreateCreature(taskService: taskService);
        var operationStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var operation = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var receivedToken = CancellationToken.None;

        creature.QueueTask(token =>
        {
            receivedToken = token;
            operationStarted.SetResult(true);
            return operation.Task;
        });
        scheduler.Tick();
        await operationStarted.Task;

        creature.Destroy();

        Assert.IsTrue(receivedToken.IsCancellationRequested);
        operation.SetResult(true);
        scheduler.Tick();
    }

    [TestMethod]
    public void MajorUpdateTick_WhenContentFails_DoesNotBlockTheNextTick()
    {
        var (creature, _, _) = CreateCreature();
        creature.FailNextContentTick = true;

        Assert.ThrowsExactly<InvalidOperationException>(() => creature.MajorUpdateTick());

        creature.MajorUpdateTick();

        Assert.AreEqual(2, creature.ContentTickCalls);
    }


    private static (TestCreature Creature, IMapRegionService MapRegionService, IServiceScope Scope) CreateCreature(
        bool onDestroyFailure = false,
        ICreatureTaskService? taskService = null)
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        var scope = Substitute.For<IServiceScope>();
        var mapRegionService = Substitute.For<IMapRegionService>();
        serviceProvider.GetService(typeof(ICreatureTaskService)).Returns(taskService ?? Substitute.For<ICreatureTaskService>());
        serviceProvider.GetService(typeof(IMapRegionService)).Returns(mapRegionService);
        serviceProvider.GetService(typeof(IAreaService)).Returns(Substitute.For<IAreaService>());
        serviceProvider.GetService(typeof(IScopedGameMediator)).Returns(Substitute.For<IScopedGameMediator>());
        scope.ServiceProvider.Returns(serviceProvider);

        return (new TestCreature(scope, onDestroyFailure), mapRegionService, scope);
    }

    private static (TestCreature Creature, IMapRegionService MapRegionService, IServiceScope Scope, IArea Area) CreateCreatureWithArea()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        var scope = Substitute.For<IServiceScope>();
        var mapRegionService = Substitute.For<IMapRegionService>();
        var area = Substitute.For<IArea>();
        serviceProvider.GetService(typeof(ICreatureTaskService)).Returns(Substitute.For<ICreatureTaskService>());
        serviceProvider.GetService(typeof(IMapRegionService)).Returns(mapRegionService);
        serviceProvider.GetService(typeof(IAreaService)).Returns(Substitute.For<IAreaService>());
        serviceProvider.GetRequiredService<IAreaService>().FindAreaByLocation(Arg.Any<ILocation>()).Returns(area);
        serviceProvider.GetService(typeof(IScopedGameMediator)).Returns(Substitute.For<IScopedGameMediator>());
        scope.ServiceProvider.Returns(serviceProvider);

        return (new TestCreature(scope, false), mapRegionService, scope, area);
    }

    private sealed class TestCreature : Creature
    {
        public bool FailOnDestroy { get; set; }
        public int DestroyCalls { get; private set; }
        public int ContentTickCalls { get; private set; }
        public int UpdatePrepareTickCalls { get; private set; }
        public int UpdateTickCalls { get; private set; }
        public int ResetTickCalls { get; private set; }
        public bool FailNextContentTick { get; set; }
        public bool ObservedRegionDetachedDuringDestroy { get; private set; }
        public bool ObservedAreaExitedDuringDestroy { get; private set; }
        public IMapRegion? RegionToAttach { get; set; }
        private bool _regionDetached;
        private bool _areaExited;

        public void MarkAreaExited() => _areaExited = true;

        public TestCreature(IServiceScope scope, bool onDestroyFailure)
            : base(scope)
        {
            FailOnDestroy = onDestroyFailure;
            Location = new Location(3200, 3200, 0, 0);
            Combat = Substitute.For<ICreatureCombat>();
            Movement = Substitute.For<IMovement>();
        }

        public override int Size => 1;
        public override IPathFinder PathFinder => Substitute.For<IPathFinder>();
        public override bool CanDestroy() => true;
        public override bool CanSuspend() => true;
        protected override void OnDestroy()
        {
            DestroyCalls++;
            ObservedRegionDetachedDuringDestroy = _regionDetached;
            ObservedAreaExitedDuringDestroy = _areaExited;
            if (FailOnDestroy)
            {
                throw new InvalidOperationException("destroy failed");
            }
        }

        public override void OnSpawn() { }
        public override void OnDeath() { }
        public override void OnKilledBy(ICreature killer) { }
        public override void OnTargetKilled(ICreature target) { }
        public override bool Poison(short amount) => false;
        public override void Respawn() { }
        public override void Interrupt(object source) { }
        public override void MovementTypeChanged(MovementType newtype) { }
        public override void TemporaryMovementTypeEnabled(MovementType type) { }
        protected override void ContentTick()
        {
            ContentTickCalls++;
            if (FailNextContentTick)
            {
                FailNextContentTick = false;
                throw new InvalidOperationException("content tick failed");
            }
        }
        protected override void UpdatesPrepareTick() => UpdatePrepareTickCalls++;
        protected override void UpdateTick() => UpdateTickCalls++;
        protected override void ResetTick() => ResetTickCalls++;
        protected override void OnLocationChange(ILocation? oldLocation) { }
        protected override void OnRegionChange() { }
        protected override IMapRegion AddToRegion() => RegionToAttach ?? Substitute.For<IMapRegion>();
        protected override void RemoveFromRegion(IMapRegion region)
        {
            LastRemovedRegion = region;
            _regionDetached = true;
        }
        public IMapRegion? LastRemovedRegion { get; private set; }
        protected override void CreatureFaced(ICreature? creature) { }
        protected override void TurnedTo(int x, int y) { }
        protected override void TextSpoken(string text) { }
        protected override void HitSplatRendered(IHitSplat splat) { }
        protected override void HitBarRendered(IHitBar bar) { }
        public override bool ShouldBeRenderedFor(ICharacter viewer) => false;
        public override bool ShouldBeRenderedFor(INpc viewer) => false;
        protected override void NonstandardMovementRendered(IForceMovement movement) { }
        protected override void GlowRendered(IGlow glow) { }
    }
}
