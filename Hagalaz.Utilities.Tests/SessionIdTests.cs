using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Utilities.Tests
{
    [TestClass]
    public class SessionIdTests
    {
        [TestMethod]
        public void NewId_ReturnsNonZeroValue()
        {
            // Act
            var newId = SessionId.NewId();

            // Assert
            Assert.AreNotEqual(0, newId);
        }
    }
}