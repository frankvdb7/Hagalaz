using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Hagalaz.Services.Authorization.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.Authorization.Tests
{
    [TestClass]
    public class OpenIddictCertificateConfigurationTests
    {
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
