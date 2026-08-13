using System;
using System.Threading;
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
            var context = new GameLoopSynchronizationContext();
            var creature = Substitute.For<ICreature>();
            ITaskItem? queuedTask = null;
            creature.When(x => x.QueueTask(Arg.Any<ITaskItem>())).Do(callInfo =>
            {
                queuedTask = callInfo.Arg<ITaskItem>();
            });

            var operationStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var operation = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var operationCompleted = false;
            creature.QueueTask(async () =>
            {
                operationStarted.SetResult(true);
                await operation.Task;
                operationCompleted = true;
            });

            Assert.IsNotNull(queuedTask);
            Assert.IsInstanceOfType(queuedTask, typeof(RsAsyncTask));
            var previousContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(context);
            try
            {
                queuedTask.Tick();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previousContext);
            }

            await operationStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
            Assert.IsFalse(operationCompleted);
            Assert.IsFalse(queuedTask.IsCompleted);

            operation.SetResult(true);
            await Task.Delay(10);
            Assert.IsFalse(operationCompleted);
            context.RunPending();
            queuedTask.Tick();

            Assert.IsTrue(operationCompleted);
            Assert.IsTrue(queuedTask.IsCompleted);
        }

        [TestMethod]
        public void QueueTask_WithCancellationAwareAsyncOperation_QueuesRsAsyncTask()
        {
            var creature = Substitute.For<ICreature>();
            ITaskItem? queuedTask = null;
            creature.When(x => x.QueueTask(Arg.Any<ITaskItem>())).Do(callInfo =>
            {
                queuedTask = callInfo.Arg<ITaskItem>();
            });

            creature.QueueTask(_ => Task.CompletedTask);

            Assert.IsNotNull(queuedTask);
            Assert.IsInstanceOfType(queuedTask, typeof(RsAsyncTask));
        }

        [TestMethod]
        public void QueueTask_WithExternalCancellation_PreventsOperationFromStarting()
        {
            var creature = Substitute.For<ICreature>();
            ITaskItem? queuedTask = null;
            creature.When(x => x.QueueTask(Arg.Any<ITaskItem>())).Do(callInfo =>
            {
                queuedTask = callInfo.Arg<ITaskItem>();
            });

            using var cancellation = new CancellationTokenSource();
            var operationStarted = false;
            creature.QueueTask(_ =>
            {
                operationStarted = true;
                return Task.CompletedTask;
            }, cancellation.Token);

            cancellation.Cancel();
            queuedTask!.Tick();

            Assert.IsFalse(operationStarted);
            Assert.IsTrue(queuedTask.IsCancelled);
        }
    }
}
