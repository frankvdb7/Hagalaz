using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Hagalaz.Services.Authorization.Config;
using Microsoft.Extensions.Configuration;

namespace Hagalaz.Services.Authorization.Services
{
    public static class OpenIddictCertificateLoader
    {
        public static X509Certificate2 Load(IConfiguration configuration, string sectionName)
        {
            var settings = configuration.GetSection(sectionName).Get<OpenIddictCertificateOptions>();
            if (settings is null || string.IsNullOrWhiteSpace(settings.Path))
            {
                throw new InvalidOperationException(
                    $"The OpenIddict certificate configuration '{sectionName}:Path' must be set outside the Development environment.");
            }

            try
            {
                var certificate = X509CertificateLoader.LoadPkcs12FromFile(
                    settings.Path,
                    settings.Password,
                    X509KeyStorageFlags.EphemeralKeySet);

                if (!certificate.HasPrivateKey)
                {
                    certificate.Dispose();
                    throw new InvalidOperationException(
                        $"The OpenIddict certificate configured at '{settings.Path}' must contain a private key.");
                }

                var now = DateTime.UtcNow;
                if (certificate.NotBefore.ToUniversalTime() > now || certificate.NotAfter.ToUniversalTime() <= now)
                {
                    certificate.Dispose();
                    throw new InvalidOperationException(
                        $"The OpenIddict certificate configured at '{settings.Path}' is not currently valid.");
                }

                return certificate;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (CryptographicException exception)
            {
                throw new InvalidOperationException(
                    $"The OpenIddict certificate configured at '{settings.Path}' could not be loaded. Verify that the path, password, and PKCS#12 file are valid.",
                    exception);
            }
            catch (IOException exception)
            {
                throw new InvalidOperationException(
                    $"The OpenIddict certificate configured at '{settings.Path}' could not be loaded. Verify that the path is readable by the service.",
                    exception);
            }
            catch (UnauthorizedAccessException exception)
            {
                throw new InvalidOperationException(
                    $"The OpenIddict certificate configured at '{settings.Path}' could not be loaded. Verify that the service can read the file.",
                    exception);
            }
        }
    }
}
