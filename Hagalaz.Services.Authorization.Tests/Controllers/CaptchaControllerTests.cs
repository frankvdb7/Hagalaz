using System.Net;
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
        public void Setup()
        {
            _captchaServiceMock = new Mock<ICaptchaService>();
            _controller = new CaptchaController(_captchaServiceMock.Object);

            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [TestMethod]
        public async Task VerifyCaptcha_ReturnsSuccess_WhenVerificationSucceeds()
        {
            var request = new CaptchaVerifyRequest { Token = "valid-token" };
            _captchaServiceMock.Setup(x => x.Verify(request.Token, "127.0.0.1", default))
                .ReturnsAsync(new HCaptchaVerifyResult { Success = true, HostName = "localhost" });

            var result = await _controller.VerifyCaptcha(request);

            Assert.IsInstanceOfType(result.Value, typeof(CaptchaVerifyResult));
            Assert.IsTrue(result.Value!.Succeeded);
        }

        [TestMethod]
        public async Task VerifyCaptcha_ReturnsFail_WhenVerificationFails()
        {
            var request = new CaptchaVerifyRequest { Token = "invalid-token" };
            _captchaServiceMock.Setup(x => x.Verify(request.Token, "127.0.0.1", default))
                .ReturnsAsync(new HCaptchaVerifyResult { Success = false, HostName = "localhost" });

            var result = await _controller.VerifyCaptcha(request);

            Assert.IsInstanceOfType(result.Value, typeof(CaptchaVerifyResult));
            Assert.IsFalse(result.Value!.Succeeded);
        }
    }
}
