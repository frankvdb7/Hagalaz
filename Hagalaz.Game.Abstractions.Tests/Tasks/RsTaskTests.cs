using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Hagalaz.Game.Abstractions.Tasks;

namespace Hagalaz.Game.Abstractions.Tests.Tasks
{
    [TestClass]
    public class RsTaskTests
    {
        private class TestableRsTask : RsTask
        {
            public int Ticks { get; private set; }
            public TestableRsTask(Action executeHandler, int executeDelay) : base(executeHandler, executeDelay) { }
            public TestableRsTask() { }

            protected override void Execute()
            {
                Ticks++;
                base.Execute();
            }

            public void TestComplete() => Complete();
        }

        private class TestableRsTask<TResult> : RsTask<TResult>
        {
            public TestableRsTask(Func<TResult> executeFunc, int executeDelay, Action<TResult>? resultHandler = null)
                : base(executeFunc, executeDelay, resultHandler) { }
        }

        [TestMethod]
        public void RsTask_Constructor_InitializesCorrectly()
        {
            // Arrange
            var executed = false;
            Action executeHandler = () => executed = true;
            const int delay = 10;

            // Act
            var task = new TestableRsTask(executeHandler, delay);

            // Assert
            Assert.IsFalse(task.IsCancelled);
            Assert.IsFalse(task.IsCompleted);
            Assert.IsFalse(task.IsFaulted);
            Assert.AreEqual(delay, task.ExecuteDelay);
        }

        [TestMethod]
        public void Tick_ReducesDelay_And_ExecutesAtZero()
        {
            // Arrange
            var executed = false;
            Action executeHandler = () => executed = true;
            var task = new TestableRsTask(executeHandler, 2);

            // Act & Assert
            task.Tick(); // Delay becomes 1
            Assert.IsFalse(executed);
            Assert.AreEqual(1, task.ExecuteDelay);
            Assert.IsFalse(task.IsCompleted);

            task.Tick(); // Delay becomes 0, executes
            Assert.IsTrue(executed);
            Assert.AreEqual(0, task.ExecuteDelay);
            Assert.IsTrue(task.IsCompleted);
        }

        [TestMethod]
        public void Tick_DoesNothing_IfCancelled()
        {
            // Arrange
            var task = new TestableRsTask(() => { }, 1);
            task.Cancel();

            // Act
            task.Tick();

            // Assert
            Assert.AreEqual(1, task.ExecuteDelay);
            Assert.IsFalse(task.IsCompleted);
        }

        [TestMethod]
        public void Tick_DoesNothing_IfCompleted()
        {
            // Arrange
            var task = new TestableRsTask(() => { }, 0);
            task.TestComplete();

            // Act
            task.Tick();

            // Assert
            Assert.AreEqual(0, task.ExecuteDelay);
        }

        [TestMethod]
        public void Cancel_SetsIsCancelledFlag()
        {
            // Arrange
            var task = new TestableRsTask(() => { }, 1);

            // Act
            task.Cancel();

            // Assert
            Assert.IsTrue(task.IsCancelled);
        }

        [TestMethod]
        public void Execute_SetsIsCompletedFlag()
        {
            // Arrange
            var task = new TestableRsTask(() => { }, 1);

            // Act
            task.Tick();

            // Assert
            Assert.IsTrue(task.IsCompleted);
        }

        [TestMethod]
        public void Execute_ThatThrows_SetsIsFaultedAndRethrows()
        {
            // Arrange
            var exception = new InvalidOperationException("Test Exception");
            var task = new TestableRsTask(() => throw exception, 1);

            // Act & Assert
            var thrownException = Assert.ThrowsException<InvalidOperationException>(() => task.Tick());
            Assert.IsTrue(task.IsFaulted);
            Assert.IsFalse(task.IsCompleted);
            Assert.AreSame(exception, thrownException);
        }

        [TestMethod]
        public void Tick_DoesNotExecute_WhenDelayIsGreaterThanOne()
        {
            // Arrange
            var executed = false;
            var task = new TestableRsTask(() => executed = true, 2);

            // Act
            task.Tick();

            // Assert
            Assert.IsFalse(executed);
            Assert.AreEqual(1, task.ExecuteDelay);
            Assert.IsFalse(task.IsCompleted);
        }

        [TestMethod]
        public void Tick_ExecutesImmediately_WhenDelayIsZero()
        {
            // Arrange
            var executed = false;
            var task = new TestableRsTask(() => executed = true, 0);

            // Act
            task.Tick();

            // Assert
            Assert.IsTrue(executed);
            Assert.IsTrue(task.IsCompleted);
        }

        [TestMethod]
        public void Tick_ExecutesImmediately_WhenDelayIsOne()
        {
            // Arrange
            var executed = false;
            var task = new TestableRsTask(() => executed = true, 1);

            // Act
            task.Tick();

            // Assert
            Assert.IsTrue(executed);
            Assert.IsTrue(task.IsCompleted);
        }

        [TestMethod]
        public void Tick_DoesNothing_AfterCompletion()
        {
            // Arrange
            var executionCount = 0;
            var task = new TestableRsTask(() => executionCount++, 1);

            // Act
            task.Tick(); // Executes and completes
            task.Tick(); // Should do nothing

            // Assert
            Assert.AreEqual(1, executionCount);
        }

        [TestMethod]
        public void RsTaskTResult_ExecutesAndHandlesResult()
        {
            // Arrange
            const int expectedResult = 42;
            var actualResult = 0;
            var task = new TestableRsTask<int>(() => expectedResult, 1, result => actualResult = result);

            // Act
            task.Tick();

            // Assert
            Assert.IsTrue(task.IsCompleted);
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void RsTaskTResult_WithNoInitialHandler_ExecutesCorrectly()
        {
            // Arrange
            const int expectedResult = 42;
            var task = new TestableRsTask<int>(() => expectedResult, 1);

            // Act
            task.Tick();

            // Assert
            Assert.IsTrue(task.IsCompleted);
        }

        [TestMethod]
        public void RegisterResultHandler_ChainsMultipleHandlers()
        {
            // Arrange
            const int initialResult = 10;
            var firstHandlerResult = 0;
            var secondHandlerResult = 0;

            var task = new TestableRsTask<int>(() => initialResult, 1, result => firstHandlerResult = result);

            // Act
            task.RegisterResultHandler(result => secondHandlerResult = result * 2);
            task.Tick();

            // Assert
            Assert.AreEqual(initialResult, firstHandlerResult);
            Assert.AreEqual(initialResult * 2, secondHandlerResult);
        }
    }
}
