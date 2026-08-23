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
        private sealed class DisposableTask : ITaskItem, IDisposable
        {
            public bool IsCancelled { get; set; }
            public bool IsCompleted { get; set; }
            public bool IsFaulted { get; set; }
            public bool IsDisposed { get; private set; }

            public void Tick() { }

            public void Cancel() => IsCancelled = true;

            public void Dispose() => IsDisposed = true;
        }

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
        public void Tick_RemovingTerminalDisposableTask_DisposesTask()
        {
            var taskService = new RsTaskService(new NullLogger<RsTaskService>());
            var task = new DisposableTask { IsCompleted = true };
            taskService.Schedule(task);

            taskService.Tick();

            Assert.IsTrue(task.IsDisposed);
            Assert.IsEmpty(taskService.Tasks);
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

        [TestMethod]
        public async Task Tick_ResumesAsyncTaskOnTheGameLoop()
        {
            var taskService = new RsTaskService(new NullLogger<RsTaskService>());
            var operation = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var resumed = false;
            var resumedThreadId = 0;
            var task = new RsAsyncTask(async () =>
            {
                await operation.Task;
                resumed = true;
                resumedThreadId = Environment.CurrentManagedThreadId;
            });

            taskService.Schedule(task);
            taskService.Tick();

            Assert.IsFalse(resumed);
            Assert.IsFalse(task.IsCompleted);

            operation.SetResult(true);
            await Task.Delay(10);

            Assert.IsFalse(resumed);
            var continuationTickThreadId = Environment.CurrentManagedThreadId;
            taskService.Tick();

            Assert.IsTrue(resumed);
            Assert.AreEqual(continuationTickThreadId, resumedThreadId);
            Assert.IsTrue(task.IsCompleted);
        }

        [TestMethod]
        public void Tick_TaskScheduledByTaskProcessing_WaitsUntilNextTick()
        {
            var taskService = new RsTaskService(new NullLogger<RsTaskService>());
            var executed = 0;
            taskService.Schedule(new RsTask(() =>
            {
                taskService.Schedule(new RsTask(() => executed++, executeDelay: 1));
            }, executeDelay: 1));

            taskService.Tick();

            Assert.AreEqual(0, executed);

            taskService.Tick();

            Assert.AreEqual(1, executed);
        }

        [TestMethod]
        public async Task Tick_TaskScheduledByAsyncContinuation_WaitsUntilNextTick()
        {
            var taskService = new RsTaskService(new NullLogger<RsTaskService>());
            var operation = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var executed = 0;
            var asyncTask = new RsAsyncTask(async () =>
            {
                await operation.Task;
                taskService.Schedule(new RsTask(() => executed++, executeDelay: 1));
            });
            taskService.Schedule(asyncTask);

            taskService.Tick();

            operation.SetResult(true);
            await Task.Delay(10);

            taskService.Tick();

            Assert.AreEqual(0, executed);

            taskService.Tick();

            Assert.AreEqual(1, executed);
        }

        [TestMethod]
        public void Tick_WhenTaskIsCanceled_ContinuesProcessingOtherTasks()
        {
            var taskService = new RsTaskService(new NullLogger<RsTaskService>());
            var canceledTask = Substitute.For<ITaskItem>();
            canceledTask.When(x => x.Tick()).Do(_ => throw new OperationCanceledException());
            var otherTaskExecuted = 0;

            taskService.Schedule(canceledTask);
            taskService.Schedule(new RsTask(() => otherTaskExecuted++, executeDelay: 1));

            taskService.Tick();

            Assert.AreEqual(1, otherTaskExecuted);
        }
    }
}
