using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hagalaz.Utilities;

namespace Hagalaz.Utilities.Tests
{
    [TestClass]
    public class SessionIdTests
    {
        [TestMethod]
        public void NewId_CalledMultipleTimes_ReturnsDifferentIds()
        {
            // Act
            var id1 = SessionId.NewId();
            var id2 = SessionId.NewId();

            // Assert
            Assert.AreNotEqual(id1, id2);
        }
    }
}