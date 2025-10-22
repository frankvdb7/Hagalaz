using Microsoft.VisualStudio.TestTools.UnitTesting;
using Raido.Common.Messages;

namespace Raido.Common.Tests.Messages
{
    [TestClass]
    public class PingMessageTests
    {
        [TestMethod]
        public void PingMessage_Instance_IsNotNull()
        {
            // Assert
            Assert.IsNotNull(PingMessage.Instance);
        }

        [TestMethod]
        public void PingMessage_Instance_IsSingleton()
        {
            // Act
            var instance1 = PingMessage.Instance;
            var instance2 = PingMessage.Instance;

            // Assert
            Assert.AreSame(instance1, instance2);
        }
    }
}
