using System;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Game.Abstractions.Tests.Tasks
{
    [TestClass]
    public class RsAsyncTaskTests
    {
        [TestMethod]
        public async Task Tick_DoesNotBlockWhileOperationIsPending()
        {
            var context = new GameLoopSynchronizationContext();
            var operation = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var resumed = false;
            var task = new RsAsyncTask(async () =>
            {
                await operation.Task;
                resumed = true;
            });

            var previousContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(context);
            try
            {
                task.Tick();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previousContext);
            }

            Assert.IsFalse(task.IsCompleted);
            Assert.IsFalse(resumed);

            operation.SetResult(true);
            await Task.Delay(10);
            Assert.IsFalse(resumed);

            context.RunPending();
            task.Tick();

            Assert.IsTrue(task.IsCompleted);
            Assert.IsTrue(resumed);
        }

        [TestMethod]
        public async Task Cancel_DoesNotSuppressPendingContinuation()
        {
            var context = new GameLoopSynchronizationContext();
            var operation = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var cleanupCompleted = false;
            var task = new RsAsyncTask(async () =>
            {
                try
                {
                    await operation.Task;
                }
                finally
                {
                    cleanupCompleted = true;
                }
            });

            var previousContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(context);
            try
            {
                task.Tick();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previousContext);
            }

            task.Cancel();

            Assert.IsFalse(task.IsCancelled);

            operation.SetResult(true);
            await Task.Delay(10);
            Assert.IsFalse(cleanupCompleted);

            context.RunPending();
            task.Tick();

            Assert.IsTrue(cleanupCompleted);
            Assert.IsTrue(task.IsCompleted);
            Assert.IsFalse(task.IsCancelled);
        }

        [TestMethod]
        public async Task Cancel_CooperativeOperationBecomesCanceledAfterItObservesToken()
        {
            var context = new GameLoopSynchronizationContext();
            var operation = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var cancellationObserved = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var task = new RsAsyncTask(async cancellationToken =>
            {
                try
                {
                    await operation.Task;
                    cancellationToken.ThrowIfCancellationRequested();
                }
                catch (OperationCanceledException)
                {
                    cancellationObserved.SetResult(true);
                    throw;
                }
            });

            var previousContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(context);
            try
            {
                task.Tick();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previousContext);
            }

            task.Cancel();
            Assert.IsFalse(task.IsCancelled);

            operation.SetResult(true);
            await Task.Delay(10);
            Assert.IsFalse(task.IsCancelled);

            context.RunPending();
            await cancellationObserved.Task.WaitAsync(TimeSpan.FromSeconds(1));
            await Task.Delay(10);

            Assert.IsTrue(task.IsCancelled);
            Assert.IsFalse(task.IsCompleted);
        }

        [TestMethod]
        public void RunPending_DefersContinuationsPostedDuringTheBatch()
        {
            var context = new GameLoopSynchronizationContext();
            var firstRan = false;
            var secondRan = false;

            context.Post(_ =>
            {
                firstRan = true;
                context.Post(__ => secondRan = true, null);
            }, null);

            context.RunPending();

            Assert.IsTrue(firstRan);
            Assert.IsFalse(secondRan);

            context.RunPending();

            Assert.IsTrue(secondRan);
        }

        [TestMethod]
        public void FaultedOperation_MarksTaskFaultedAndRethrows()
        {
            var exception = new InvalidOperationException("Test exception");
            var task = new RsAsyncTask(() => Task.FromException(exception));

            var thrown = Assert.ThrowsExactly<InvalidOperationException>(() => task.Tick());

            Assert.AreSame(exception, thrown);
            Assert.IsTrue(task.IsFaulted);
            Assert.IsFalse(task.IsCompleted);
        }
    }
}
