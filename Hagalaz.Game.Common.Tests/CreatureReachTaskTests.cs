using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Common.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Common.Tests
{
    [TestClass]
    public class CreatureReachTaskTests
    {
        private ICreature _reacher = null!;
        private ICreature _target = null!;
        private ISmartPathFinder _pathFinder = null!;
        private IServiceProvider _serviceProvider = null!;

        [TestInitialize]
        public void Setup()
        {
            _reacher = Substitute.For<ICreature>();
            _target = Substitute.For<ICreature>();
            _pathFinder = Substitute.For<ISmartPathFinder>();
            _serviceProvider = Substitute.For<IServiceProvider>();

            var pathFinderProvider = Substitute.For<IPathFinderProvider>();
            pathFinderProvider.Smart.Returns(_pathFinder);
            _serviceProvider.GetService(typeof(IPathFinderProvider)).Returns(pathFinderProvider);
            _reacher.ServiceProvider.Returns(_serviceProvider);
            _reacher.Viewport.VisibleCreatures.Returns(new List<ICreature> { _target });
        }

        [TestMethod]
        public void PerformTickImpl_WhenAlreadyAtTarget_CompletesSuccessfully()
        {
            // Arrange
            var callbackCalled = false;
            var success = false;
            var task = new CreatureReachTask(_reacher, _target, (result) =>
            {
                callbackCalled = true;
                success = result;
            });
            var path = Substitute.For<IPath>();
            path.Successful.Returns(true);
            path.ReachedDestination.Returns(true);
            _pathFinder.Find(_reacher, _target, true).Returns(path);

            // Act
            task.Tick();

            // Assert
            Assert.IsTrue(callbackCalled);
            Assert.IsTrue(success);
        }

        [TestMethod]
        public void PerformTickImpl_WhenTargetNotReached_AddsPathToMovementQueue()
        {
            // Arrange
            var task = new CreatureReachTask(_reacher, _target, (result) => { });
            var path = Substitute.For<IPath>();
            path.Successful.Returns(true);
            path.MovedNear.Returns(false);
            path.ReachedDestination.Returns(false);
            _pathFinder.Find(_reacher, _target, true).Returns(path);

            // Act
            task.Tick();

            // Assert
            _reacher.Movement.Received(1).AddToQueue(path);
        }

        [TestMethod]
        public void PerformTickImpl_WhenPathIsUnsuccessful_CancelsAndCallsCallbackWithFalse()
        {
            // Arrange
            var callbackCalled = false;
            var success = true;
            var task = new CreatureReachTask(_reacher, _target, (result) =>
            {
                callbackCalled = true;
                success = result;
            });
            var path = Substitute.For<IPath>();
            path.Successful.Returns(false);
            _pathFinder.Find(_reacher, _target, true).Returns(path);

            // Act
            task.Tick();

            // Assert
            Assert.IsTrue(callbackCalled);
            Assert.IsFalse(success);
        }

        [TestMethod]
        public void CreatureReachTask_WhenInterrupted_IsCancelled()
        {
            // Arrange
            EventHappened<CreatureInterruptedEvent> handler = null;
            _reacher.RegisterEventHandler(Arg.Do<EventHappened<CreatureInterruptedEvent>>(h => handler = h));

            var task = new CreatureReachTask(_reacher, _target, (result) => { });

            // Act
            handler.Invoke(new CreatureInterruptedEvent(_reacher, new object()));

            // Assert
            Assert.IsTrue(task.IsCancelled);
        }
    }
}
