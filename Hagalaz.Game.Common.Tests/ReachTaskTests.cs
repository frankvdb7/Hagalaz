using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Hagalaz.Game.Common.Tasks;

namespace Hagalaz.Game.Common.Tests
{
    [TestClass]
    public class ReachTaskTests
    {
        private class TestableReachTask : ReachTask
        {
            public TestableReachTask(Type[] conditions) : base(conditions)
            {
            }

            public bool PublicCanInterrupt(object source) => CanInterrupt(source);
        }

        [TestMethod]
        public void CanInterrupt_WithNullSource_ReturnsTrue()
        {
            // Arrange
            var task = new TestableReachTask(new Type[] { typeof(string) });

            // Act
            var result = task.PublicCanInterrupt(null);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void CanInterrupt_WithUnrelatedSource_ReturnsTrue()
        {
            // Arrange
            var task = new TestableReachTask(new Type[] { typeof(string) });

            // Act
            var result = task.PublicCanInterrupt(123);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void CanInterrupt_WithDirectlyBlockedSource_ReturnsFalse()
        {
            // Arrange
            var task = new TestableReachTask(new Type[] { typeof(string) });

            // Act
            var result = task.PublicCanInterrupt("test");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void CanInterrupt_WithDerivedBlockedSource_ReturnsFalse()
        {
            // Arrange
            var task = new TestableReachTask(new Type[] { typeof(Exception) });

            // Act
            var result = task.PublicCanInterrupt(new InvalidOperationException());

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void CanInterrupt_WithNoConditions_ReturnsTrue()
        {
            // Arrange
            var task = new TestableReachTask(new Type[0]);

            // Act
            var result = task.PublicCanInterrupt("test");

            // Assert
            Assert.IsTrue(result);
        }
    }
}
