using System.Net;
using System.Text;
using Hagalaz.Services.Authorization.Api;
using Hagalaz.Services.Authorization.Config;
using Hagalaz.Services.Authorization.Services;
using Microsoft.Extensions.Options;
using Refit;

namespace Hagalaz.Services.Authorization.Tests
{
    [TestClass]
    public class HCaptchaServiceTests
    {
        [TestMethod]
        public async Task Verify_WithRemoteIp_PostsUrlEncodedFormAndReturnsProviderResult()
        {
            // Arrange
            var handler = new CaptchaHttpMessageHandler(
                """{"success":true,"challenge_ts":"2026-10-27T12:00:00Z","hostname":"hcaptcha.test","credit":false}""");
            using var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://hcaptcha.test")
            };
            var captchaApi = RestService.For<IHCaptchaApi>(httpClient);
            var service = new HCaptchaService(captchaApi, Options.Create(CreateOptions()));

            // Act
            var result = await service.Verify("token+with&reserved=chars", "2001:db8::1");

            // Assert
            Assert.AreEqual(HttpMethod.Post, handler.RequestMethod);
            Assert.AreEqual(new Uri("https://hcaptcha.test/siteverify"), handler.RequestUri);
            Assert.AreEqual("application/x-www-form-urlencoded", handler.ContentType);
            var form = ParseForm(handler.RequestBody!);
            Assert.AreEqual("test-secret", form["secret"]);
            Assert.AreEqual("token+with&reserved=chars", form["response"]);
            Assert.AreEqual("2001:db8::1", form["remoteip"]);
            Assert.IsTrue(result.Success);
            Assert.AreEqual("hcaptcha.test", result.HostName);
        }

        [TestMethod]
        public async Task Verify_WithoutRemoteIp_DeserializesRejectedResponseWithoutHostname()
        {
            // Arrange
            var handler = new CaptchaHttpMessageHandler(
                """{"success":false,"error-codes":["invalid-input-response"]}""");
            using var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://hcaptcha.test")
            };
            var captchaApi = RestService.For<IHCaptchaApi>(httpClient);
            var service = new HCaptchaService(captchaApi, Options.Create(CreateOptions()));

            // Act
            var result = await service.Verify("invalid-token");

            // Assert
            var form = ParseForm(handler.RequestBody!);
            CollectionAssert.AreEquivalent(new[] { "secret", "response" }, form.Keys.ToArray());
            Assert.AreEqual("test-secret", form["secret"]);
            Assert.AreEqual("invalid-token", form["response"]);
            Assert.IsFalse(result.Success);
            Assert.IsNull(result.HostName);
            CollectionAssert.AreEqual(new[] { "invalid-input-response" }, result.ErrorCodes!);
        }

        private static CaptchaOptions CreateOptions() => new()
        {
            ApiBaseUrl = "https://hcaptcha.test",
            SiteKey = "test-site-key",
            Secret = "test-secret"
        };

        private static Dictionary<string, string> ParseForm(string body)
        {
            return body.Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(field => field.Split('=', 2))
                .ToDictionary(
                    parts => WebUtility.UrlDecode(parts[0]),
                    parts => WebUtility.UrlDecode(parts.Length > 1 ? parts[1] : string.Empty),
                    StringComparer.Ordinal);
        }

        private sealed class CaptchaHttpMessageHandler : HttpMessageHandler
        {
            private readonly string _responseJson;

            public CaptchaHttpMessageHandler(string responseJson)
            {
                _responseJson = responseJson;
            }

            public HttpMethod? RequestMethod { get; private set; }
            public Uri? RequestUri { get; private set; }
            public string? ContentType { get; private set; }
            public string? RequestBody { get; private set; }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                RequestMethod = request.Method;
                RequestUri = request.RequestUri;
                ContentType = request.Content?.Headers.ContentType?.MediaType;
                RequestBody = request.Content is null
                    ? null
                    : await request.Content.ReadAsStringAsync(cancellationToken);

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(_responseJson, Encoding.UTF8, "application/json")
                };
            }
        }
    }
}
