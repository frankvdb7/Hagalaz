using System;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Extensions.Tests
{
    [TestClass]
    public class CreatureExtensionsTests
    {
        [TestMethod]
        public void IsDead_WhenCombatIsDead_ReturnsTrue()
        {
            // Arrange
            var combat = Substitute.For<ICreatureCombat>();
            combat.IsDead.Returns(true);
            var creature = Substitute.For<ICreature>();
            creature.Combat.Returns(combat);

            // Act
            var isDead = creature.IsDead();

            // Assert
            Assert.IsTrue(isDead);
        }

        [TestMethod]
        public void IsDead_WhenCombatIsAlive_ReturnsFalse()
        {
            // Arrange
            var combat = Substitute.For<ICreatureCombat>();
            combat.IsDead.Returns(false);
            var creature = Substitute.For<ICreature>();
            creature.Combat.Returns(combat);

            // Act
            var isDead = creature.IsDead();

            // Assert
            Assert.IsFalse(isDead);
        }

        [TestMethod]
        public void IsAlive_WhenCombatIsAlive_ReturnsTrue()
        {
            // Arrange
            var combat = Substitute.For<ICreatureCombat>();
            combat.IsDead.Returns(false);
            var creature = Substitute.For<ICreature>();
            creature.Combat.Returns(combat);

            // Act
            var isAlive = creature.IsAlive();

            // Assert
            Assert.IsTrue(isAlive);
        }

        [TestMethod]
        public void IsAlive_WhenCombatIsDead_ReturnsFalse()
        {
            // Arrange
            var combat = Substitute.For<ICreatureCombat>();
            combat.IsDead.Returns(true);
            var creature = Substitute.For<ICreature>();
            creature.Combat.Returns(combat);

            // Act
            var isAlive = creature.IsAlive();

            // Assert
            Assert.IsFalse(isAlive);
        }

        [TestMethod]
        public async Task QueueTask_WithAsyncOperation_DoesNotBlockWhileOperationIsPending()
        {
            var creature = Substitute.For<ICreature>();
            ITaskItem? queuedTask = null;
            creature.When(x => x.QueueTask(Arg.Any<ITaskItem>())).Do(callInfo =>
            {
                queuedTask = callInfo.Arg<ITaskItem>();
            });

            var operationStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var operation = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var operationFinished = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var operationCompleted = false;
            creature.QueueTask(async () =>
            {
                operationStarted.SetResult(true);
                await operation.Task;
                operationCompleted = true;
                operationFinished.SetResult(true);
            });

            Assert.IsNotNull(queuedTask);
            Assert.IsInstanceOfType(queuedTask, typeof(RsAsyncTask));
            await Task.Run(() => queuedTask.Tick()).WaitAsync(TimeSpan.FromSeconds(1));
            await operationStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
            Assert.IsFalse(operationCompleted);
            Assert.IsFalse(queuedTask.IsCompleted);

            operation.SetResult(true);
            await operationFinished.Task.WaitAsync(TimeSpan.FromSeconds(1));
            queuedTask.Tick();

            Assert.IsTrue(operationCompleted);
            Assert.IsTrue(queuedTask.IsCompleted);
        }
    }
}
