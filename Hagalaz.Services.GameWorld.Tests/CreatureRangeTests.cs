using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

namespace Hagalaz.Services.GameWorld.Tests
{
    [TestClass]
    public class CreatureRangeTests
    {
        private IServiceScope _serviceScope;
        private Creature _creature1;
        private Creature _creature2;

        [TestInitialize]
        public void Setup()
        {
            var serviceProviderMock = Substitute.For<IServiceProvider>();
            serviceProviderMock.GetService(typeof(ICreatureTaskService)).Returns(Substitute.For<ICreatureTaskService>());
            serviceProviderMock.GetService(typeof(IMapRegionService)).Returns(Substitute.For<IMapRegionService>());
            serviceProviderMock.GetService(typeof(IAreaService)).Returns(Substitute.For<IAreaService>());

            _serviceScope = Substitute.For<IServiceScope>();
            _serviceScope.ServiceProvider.Returns(serviceProviderMock);
        }

        [TestMethod]
        public void WithinRange_1x1_SameLocation_ReturnsTrue()
        {
            _creature1 = new TestCreature(_serviceScope, 1);
            _creature2 = new TestCreature(_serviceScope, 1);
            var loc = new Location(100, 100, 0, 0);
            _creature1.SetLocation(loc, firstUpdate: true);
            _creature2.SetLocation(loc, firstUpdate: true);

            Assert.IsTrue(_creature1.WithinRange(_creature2, 0));
            Assert.IsTrue(_creature1.WithinRange(_creature2, 1));
        }

        [TestMethod]
        public void WithinRange_1x1_Adjacent_ReturnsTrue()
        {
            _creature1 = new TestCreature(_serviceScope, 1);
            _creature2 = new TestCreature(_serviceScope, 1);
            _creature1.SetLocation(new Location(100, 100, 0, 0), firstUpdate: true);
            _creature2.SetLocation(new Location(101, 100, 0, 0), firstUpdate: true);

            Assert.IsTrue(_creature1.WithinRange(_creature2, 1));
            Assert.IsFalse(_creature1.WithinRange(_creature2, 0));
        }

        [TestMethod]
        public void WithinRange_1x1_Diagonal_ReturnsTrueAtRange2()
        {
            _creature1 = new TestCreature(_serviceScope, 1);
            _creature2 = new TestCreature(_serviceScope, 1);
            _creature1.SetLocation(new Location(100, 100, 0, 0), firstUpdate: true);
            _creature2.SetLocation(new Location(101, 101, 0, 0), firstUpdate: true);

            // sqrt(1^2 + 1^2) = 1.414. (int)1.414 = 1.
            Assert.IsTrue(_creature1.WithinRange(_creature2, 1));
            Assert.IsFalse(_creature1.WithinRange(_creature2, 0));
        }

        [TestMethod]
        public void WithinRange_3x3_Overlap_ReturnsTrue()
        {
            _creature1 = new TestCreature(_serviceScope, 3);
            _creature2 = new TestCreature(_serviceScope, 3);
            _creature1.SetLocation(new Location(100, 100, 0, 0), firstUpdate: true);
            _creature2.SetLocation(new Location(102, 102, 0, 0), firstUpdate: true); // Overlaps at (102,102)

            Assert.IsTrue(_creature1.WithinRange(_creature2, 0));
        }

        [TestMethod]
        public void WithinRange_3x3_Boundary_ReturnsTrue()
        {
            _creature1 = new TestCreature(_serviceScope, 3); // (100,100) to (102,102)
            _creature2 = new TestCreature(_serviceScope, 3); // (106,100) to (108,102)
            _creature1.SetLocation(new Location(100, 100, 0, 0), firstUpdate: true);
            _creature2.SetLocation(new Location(106, 100, 0, 0), firstUpdate: true);

            // Distance from 102 to 106 is 4.
            Assert.IsTrue(_creature1.WithinRange(_creature2, 4));
            Assert.IsFalse(_creature1.WithinRange(_creature2, 3));
        }

        [TestMethod]
        public void WithinRange_DifferentPlane_ReturnsFalse()
        {
            _creature1 = new TestCreature(_serviceScope, 1);
            _creature2 = new TestCreature(_serviceScope, 1);
            _creature1.SetLocation(new Location(100, 100, 0, 0), firstUpdate: true);
            _creature2.SetLocation(new Location(100, 100, 1, 0), firstUpdate: true);

            Assert.IsFalse(_creature1.WithinRange(_creature2, 10));
        }

        [TestMethod]
        public void WithinRange_DifferentDimension_ReturnsFalse()
        {
            _creature1 = new TestCreature(_serviceScope, 1);
            _creature2 = new TestCreature(_serviceScope, 1);
            _creature1.SetLocation(new Location(100, 100, 0, 0), firstUpdate: true);
            _creature2.SetLocation(new Location(100, 100, 0, 1), firstUpdate: true);

            Assert.IsFalse(_creature1.WithinRange(_creature2, 10));
        }

        private class TestCreature : Creature
        {
            private readonly int _size;
            public TestCreature(IServiceScope serviceScope, int size) : base(serviceScope)
            {
                _size = size;
                Movement = Substitute.For<IMovement>();
                Combat = Substitute.For<ICreatureCombat>();
                Viewport = Substitute.For<IViewport>();
            }

            public override int Size => _size;
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
