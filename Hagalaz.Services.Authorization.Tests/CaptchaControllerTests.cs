using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Services.Authorization.Controllers;
using Hagalaz.Services.Authorization.Model;
using Hagalaz.Services.Authorization.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Hagalaz.Services.Authorization.Tests.Controllers
{
    [TestClass]
    public class CaptchaControllerTests
    {
        private Mock<ICaptchaService> _captchaServiceMock = null!;
        private CaptchaController _controller = null!;

        [TestInitialize]
        public void Initialize()
        {
            _captchaServiceMock = new Mock<ICaptchaService>();
            _controller = new CaptchaController(_captchaServiceMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        [TestMethod]
        public async Task VerifyCaptcha_WithNullRequest_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.VerifyCaptcha(null!);

            // Assert
            Assert.IsInstanceOfType(result.Result, typeof(BadRequestResult));
            _captchaServiceMock.Verify(
                service => service.Verify(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [TestMethod]
        public void VerifyCaptcha_DoesNotRequireAntiforgeryValidation()
        {
            // Assert
            var action = typeof(CaptchaController).GetMethod(nameof(CaptchaController.VerifyCaptcha));
            Assert.IsNotNull(action);
            Assert.IsFalse(Attribute.IsDefined(action, typeof(ValidateAntiForgeryTokenAttribute), inherit: true));
        }

        [TestMethod]
        public async Task VerifyCaptcha_WithSuccessfulVerification_ForwardsTokenAndRemoteIpAndReturnsSuccess()
        {
            // Arrange
            var request = new CaptchaVerifyRequest { Token = "valid-token" };
            _controller.HttpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("203.0.113.10");
            _captchaServiceMock.Setup(s => s.Verify(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HCaptchaVerifyResult { Success = true, HostName = "localhost" });

            // Act
            var result = await _controller.VerifyCaptcha(request);

            // Assert
            Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
            var okResult = (OkObjectResult)result.Result;
            Assert.AreEqual(CaptchaVerifyResult.Success, okResult.Value);
            _captchaServiceMock.Verify(
                service => service.Verify("valid-token", "203.0.113.10", It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task VerifyCaptcha_WithRejectedVerification_ReturnsFail()
        {
            // Arrange
            var request = new CaptchaVerifyRequest { Token = "invalid-token" };
            _captchaServiceMock.Setup(s => s.Verify(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HCaptchaVerifyResult { Success = false, ErrorCodes = ["invalid-input-response"] });

            // Act
            var result = await _controller.VerifyCaptcha(request);

            // Assert
            Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
            var okResult = (OkObjectResult)result.Result;
            Assert.AreEqual(CaptchaVerifyResult.Fail, okResult.Value);
            _captchaServiceMock.Verify(
                service => service.Verify("invalid-token", null, It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
