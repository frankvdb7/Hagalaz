using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
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
using Hagalaz.Services.GameWorld.Model.Creatures;

namespace Hagalaz.Services.GameWorld.Tests
{
    [TestClass]
    public class CreatureStateTests
    {
        private IServiceScope _serviceScope;
        private IGameMediator _mediatorMock;
        private ICreatureTaskService _taskServiceMock;
        private IMapRegionService _mapRegionServiceMock;
        private Creature _creature;

        [TestInitialize]
        public void Setup()
        {
            var serviceProviderMock = Substitute.For<IServiceProvider>();
            _mediatorMock = Substitute.For<IGameMediator>();
            _taskServiceMock = Substitute.For<ICreatureTaskService>();
            _mapRegionServiceMock = Substitute.For<IMapRegionService>();

            serviceProviderMock.GetService(typeof(IGameMediator)).Returns(_mediatorMock);
            serviceProviderMock.GetService(typeof(ICreatureTaskService)).Returns(_taskServiceMock);
            serviceProviderMock.GetService(typeof(IMapRegionService)).Returns(_mapRegionServiceMock);

            _serviceScope = Substitute.For<IServiceScope>();
            _serviceScope.ServiceProvider.Returns(serviceProviderMock);

            _creature = new CreatureMock(_serviceScope);
        }

        [TestMethod]
        public void AddState_AddsNewState()
        {
            var state = new ResistPoisonState { };
            _creature.AddState(state);

            Assert.IsTrue(_creature.HasState<ResistPoisonState>());
            Assert.AreEqual(1, _creature.GetStates().Count());
            Assert.AreEqual(state, _creature.GetStates().First());
        }

        [TestMethod]
        public void AddState_UpdatesExistingStateIfNewer()
        {
            var oldState = new ResistPoisonState { };
            _creature.AddState(oldState);

            var newState = new ResistPoisonState { };
            _creature.AddState(newState);

            Assert.IsTrue(_creature.HasState<ResistPoisonState>());
            Assert.AreEqual(1, _creature.GetStates().Count());
        }

        [TestMethod]
        public void AddState_DoesNotUpdateExistingStateIfOlder()
        {
            var oldState = new ResistPoisonState { };
            _creature.AddState(oldState);

            var newState = new ResistPoisonState { };
            _creature.AddState(newState);

            Assert.IsTrue(_creature.HasState<ResistPoisonState>());
            Assert.AreEqual(1, _creature.GetStates().Count());
        }

        [TestMethod]
        public void RemoveState_RemovesExistingState()
        {
            var state = new ResistPoisonState { };
            _creature.AddState(state);
            Assert.IsTrue(_creature.HasState<ResistPoisonState>());

            _creature.RemoveState<ResistPoisonState>();
            Assert.IsFalse(_creature.HasState<ResistPoisonState>());
            Assert.AreEqual(0, _creature.GetStates().Count());
        }

        [TestMethod]
        public void RemoveState_DoesNothingIfStateDoesNotExist()
        {
            _creature.RemoveState<ResistPoisonState>();
            Assert.IsFalse(_creature.HasState<ResistPoisonState>());
            Assert.AreEqual(0, _creature.GetStates().Count());
        }

        [TestMethod]
        public void GetStates_ReturnsAllActiveStates()
        {
            var state1 = new ResistPoisonState { };
            var state2 = new RestingState { };
            _creature.AddState(state1);
            _creature.AddState(state2);

            var states = _creature.GetStates().ToList();
            Assert.AreEqual(2, states.Count);
            Assert.IsTrue(states.Contains(state1));
            Assert.IsTrue(states.Contains(state2));
        }

        private class CreatureMock : Creature
        {
            public CreatureMock(IServiceScope serviceScope)
                : base(serviceScope)
            {
                Location = Substitute.For<ILocation>();
                Movement = Substitute.For<IMovement>();
                Combat = Substitute.For<ICreatureCombat>();
            }

            public override int Size => 1;
            public override IPathFinder PathFinder => Substitute.For<IPathFinder>();

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
            protected override Task UpdateTick() => Task.CompletedTask;
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
}
