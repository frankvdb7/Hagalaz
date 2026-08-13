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
        public async Task QueueAsyncTask_DoesNotBlockWhilePreparationIsPending()
        {
            var creature = Substitute.For<ICreature>();
            ITaskItem? initialTask = null;
            var continuationTask = new TaskCompletionSource<ITaskItem>(TaskCreationOptions.RunContinuationsAsynchronously);
            creature.When(x => x.QueueTask(Arg.Any<ITaskItem>())).Do(callInfo =>
            {
                var task = callInfo.Arg<ITaskItem>();
                if (initialTask is null)
                {
                    initialTask = task;
                }
                else
                {
                    continuationTask.TrySetResult(task!);
                }
            });

            var preparationStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var preparation = new TaskCompletionSource<Action?>(TaskCreationOptions.RunContinuationsAsynchronously);
            creature.QueueAsyncTask(() =>
            {
                preparationStarted.SetResult(true);
                return preparation.Task;
            });

            Assert.IsNotNull(initialTask);
            Assert.IsInstanceOfType(initialTask, typeof(RsTask));
            Assert.IsNotInstanceOfType(initialTask, typeof(RsAsyncTask));
            await Task.Run(() => initialTask.Tick()).WaitAsync(TimeSpan.FromSeconds(1));
            await preparationStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
            Assert.IsFalse(continuationTask.Task.IsCompleted);

            var invoked = false;
            preparation.SetResult(() => invoked = true);
            var continuation = await continuationTask.Task.WaitAsync(TimeSpan.FromSeconds(1));
            continuation.Tick();

            Assert.IsTrue(invoked);
        }
    }
}
