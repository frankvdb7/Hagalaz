using System.Threading.Tasks;
using Hagalaz.Services.Authorization.Model;
using Hagalaz.Services.Authorization.Services;
using Microsoft.AspNetCore.Mvc;

namespace Hagalaz.Services.Authorization.Controllers
{
    [ApiController]
    public class CaptchaController : Controller
    {
        private readonly ICaptchaService _captchaService;

        public CaptchaController(ICaptchaService captchaService)
        {
            _captchaService = captchaService;
        }

        [HttpPost("~/captcha/verify"), Produces("application/json")]
        public async Task<ActionResult<CaptchaVerifyResult>> VerifyCaptcha([FromBody] CaptchaVerifyRequest request)
        {
            var result = await _captchaService.Verify(request.Token, HttpContext.Connection.RemoteIpAddress?.ToString());
            return result.Success ? CaptchaVerifyResult.Success : CaptchaVerifyResult.Fail;
        }
    }
}