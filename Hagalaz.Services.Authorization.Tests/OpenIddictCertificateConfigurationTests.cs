using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Hagalaz.Services.Authorization.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenIddictServerOptions = OpenIddict.Server.OpenIddictServerOptions;

namespace Hagalaz.Services.Authorization.Tests
{
    [TestClass]
    public class OpenIddictCertificateConfigurationTests
    {
        [TestMethod]
        public void ConfigureIssuer_UsesConfiguredIssuer()
        {
            var services = new ServiceCollection();
            services.AddOptions();
            var options = new OpenIddictServerBuilder(services);
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["OpenIddict:Issuer"] = "https://auth.example.test/"
                })
                .Build();

            OpenIddictServerConfiguration.ConfigureIssuer(options, configuration, isDevelopment: false);

            var serverOptions = services.BuildServiceProvider()
                .GetRequiredService<IOptions<OpenIddictServerOptions>>().Value;
            Assert.AreEqual(new Uri("https://auth.example.test/"), serverOptions.Issuer);
        }

        [TestMethod]
        public void ConfigureIssuer_Development_UsesHttpsPortFallback()
        {
            var services = new ServiceCollection();
            services.AddOptions();
            var options = new OpenIddictServerBuilder(services);
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ASPNETCORE_HTTPS_PORT"] = "7006"
                })
                .Build();

            OpenIddictServerConfiguration.ConfigureIssuer(options, configuration, isDevelopment: true);

            var serverOptions = services.BuildServiceProvider()
                .GetRequiredService<IOptions<OpenIddictServerOptions>>().Value;
            Assert.AreEqual(new Uri("https://localhost:7006/"), serverOptions.Issuer);
        }

        [TestMethod]
        public void ConfigureIssuer_Development_PrefersHttpsPortOverHttpPort()
        {
            var services = new ServiceCollection();
            services.AddOptions();
            var options = new OpenIddictServerBuilder(services);
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ASPNETCORE_HTTPS_PORT"] = "7006",
                    ["ASPNETCORE_HTTP_PORT"] = "5009"
                })
                .Build();

            OpenIddictServerConfiguration.ConfigureIssuer(options, configuration, isDevelopment: true);

            var serverOptions = services.BuildServiceProvider()
                .GetRequiredService<IOptions<OpenIddictServerOptions>>().Value;
            Assert.AreEqual(new Uri("https://localhost:7006/"), serverOptions.Issuer);
        }

        [TestMethod]
        public void ConfigureIssuer_Development_UsesHttpPortFallback()
        {
            var services = new ServiceCollection();
            services.AddOptions();
            var options = new OpenIddictServerBuilder(services);
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ASPNETCORE_HTTP_PORT"] = "5009"
                })
                .Build();

            OpenIddictServerConfiguration.ConfigureIssuer(options, configuration, isDevelopment: true);

            var serverOptions = services.BuildServiceProvider()
                .GetRequiredService<IOptions<OpenIddictServerOptions>>().Value;
            Assert.AreEqual(new Uri("http://localhost:5009/"), serverOptions.Issuer);
        }

        [TestMethod]
        public void ConfigureIssuer_Development_ThrowsWhenNoLaunchProfilePortExists()
        {
            var services = new ServiceCollection();
            var options = new OpenIddictServerBuilder(services);
            var configuration = new ConfigurationBuilder().Build();

            var exception = Assert.Throws<InvalidOperationException>(() =>
                OpenIddictServerConfiguration.ConfigureIssuer(options, configuration, isDevelopment: true));

            StringAssert.Contains(exception.Message, "OpenIddict:Issuer");
        }

        [TestMethod]
        public void ConfigureIssuer_Production_PreservesConfiguredHttpsPath()
        {
            var services = new ServiceCollection();
            services.AddOptions();
            var options = new OpenIddictServerBuilder(services);
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["OpenIddict:Issuer"] = "https://auth.example.test/identity/"
                })
                .Build();

            OpenIddictServerConfiguration.ConfigureIssuer(options, configuration, isDevelopment: false);

            var serverOptions = services.BuildServiceProvider()
                .GetRequiredService<IOptions<OpenIddictServerOptions>>().Value;
            Assert.AreEqual(new Uri("https://auth.example.test/identity/"), serverOptions.Issuer);
        }

        [TestMethod]
        public void ConfigureIssuer_RejectsRelativeIssuer()
        {
            var services = new ServiceCollection();
            var options = new OpenIddictServerBuilder(services);
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["OpenIddict:Issuer"] = "auth.example.test"
                })
                .Build();

            var exception = Assert.Throws<InvalidOperationException>(() =>
                OpenIddictServerConfiguration.ConfigureIssuer(options, configuration, isDevelopment: false));

            StringAssert.Contains(exception.Message, "OpenIddict:Issuer");
        }

        [TestMethod]
        public void ConfigureIssuer_RejectsQueryInIssuer()
        {
            var services = new ServiceCollection();
            var options = new OpenIddictServerBuilder(services);
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["OpenIddict:Issuer"] = "https://auth.example.test/?tenant=one"
                })
                .Build();

            var exception = Assert.Throws<InvalidOperationException>(() =>
                OpenIddictServerConfiguration.ConfigureIssuer(options, configuration, isDevelopment: false));

            StringAssert.Contains(exception.Message, "query or fragment");
        }

        [TestMethod]
        public void ConfigureIssuer_RejectsFragmentInIssuer()
        {
            var services = new ServiceCollection();
            var options = new OpenIddictServerBuilder(services);
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["OpenIddict:Issuer"] = "https://auth.example.test/#issuer"
                })
                .Build();

            var exception = Assert.Throws<InvalidOperationException>(() =>
                OpenIddictServerConfiguration.ConfigureIssuer(options, configuration, isDevelopment: false));

            StringAssert.Contains(exception.Message, "query or fragment");
        }

        [TestMethod]
        public void ConfigureIssuer_Production_ThrowsWhenIssuerIsMissing()
        {
            var services = new ServiceCollection();
            var options = new OpenIddictServerBuilder(services);
            var configuration = new ConfigurationBuilder().Build();

            var exception = Assert.Throws<InvalidOperationException>(() =>
                OpenIddictServerConfiguration.ConfigureIssuer(options, configuration, isDevelopment: false));

            StringAssert.Contains(exception.Message, "OpenIddict:Issuer");
        }

        [TestMethod]
        public void ConfigureIssuer_Production_RejectsNonHttpsIssuer()
        {
            var services = new ServiceCollection();
            var options = new OpenIddictServerBuilder(services);
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["OpenIddict:Issuer"] = "http://auth.example.test/"
                })
                .Build();

            var exception = Assert.Throws<InvalidOperationException>(() =>
                OpenIddictServerConfiguration.ConfigureIssuer(options, configuration, isDevelopment: false));

            StringAssert.Contains(exception.Message, "HTTPS");
        }

        [TestMethod]
        public void ConfigureCredentials_OutsideDevelopment_ThrowsWhenSigningCertificateIsMissing()
        {
            var services = new ServiceCollection();
            var options = new OpenIddictServerBuilder(services);
            var configuration = new ConfigurationBuilder().Build();

            var exception = Assert.Throws<InvalidOperationException>(() =>
                OpenIddictServerConfiguration.ConfigureCredentials(options, configuration, isDevelopment: false));

            StringAssert.Contains(exception.Message, "OpenIddict:SigningCertificate:Path");
        }

        [TestMethod]
        public void Load_ValidPkcs12Certificate_ReturnsCertificateWithPrivateKey()
        {
            var path = Path.Combine(Path.GetTempPath(), $"hagalaz-openiddict-{Guid.NewGuid():N}.pfx");

            try
            {
                using var key = RSA.Create(2048);
                var request = new CertificateRequest(
                    "CN=Hagalaz OpenIddict test",
                    key,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);
                using var source = request.CreateSelfSigned(
                    DateTimeOffset.UtcNow.AddMinutes(-1),
                    DateTimeOffset.UtcNow.AddMinutes(5));
                File.WriteAllBytes(path, source.Export(X509ContentType.Pkcs12, "test-password"));

                var configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["OpenIddict:SigningCertificate:Path"] = path,
                        ["OpenIddict:SigningCertificate:Password"] = "test-password"
                    })
                    .Build();

                using var loaded = OpenIddictCertificateLoader.Load(
                    configuration,
                    "OpenIddict:SigningCertificate");

                Assert.IsTrue(loaded.HasPrivateKey);
                Assert.AreEqual(source.Thumbprint, loaded.Thumbprint);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
