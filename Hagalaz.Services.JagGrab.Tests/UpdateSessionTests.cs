using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Hagalaz.Services.JagGrab.Network;
using Hagalaz.Services.JagGrab;
using Xunit;

namespace Hagalaz.Services.JagGrab.Tests
{
    public class UpdateSessionTests
    {
        [Fact]
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
            Assert.NotNull(session);
        }

        [Fact]
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
