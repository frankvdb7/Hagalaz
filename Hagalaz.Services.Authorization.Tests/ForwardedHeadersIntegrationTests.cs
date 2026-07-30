using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Hagalaz.ServiceDefaults;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.Authorization.Tests
{
    [TestClass]
    public class ForwardedHeadersIntegrationTests
    {
        [TestMethod]
        public void ProductionDefaults_RequireTrustedForwarderConfiguration()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = Environments.Production
            });

            var exception = Assert.Throws<InvalidOperationException>(() =>
                builder.AddServiceDefaults(requireTrustedForwardedHeaders: true));

            StringAssert.Contains(exception.Message, "ForwardedHeaders:KnownProxies");
        }

        [TestMethod]
        public async Task ProductionProxyRequest_UsesForwardedHttpsSchemeBeforeRedirection()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = Environments.Production
            });
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ForwardedHeaders:KnownNetworks:0"] = "127.0.0.0/8"
            });
            builder.AddServiceDefaults(requireTrustedForwardedHeaders: true);
            builder.Services.AddAuthentication();

            var app = builder.Build();
            app.UseServiceDefaults();
            app.MapGet("/test-scheme", (HttpContext context) => Results.Text(context.Request.Scheme));

            await app.StartAsync();
            try
            {
                using var handler = new HttpClientHandler { AllowAutoRedirect = false };
                using var client = new HttpClient(handler)
                {
                    BaseAddress = new System.Uri(app.Urls.Single())
                };
                client.DefaultRequestHeaders.TryAddWithoutValidation("X-Forwarded-Proto", "https");

                using var response = await client.GetAsync("/test-scheme");
                var scheme = await response.Content.ReadAsStringAsync();

                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                Assert.AreEqual("https", scheme);
            }
            finally
            {
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }
    }
}
