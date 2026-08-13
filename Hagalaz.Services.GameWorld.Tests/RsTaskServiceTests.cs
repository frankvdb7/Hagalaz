using System;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Services.GameWorld.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests
{
    [TestClass]
    public class RsTaskServiceTests
    {
        [TestMethod]
        public void Tick_WithMultipleCompletedTasks_RemovesAllCompletedTasks()
        {
            // Arrange
            var logger = new NullLogger<RsTaskService>();
            var taskService = new RsTaskService(logger);

            var task1 = Substitute.For<ITaskItem>();
            task1.IsCompleted.Returns(true);

            var task2 = Substitute.For<ITaskItem>();
            task2.IsCompleted.Returns(true);

            taskService.Schedule(task1);
            taskService.Schedule(task2);

            // Act
            taskService.Tick();

            // Assert
            var task3 = Substitute.For<ITaskItem>();
            taskService.Schedule(task3);
            taskService.Tick();

            Assert.HasCount(1, taskService.Tasks);
            Assert.AreSame(task3, taskService.Tasks[0]);
        }

        [TestMethod]
        public void Tick_WithDelayedTask_PreservesExecutionOrder()
        {
            // Arrange
            var logger = new NullLogger<RsTaskService>();
            var taskService = new RsTaskService(logger);
            var executionOrder = new List<int>();
            taskService.Schedule(new RsTask(() => executionOrder.Add(1), executeDelay: 2));

            // Act
            taskService.Tick();
            Assert.IsEmpty(executionOrder);
            taskService.Tick();

            // Assert
            CollectionAssert.AreEqual(new[] { 1 }, executionOrder);
        }

        [TestMethod]
        public async Task Schedule_FromAnotherThread_IsProcessedOnNextTick()
        {
            var logger = new NullLogger<RsTaskService>();
            var taskService = new RsTaskService(logger);
            using var entered = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            var firstTask = new RsTask(() =>
            {
                entered.Set();
                release.Wait();
            }, executeDelay: 1);
            taskService.Schedule(firstTask);

            var secondExecuted = 0;
            var tickTask = Task.Run(taskService.Tick);
            try
            {
                Assert.IsTrue(entered.Wait(TimeSpan.FromSeconds(5)));
                await Task.Run(() => taskService.Schedule(new RsTask(() => secondExecuted++, executeDelay: 1)));
                Assert.AreEqual(0, secondExecuted);
            }
            finally
            {
                release.Set();
                await tickTask;
            }

            taskService.Tick();

            Assert.AreEqual(1, secondExecuted);
        }
    }
}
