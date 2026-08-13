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
            var operation = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var result = 0;
            var task = new RsAsyncTask<int>(_ => operation.Task, value => result = value);

            await Task.Run(task.Tick).WaitAsync(TimeSpan.FromSeconds(1));

            Assert.IsFalse(task.IsCompleted);
            Assert.AreEqual(0, result);

            operation.SetResult(42);
            task.Tick();

            Assert.IsTrue(task.IsCompleted);
            Assert.AreEqual(42, result);
        }

        [TestMethod]
        public void Cancel_CancelsPreparationAndPreventsCompletion()
        {
            var cancellation = CancellationToken.None;
            var operation = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var result = 0;
            var task = new RsAsyncTask<int>(token =>
            {
                cancellation = token;
                return operation.Task;
            }, value => result = value);

            task.Tick();
            task.Cancel();

            Assert.IsTrue(task.IsCancelled);
            Assert.IsTrue(cancellation.IsCancellationRequested);

            operation.SetResult(42);
            task.Tick();

            Assert.AreEqual(0, result);
            Assert.IsFalse(task.IsCompleted);
        }

        [TestMethod]
        public void FaultedOperation_MarksTaskFaultedAndRethrows()
        {
            var exception = new InvalidOperationException("Test exception");
            var task = new RsAsyncTask<int>(_ => Task.FromException<int>(exception));

            var thrown = Assert.ThrowsExactly<InvalidOperationException>(() => task.Tick());

            Assert.AreSame(exception, thrown);
            Assert.IsTrue(task.IsFaulted);
            Assert.IsFalse(task.IsCompleted);
        }
    }
}
