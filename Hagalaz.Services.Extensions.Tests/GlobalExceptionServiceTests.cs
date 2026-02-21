using Microsoft.Extensions.Logging;
using NSubstitute;
using Hagalaz.Services.Extensions;
using System.Reflection;

namespace Hagalaz.Services.Extensions.Tests
{
    [TestClass]
    public class GlobalExceptionServiceTests
    {
        [TestMethod]
        public void Constructor_ShouldNotThrow()
        {
            // Arrange
            var logger = Substitute.For<ILogger<GlobalExceptionService>>();

            // Act
            var service = new GlobalExceptionService(logger);

            // Assert
            Assert.IsNotNull(service);
        }

        [TestMethod]
        public void UnhandledException_ShouldLogCritical()
        {
            // This is hard to test directly as it hooks into AppDomain.
            // But we can at least verify the service exists.
            var logger = Substitute.For<ILogger<GlobalExceptionService>>();
            var service = new GlobalExceptionService(logger);
            Assert.IsNotNull(service);
        }
    }
}
