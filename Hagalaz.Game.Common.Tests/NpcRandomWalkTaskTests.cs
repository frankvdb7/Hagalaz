using Hagalaz.Game.Abstractions.Logic.Random;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Common.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;

namespace Hagalaz.Game.Common.Tests.Tasks
{
    [TestClass]
    public class NpcRandomWalkTaskTests
    {
        private INpc _npc;
        private ISimplePathFinder _pathFinder;
        private IRandomProvider _randomProvider;
        private IServiceProvider _serviceProvider;
        private IPathFinderProvider _pathFinderProvider;
        private ICreatureCombat _combat;
        private IMovement _movement;
        private IBounds _bounds;

        [TestInitialize]
        public void Initialize()
        {
            _npc = Substitute.For<INpc>();
            _pathFinder = Substitute.For<ISimplePathFinder>();
            _randomProvider = Substitute.For<IRandomProvider>();
            _serviceProvider = Substitute.For<IServiceProvider>();
            _pathFinderProvider = Substitute.For<IPathFinderProvider>();
            _combat = Substitute.For<ICreatureCombat>();
            _movement = Substitute.For<IMovement>();
            _bounds = Substitute.For<IBounds>();

            _serviceProvider.GetService(typeof(IPathFinderProvider)).Returns(_pathFinderProvider);
            _serviceProvider.GetService(typeof(IRandomProvider)).Returns(_randomProvider);
            _pathFinderProvider.Simple.Returns(_pathFinder);

            _npc.ServiceProvider.Returns(_serviceProvider);
            _npc.Combat.Returns(_combat);
            _npc.Movement.Returns(_movement);
            _npc.Bounds.Returns(_bounds);
        }

        [TestMethod]
        public void PerformTick_NpcMovementLocked_DoesNotMove()
        {
            _npc.Movement.Locked.Returns(true);

            var task = new NpcRandomWalkTask(_npc);
            task.Tick();

            _randomProvider.DidNotReceive().NextDouble();
            _pathFinder.DidNotReceiveWithAnyArgs().Find((IEntity)default, (IVector3)default, default);
        }

        [TestMethod]
        public void PerformTick_NpcMoving_DoesNotMove()
        {
            _npc.Movement.Moving.Returns(true);

            var task = new NpcRandomWalkTask(_npc);
            task.Tick();

            _randomProvider.DidNotReceive().NextDouble();
            _pathFinder.DidNotReceiveWithAnyArgs().Find((IEntity)default, (IVector3)default, default);
        }

        [TestMethod]
        public void PerformTick_NpcInCombat_DoesNotMove()
        {
            _npc.Combat.IsInCombat().Returns(true);

            var task = new NpcRandomWalkTask(_npc);
            task.Tick();

            _randomProvider.DidNotReceive().NextDouble();
            _pathFinder.DidNotReceiveWithAnyArgs().Find((IEntity)default, (IVector3)default, default);
        }

        [TestMethod]
        public void PerformTick_RandomCheckFails_DoesNotMove()
        {
            _randomProvider.NextDouble().Returns(0.02);

            var task = new NpcRandomWalkTask(_npc);
            task.Tick();

            _pathFinder.DidNotReceiveWithAnyArgs().Find((IEntity)default, (IVector3)default, default);
        }

        [TestMethod]
        public void PerformTick_RandomMoveIsZero_DoesNotMove()
        {
            _randomProvider.NextDouble().Returns(0.005);

            var task = new NpcRandomWalkTask(_npc);
            task.Tick();

            _pathFinder.DidNotReceiveWithAnyArgs().Find((IEntity)default, (IVector3)default, default);
        }

        [TestMethod]
        public void PerformTick_PathNotFound_DoesNotMove()
        {
            _randomProvider.NextDouble().Returns(0.005);
            _npc.Location.Returns(Location.Create(10, 10, 0));
            _npc.Bounds.MinimumLocation.Returns(Location.Create(0, 0, 0));
            _npc.Bounds.MaximumLocation.Returns(Location.Create(20, 20, 0));

            var path = Substitute.For<IPath>();
            path.Successful.Returns(false);
            _pathFinder.Find(Arg.Any<IEntity>(), Arg.Any<IVector3>(), Arg.Any<bool>()).Returns(path);

            var task = new NpcRandomWalkTask(_npc);
            task.Tick();

            _movement.DidNotReceive().ClearQueue();
        }

        [TestMethod]
        public void PerformTick_ValidPath_AddsToMovementQueue()
        {
            _randomProvider.NextDouble().Returns(0.005);
            _npc.Location.Returns(Location.Create(10, 10, 0));
            _npc.Bounds.MinimumLocation.Returns(Location.Create(0, 0, 0));
            _npc.Bounds.MaximumLocation.Returns(Location.Create(20, 20, 0));
            _npc.Bounds.InBounds(Arg.Any<Location>()).Returns(true);

            var path = Substitute.For<IPath>();
            path.Successful.Returns(true);
            var pathPoints = new List<IVector3> { Location.Create(11, 11, 0), Location.Create(12, 12, 0) };
            path.GetEnumerator().Returns(pathPoints.GetEnumerator());
            _pathFinder.Find(Arg.Any<IEntity>(), Arg.Any<IVector3>(), Arg.Any<bool>()).Returns(path);

            var task = new NpcRandomWalkTask(_npc);
            task.Tick();

            _movement.Received(1).ClearQueue();
            _movement.Received(1).MovementType = MovementType.Walk;
            _movement.Received(2).AddToQueue(Arg.Any<Location>());
        }

        [TestMethod]
        public void PerformTick_PathGoesOutOfBounds_StopsAddingToQueue()
        {
            _randomProvider.NextDouble().Returns(0.005);
            _npc.Location.Returns(Location.Create(10, 10, 0));
            _npc.Bounds.MinimumLocation.Returns(Location.Create(0, 0, 0));
            _npc.Bounds.MaximumLocation.Returns(Location.Create(11, 11, 0));
            _npc.Bounds.InBounds(Location.Create(11, 11, 0, 0)).Returns(true);
            _npc.Bounds.InBounds(Location.Create(12, 12, 0, 0)).Returns(false);

            var path = Substitute.For<IPath>();
            path.Successful.Returns(true);
            var pathPoints = new List<IVector3> { Location.Create(11, 11, 0), Location.Create(12, 12, 0) };
            path.GetEnumerator().Returns(pathPoints.GetEnumerator());

            _pathFinder.Find(Arg.Any<IEntity>(), Arg.Any<IVector3>(), Arg.Any<bool>()).Returns(path);

            var task = new NpcRandomWalkTask(_npc);
            task.Tick();

            _movement.Received(1).ClearQueue();
            _movement.Received(1).MovementType = MovementType.Walk;
            _movement.Received(1).AddToQueue(Arg.Any<Location>());
        }
    }
}
