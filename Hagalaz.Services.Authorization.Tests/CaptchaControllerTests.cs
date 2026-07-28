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
        }

        [TestMethod]
        public async Task VerifyCaptcha_WithValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new CaptchaVerifyRequest { Token = "valid-token" };
            _captchaServiceMock.Setup(s => s.Verify(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HCaptchaVerifyResult { Success = true, HostName = "localhost" });

            // Act
            var result = await _controller.VerifyCaptcha(request);

            // Assert
            Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
            var okResult = (OkObjectResult)result.Result;
            Assert.AreEqual(CaptchaVerifyResult.Success, okResult.Value);
        }
    }
}
