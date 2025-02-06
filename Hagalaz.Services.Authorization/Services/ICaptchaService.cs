using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Services.Authorization.Model;

namespace Hagalaz.Services.Authorization.Services
{
    public interface ICaptchaService
    {
        /// <summary>
        /// Requests the hCaptcha API. Timeout configuration is provided via <paramref name="cancellationToken"/>
        /// </summary>
        /// <param name="token">The client's token.</param>
        /// <param name="remoteIp">Optional the client's IP address</param>
        /// <param name="cancellationToken"></param>
        Task<HCaptchaVerifyResult> Verify(string token, string? remoteIp = null, CancellationToken cancellationToken = default);
    }
}