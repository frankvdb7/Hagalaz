using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Raido.Common.Protocol;

namespace Raido.Common.Tests.Protocol
{
    [TestClass]
    public class RaidoMessageHandlerAttributeTests
    {
        [TestMethod]
        public void RaidoMessageHandlerAttribute_Constructor_SetsMessageProperty()
        {
            // Arrange
            var messageType = typeof(string);

            // Act
            var attribute = new RaidoMessageHandlerAttribute(messageType);

            // Assert
            Assert.AreEqual(messageType, attribute.Message);
        }
    }
}
