using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.Authorization.Services
{
    public static class OpenIddictServerConfiguration
    {
        public static void ConfigureIssuer(
            OpenIddictServerBuilder options,
            IConfiguration configuration,
            bool isDevelopment)
        {
            var issuer = configuration.GetValue<string>("OpenIddict:Issuer");
            if (string.IsNullOrWhiteSpace(issuer) && isDevelopment)
            {
                var httpsPort = configuration.GetValue<int?>("ASPNETCORE_HTTPS_PORT");
                var httpPort = configuration.GetValue<int?>("ASPNETCORE_HTTP_PORT");
                if (httpsPort is > 0 and <= 65535)
                {
                    issuer = $"https://localhost:{httpsPort}/";
                }
                else if (httpPort is > 0 and <= 65535)
                {
                    issuer = $"http://localhost:{httpPort}/";
                }
            }

            if (!Uri.TryCreate(issuer, UriKind.Absolute, out var issuerUri) ||
                string.IsNullOrWhiteSpace(issuerUri.Host) ||
                !string.IsNullOrEmpty(issuerUri.Query) ||
                !string.IsNullOrEmpty(issuerUri.Fragment) ||
                (!isDevelopment && issuerUri.Scheme != Uri.UriSchemeHttps))
            {
                throw new InvalidOperationException(
                    "OpenIddict:Issuer must be configured as an absolute URI without a query or fragment." +
                    (isDevelopment ? string.Empty : " Production issuers must use HTTPS."));
            }

            options.SetIssuer(issuerUri);
        }

        public static void ConfigureCredentials(
            OpenIddictServerBuilder options,
            IConfiguration configuration,
            bool isDevelopment)
        {
            if (isDevelopment)
            {
                options.AddDevelopmentSigningCertificate()
                    .AddDevelopmentEncryptionCertificate();
                return;
            }

            options.AddSigningCertificate(
                    OpenIddictCertificateLoader.Load(configuration, "OpenIddict:SigningCertificate"))
                .AddEncryptionCertificate(
                    OpenIddictCertificateLoader.Load(configuration, "OpenIddict:EncryptionCertificate"));
        }
    }
}
