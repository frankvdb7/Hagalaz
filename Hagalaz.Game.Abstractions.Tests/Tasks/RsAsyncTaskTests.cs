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
        public async Task Cancel_PreventsPendingContinuationFromResuming()
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

            task.Cancel();

            Assert.IsTrue(task.IsCancelled);

            operation.SetResult(true);
            await Task.Delay(10);
            context.RunPending();
            task.Tick();

            Assert.IsFalse(resumed);
            Assert.IsFalse(task.IsCompleted);
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
