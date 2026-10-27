using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Services.Authorization.Api;
using Hagalaz.Services.Authorization.Config;
using Hagalaz.Services.Authorization.Model;
using Microsoft.Extensions.Options;

namespace Hagalaz.Services.Authorization.Services
{
    public class HCaptchaService : ICaptchaService
    {
        private readonly IHCaptchaApi _captchaApi;
        private readonly IOptions<CaptchaOptions> _options;

        public HCaptchaService(IHCaptchaApi captchaApi, IOptions<CaptchaOptions> options)
        {
            _captchaApi = captchaApi;
            _options = options;
        }

        public async Task<HCaptchaVerifyResult> Verify(string token, string? remoteIp = null, CancellationToken cancellationToken = default)
        {
            var options = _options.Value;
            var form = new Dictionary<string, object>
            {
                ["secret"] = options.Secret,
                ["response"] = token
            };

            if (!string.IsNullOrWhiteSpace(remoteIp))
            {
                form["remoteip"] = remoteIp;
            }

            return await _captchaApi.Verify(form, cancellationToken);
        }
    }
}
