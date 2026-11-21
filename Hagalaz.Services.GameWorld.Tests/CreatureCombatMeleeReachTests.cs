using Hagalaz.Game.Abstractions.Builders.HitSplat;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Services.GameWorld.Logic.Pathfinding;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Configuration;
using Hagalaz.Services.GameWorld.Model.Creatures;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Store;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests
{
    [TestClass]
    public class CreatureCombatMeleeReachTests
    {
        private INpc _owner = null!;
        private INpc _target = null!;
        private IPathFinder _pathFinder = null!;
        private IMovement _targetMovement = null!;
        private StubCombat _combat = null!;
        private readonly HashSet<(int ToX, int ToY, int Dx, int Dy)> _blockedSteps = new();

        [TestInitialize]
        public void TestInitialize()
        {
            var entityStore = new EntityStore();
            _owner = EntityTestFactory.Create<INpc>();
            _target = EntityTestFactory.Create<INpc>();
            _pathFinder = Substitute.For<IPathFinder>();
            _targetMovement = Substitute.For<IMovement>();
            var ownerMovement = Substitute.For<IMovement>();

            _owner.PathFinder.Returns(_pathFinder);
            _owner.Movement.Returns(ownerMovement);
            _owner.Size.Returns(1);
            _owner.Location.Returns(Location.Create(3200, 3200));
            _target.Size.Returns(1);
            _target.Movement.Returns(_targetMovement);
            SetTargetLocation(3201, 3200);

            _pathFinder.CheckStep(Arg.Any<IVector3>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>())
                .Returns(call => !_blockedSteps.Contains((call.ArgAt<IVector3>(0).X, call.ArgAt<IVector3>(0).Y, call.ArgAt<int>(1), call.ArgAt<int>(2))));
            _pathFinder.Find(Arg.Any<IEntity>(), Arg.Any<IEntity>(), Arg.Any<bool>()).Returns(Substitute.For<IPath>());

            entityStore.Add(_owner);
            entityStore.Add(_target);
            _combat = new StubCombat(_owner, new EntityService(entityStore), Substitute.For<IProjectilePathFinder>(), attackDistance: 1);
            _combat.SetTargetHandle(_target.Handle);
        }

        [TestMethod]
        public void Tick_CardinalAdjacentAndOpen_Attacks()
        {
            _combat.Tick();

            Assert.AreEqual(1, _combat.Attacks);
            _pathFinder.DidNotReceiveWithAnyArgs().Find(default!, default(IEntity)!, default);
        }

        [TestMethod]
        public void Tick_CardinalAdjacentAcrossWall_DoesNotAttackAndFallsBackToPathfinder()
        {
            _blockedSteps.Add((3201, 3200, 1, 0));

            _combat.Tick();
            _combat.Tick();

            Assert.AreEqual(0, _combat.Attacks);
            _pathFinder.Received(2).Find(_owner, _target, true);
        }

        [TestMethod]
        public void Tick_DiagonalAdjacentAndOpen_Attacks()
        {
            SetTargetLocation(3201, 3201);

            _combat.Tick();

            Assert.AreEqual(1, _combat.Attacks);
        }

        [TestMethod]
        public void Tick_DiagonalAdjacentWithClippedCorner_DoesNotAttack()
        {
            SetTargetLocation(3201, 3201);
            _blockedSteps.Add((3201, 3201, 1, 1));

            _combat.Tick();

            Assert.AreEqual(0, _combat.Attacks);
            _pathFinder.Received(1).Find(_owner, _target, true);
        }

        [TestMethod]
        public void Tick_LargeOwnerWithOneOpenEdge_Attacks()
        {
            _owner.Size.Returns(2);
            // Owner covers x 3200-3201 / y 3200-3201; target at 3202,3200 touches both rows.
            SetTargetLocation(3202, 3200);
            _blockedSteps.Add((3202, 3200, 1, 0));

            _combat.Tick();

            Assert.AreEqual(1, _combat.Attacks);
        }

        [TestMethod]
        public void Tick_LargeOwnerWithAllEdgesBlocked_DoesNotAttack()
        {
            _owner.Size.Returns(2);
            SetTargetLocation(3202, 3200);
            _blockedSteps.Add((3202, 3200, 1, 0));
            _blockedSteps.Add((3202, 3200, 1, -1)); // diagonal edge from owner tile (3201,3201)

            _combat.Tick();

            Assert.AreEqual(0, _combat.Attacks);
        }

        [TestMethod]
        public void Tick_TargetMovedThisTick_ValidatesCollisionAgainstLastLocation()
        {
            // Target walked 3201,3200 -> 3202,3200; combat evaluates the previous tile.
            SetTargetLocation(3202, 3200);
            _target.LastLocation.Returns(Location.Create(3201, 3200));
            _targetMovement.Moved.Returns(true);
            _blockedSteps.Add((3202, 3200, 1, 0)); // blocked at current tile only

            _combat.Tick();

            Assert.AreEqual(1, _combat.Attacks);
            _pathFinder.Received().CheckStep(Arg.Is<IVector3>(l => l.X == 3201 && l.Y == 3200 && l.Z == 0), 1, 0, 1);
            _pathFinder.DidNotReceive().CheckStep(Arg.Is<IVector3>(l => l.X == 3202 && l.Y == 3200), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>());
        }

        [TestMethod]
        public void Tick_RangedWithinRange_DoesNotUseMeleeCollisionCheck()
        {
            var projectilePathFinder = Substitute.For<IProjectilePathFinder>();
            var clearPath = Substitute.For<IPath>();
            clearPath.Successful.Returns(true);
            projectilePathFinder.Find(_owner, _target, true).Returns(clearPath);
            _combat = new StubCombat(_owner, _combat.EntityServiceForTest, projectilePathFinder, attackDistance: 5);
            _combat.SetTargetHandle(_target.Handle);
            SetTargetLocation(3203, 3200);
            _blockedSteps.Add((3203, 3200, 1, 0));

            _combat.Tick();

            Assert.AreEqual(1, _combat.Attacks);
            _pathFinder.DidNotReceiveWithAnyArgs().CheckStep(default!, default, default, default);
        }

        [TestMethod]
        public void Tick_RepeatedTicksAcrossWall_NeverAttackUntilWallIsRemoved()
        {
            _blockedSteps.Add((3201, 3200, 1, 0));

            for (var i = 0; i < 10; i++)
            {
                _combat.Tick();
            }

            Assert.AreEqual(0, _combat.Attacks);

            _blockedSteps.Clear();
            _combat.Tick();

            Assert.AreEqual(1, _combat.Attacks);
        }

        [TestMethod]
        public void Tick_RealPathFinderWithWestWallOnTargetTile_DoesNotAttack()
        {
            var regionService = Substitute.For<IMapRegionService>();
            regionService.GetClippingFlag(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>()).Returns(CollisionFlag.Walkable);
            regionService.GetClippingFlag(3201, 3200, 0, 0).Returns(CollisionFlag.CheckWest);
            _owner.PathFinder.Returns(new DumbPathFinder(regionService));

            _combat.Tick();
            _combat.Tick();

            Assert.AreEqual(0, _combat.Attacks);
        }

        [TestMethod]
        public void Tick_RealPathFinderWithGatewayWallOnly_DoesNotAttack()
        {
            // Gateway walls carry WallWest without WallAllowRangeWest or FloorBlock.
            var regionService = Substitute.For<IMapRegionService>();
            regionService.GetClippingFlag(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>()).Returns(CollisionFlag.Walkable);
            regionService.GetClippingFlag(3201, 3200, 0, 0).Returns(CollisionFlag.WallWest);
            _owner.PathFinder.Returns(new DumbPathFinder(regionService));

            _combat.Tick();
            _combat.Tick();

            Assert.AreEqual(0, _combat.Attacks);
        }

        [TestMethod]
        public void Tick_RealPathFinderWithOpenGround_Attacks()
        {
            var regionService = Substitute.For<IMapRegionService>();
            regionService.GetClippingFlag(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>()).Returns(CollisionFlag.Walkable);
            _owner.PathFinder.Returns(new DumbPathFinder(regionService));

            _combat.Tick();

            Assert.AreEqual(1, _combat.Attacks);
        }

        private void SetTargetLocation(int x, int y) => _target.Location.Returns(Location.Create(x, y));

        private sealed class StubCombat : CreatureCombat
        {
            private readonly int _attackDistance;

            public StubCombat(INpc owner, IEntityService entityService, IProjectilePathFinder projectilePathFinder, int attackDistance)
                : base(owner, entityService, projectilePathFinder, Substitute.For<ISmartPathFinder>(),
                    Options.Create(new CombatOptions()), Substitute.For<IHitSplatBuilder>(), new CreatureCombatRules())
            {
                _attackDistance = attackDistance;
                EntityServiceForTest = entityService;
            }

            public IEntityService EntityServiceForTest { get; }
            public int Attacks { get; private set; }

            public new void SetTargetHandle(EntityHandle<ICreature> handle) => base.SetTargetHandle(handle);

            protected override void AttackTick() => Attacks++;
            public override int GetAttackDistance() => _attackDistance;
            public override int GetAttackSpeed() => 1;
            protected override bool CanSetResolvedTarget(ICreature target) => true;
            protected override void CursesTick() { }
            protected override void OnLastAttackedFade() { }
            protected override int RenderDeath() => 0;
            public override void OnDeath() { }
            public override void OnTargetKilled(ICreature target) { }
            public override void OnKilledBy(ICreature killer) { }
            public override bool CanBeLootedBy(ICreature killer) => false;
            public override void OnSpawn() { }
            public override bool CanAttack(ICreature target) => true;
            public override bool CanBeAttackedBy(ICreature attacker) => true;
            public override bool SetTarget(EntityHandle<ICreature> target) => true;
            public override bool CanSetTarget(EntityHandle<ICreature> target) => true;
            public override void OnAttackPerformed(ICreature target) { }
            public override void CancelTarget() { }
            public override AttackStyle GetAttackStyle() => default;
            public override AttackBonus GetAttackBonusType() => default;
            public override int GetBonus(BonusType bonusType) => 0;
            public override int GetPrayerBonus(BonusPrayerType bonusType) => 0;
            public override int GetAttackLevel() => 1;
            public override int GetStrengthLevel() => 1;
            public override int GetDefenceLevel() => 1;
            public override int GetRangedLevel() => 1;
            public override int GetMagicLevel() => 1;
            public override int IncomingAttack(ICreature attacker, DamageType damageType, int damage, int delay) => 0;
            public override int Attack(ICreature attacker, DamageType damageType, int damage, ref int soaked) => 0;
        }
    }
}
