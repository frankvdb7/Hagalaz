using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Hagalaz.Services.JagGrab.Network;
using Hagalaz.Services.JagGrab;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.JagGrab.Tests
{
[TestClass]
    public class UpdateSessionTests
    {
        [TestMethod]
        public void Constructor_ShouldInitializeFields()
        {
            // Arrange
            var logger = Substitute.For<ILogger<UpdateSession>>();
            var options = Substitute.For<IOptions<JagGrabConfig>>();
            var rhLogger = Substitute.For<ILogger<RequestHandler>>();
            var requestHandler = new RequestHandler(options, rhLogger);

            // Act
            var session = new UpdateSession(requestHandler, logger);

            // Assert
            Assert.IsNotNull(session);
        }

        [TestMethod]
        public void Disconnect_ShouldNotThrow()
        {
            // Arrange
            var logger = Substitute.For<ILogger<UpdateSession>>();
            var options = Substitute.For<IOptions<JagGrabConfig>>();
            var rhLogger = Substitute.For<ILogger<RequestHandler>>();
            var requestHandler = new RequestHandler(options, rhLogger);
            var session = new UpdateSession(requestHandler, logger);

            // Act & Assert
            session.Disconnect();
        }
    }
}
