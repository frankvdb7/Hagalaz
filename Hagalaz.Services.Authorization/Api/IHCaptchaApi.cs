using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Services.Authorization.Model;
using Refit;

namespace Hagalaz.Services.Authorization.Api
{
    public interface IHCaptchaApi
    {
        /// <summary>
        /// The endpoint expects a URL-encoded form POST containing the secret API key and hCaptcha response token. The user's IP address may be included as an additional security check.
        /// </summary>
        [Post("/siteverify")]
        Task<HCaptchaVerifyResult> Verify(
            [Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, object> form,
            CancellationToken cancellationToken = default);
    }
}
